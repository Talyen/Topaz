using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Topaz.Tests
{
    public sealed class BoundedAreaTests
    {
        static Type Runtime(string name)=>Type.GetType("Topaz."+name+", Assembly-CSharp",true);
        static object Field(object value,string name)=>value.GetType().GetField(name).GetValue(value);
        static void Set(object value,string name,object next)=>value.GetType().GetField(name).SetValue(value,next);
        static object Call(object value,string name,params object[] args)
        {
            try{return value.GetType().GetMethod(name).Invoke(value,args);}
            catch(TargetInvocationException error){throw error.InnerException;}
        }
        static object Graph(int seed)=>Runtime("Generation.WorldGraph").GetMethod("Create").Invoke(null,new object[]{seed});
        static object Settings()=>Runtime("Generation.WoodlandSettings").GetMethod("BoundedWorld").Invoke(null,null);
        static object[] Values(object value)=>((IEnumerable)value).Cast<object>().ToArray();
        static object[] Property(object value,string name)=>Values(value.GetType().GetProperty(name)?.GetValue(value) ?? Field(value,name));
        static object Plan(object area,object settings)=>Field(Activator.CreateInstance(Runtime("Generation.AreaPlan"),area,settings),"Landscape");
        [TestCase(42)][TestCase(-71)][TestCase(1803)]
        public void GraphAndAllRecipesHavePairedReachableExitsAndThreeBeats(int seed)
        {
            var graph=Graph(seed);Call(graph,"Validate");
            Assert.That(JsonUtility.ToJson(graph),Is.EqualTo(JsonUtility.ToJson(Graph(seed))));
            foreach(var area in Values(Field(graph,"areas")))
            {
                var plan=Plan(area,Settings());
                Assert.That(Property(plan,"Discoveries").Length,Is.EqualTo(3));
                Assert.That(Property(plan,"RequiredResources").Length,Is.EqualTo(16));
                Assert.That(Property(plan,"RequiredResources").All(s=>((string)Field(s,"Id")).StartsWith(Field(area,"id")+"/")),Is.True);
                Call(plan,"Validate");
                foreach(var exit in Values(Field(area,"exits")))
                {
                    var p=(Vector3)Call(plan,"ExitPosition",exit);
                    Assert.That(Call(plan,"Contains",p.x,p.z),Is.True);
                    Assert.That(Call(plan,"AreaBuildable",p.x,p.z,1f),Is.False);
                }
            }
        }
        [Test]
        public void ChestCoordinatesCannotShareMaterialsAcrossAreas()
        {
            var index=Activator.CreateInstance(Runtime("Gameplay.WorldStorageIndex"));
            var records=Array.CreateInstance(Runtime("Gameplay.StructureStateRecord"),2);
            for(int i=0;i<2;i++)
            {
                var record=Activator.CreateInstance(records.GetType().GetElementType());
                Set(record,"instanceId",i==0?"home":"river");Set(record,"definitionId","chest");
                Set(record,"regionId",i==0?"wilderness":"area.river");Set(record,"x",3f);Set(record,"z",4f);records.SetValue(record,i);
            }
            // The catalog remains the owner of persistent definition IDs.
            var chest=Runtime("Gameplay.BuildCatalog").GetField("Chest").GetRawConstantValue();
            foreach(var record in records)Set(record,"definitionId",chest);
            Call(index,"Rebuild",records);
            Assert.That(Field(Values(Call(index,"Nearby",0f,0f,"wilderness")).Single(),"instanceId"),Is.EqualTo("home"));
            Assert.That(Field(Values(Call(index,"Nearby",0f,0f,"area.river")).Single(),"instanceId"),Is.EqualTo("river"));
        }
        [Test]
        public void BrokenReturnPassageIsRejected()
        {
            var graph=Graph(42);var area=Values(Field(graph,"areas"))[0];var exit=Values(Field(area,"exits"))[0];
            Set(exit,"destinationExitId","missing");Assert.Throws<ArgumentException>(()=>Call(graph,"Validate"));
        }
        [Test]
        public void QualityDensityDoesNotChangeAreaResourcesOrExits()
        {
            var area=Values(Field(Graph(42),"areas"))[1];var settings=Field(area,"generationSettings");
            var first=Plan(area,settings);Set(settings,"decorationCount",0);Set(settings,"groundCoverDensity",0f);var second=Plan(area,settings);
            string Resource(object s)=>Field(s,"Id")+":"+Field(s,"X")+":"+Field(s,"Z");
            Assert.That(Property(second,"RequiredResources").Select(Resource),Is.EqualTo(Property(first,"RequiredResources").Select(Resource)));
            Assert.That(Property(second,"Routes").Select(r=>Field(r,"Id")),Is.EqualTo(Property(first,"Routes").Select(r=>Field(r,"Id"))));
        }
        [Test,Category("Stress")]
        public void HundredSeedAreaGraphsAndRecipesRemainValid()
        {for(int seed=0;seed<100;seed++){var graph=Graph(seed);Call(graph,"Validate");foreach(var area in Values(Field(graph,"areas")))Call(Plan(area,Settings()),"Validate");}}
        [Test]
        public void SaveRoundTripRetainsAreaVisitAndSurvivingEnemyHealth()
        {
            string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"TopazAreas-"+Guid.NewGuid().ToString("N"));
            try
            {
                var profile=Activator.CreateInstance(Runtime("Gameplay.TopazProfileData"));
                var character=Call(profile,"CreateCharacter","viking.villager.male.1");var world=Call(profile,"CreateWorld");
                var visit=Call(profile,"GetOrCreateVisit",Field(character,"id"),Field(world,"id"));
                Set(visit,"regionId","area.river");((IList)Field(visit,"discoveredAreaIds")).Add("area.river");
                var enemy=Activator.CreateInstance(Runtime("Gameplay.EnemyStateRecord"));
                Set(enemy,"areaId","area.river");Set(enemy,"spawnId","area.river/beat.0.enemy.0");Set(enemy,"health",2);Set(enemy,"x",30f);
                ((IList)Field(world,"enemyStates")).Add(enemy);
                var repository=Activator.CreateInstance(Runtime("Gameplay.ProfileRepository"),directory);Call(repository,"Save",profile);
                var loaded=Call(repository,"Load");Call(loaded,"Validate");
                Assert.That(Field(Values(Field(loaded,"visits"))[0],"regionId"),Is.EqualTo("area.river"));
                var returned=Values(Field(Values(Field(loaded,"worlds"))[0],"enemyStates")).Single();
                Assert.That(Field(returned,"health"),Is.EqualTo(2));Assert.That(Field(returned,"areaId"),Is.EqualTo("area.river"));
            }
            finally{if(System.IO.Directory.Exists(directory))System.IO.Directory.Delete(directory,true);}
        }
    }
}
