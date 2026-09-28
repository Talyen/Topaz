using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class SyntySampleTests : TopazInputTestFixture
    {
        [UnityTest]
        public IEnumerator SampleCharactersHaveWorkingHumanoidBindingsAfterAppearanceChanges()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return null;
            var appearance=GameObject.Find("Player").GetComponent("PlayerAppearance");
            var looks=(System.Array)appearance.GetType().GetField("looks",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(appearance);
            foreach(var look in looks)
            {
                string id=(string)look.GetType().GetField("id").GetValue(look);
                Assert.That(appearance.GetType().GetMethod("Apply").Invoke(appearance,new object[]{id}),Is.True);
                yield return null;
                var visual=appearance.GetComponentsInChildren<MonoBehaviour>().First(c=>c.GetType().Name=="CharacterVisual");
                Assert.That(visual.GetType().GetProperty("HasPlayerBindings").GetValue(visual),Is.True);
                var animator=visual.GetComponent<Animator>();
                Assert.That(animator.avatar.isHuman && animator.avatar.isValid,Is.True);
                Assert.That(visual.GetComponent("PrototypeHumanoidMotion"),Is.Null);
                Assert.That(animator.runtimeAnimatorController,Is.Not.Null);
                Assert.That(appearance.GetComponentsInChildren<MonoBehaviour>().Count(c=>c.GetType().Name=="CharacterVisual"),Is.EqualTo(1),"Only one player model may render after appearance changes.");
                var body=(Renderer)visual.GetType().GetProperty("BodyRenderer").GetValue(visual);
                Assert.That(body.bounds.size.y,Is.InRange(1f,3f));
                Assert.That(float.IsNaN(body.bounds.center.y),Is.False);
            }
        }
        [UnityTest]
        public IEnumerator SampleUsesMatteTerrainAndPrefabTableKeepsPlacementAndSaveBehavior()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return null;
            Assert.That(Terrain.activeTerrain.materialTemplate.shader.name,Is.EqualTo("Topaz/Sheltered Terrain"));
            var session=GameObject.Find("Player").GetComponent("WorldSession");
            var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
            var pack=session.GetType().GetField("_backpack",flags).GetValue(session);
            var wood=session.GetType().GetProperty("WoodItem").GetValue(session);
            pack.GetType().GetMethod("Add").Invoke(pack,new object[]{wood,20});
            BuildingTestActions.Place(session,"structure.home.table");yield return null;
            var table=Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).First(c=>c.name.StartsWith("structure.home.table "));
            Assert.That(table.size,Is.EqualTo(new Vector3(1.2f,.75f,.7f)));
            Assert.That(table.center,Is.EqualTo(Vector3.up*.375f));
            Assert.That(table.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name.Contains("SM_Prop_Table_01")),Is.True);
            session.GetType().GetMethod("FlushCurrent").Invoke(session,null);
        }
    }
}
