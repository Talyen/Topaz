using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Topaz.Generation.Editor
{
    public static class WoodlandGenerationTools
    {
        [Serializable] sealed class Report { public int version; public int firstSeed; public int regions; public double milliseconds; public string status; }
        [MenuItem("Topaz/Generation/Validate Seed Batch")]
        public static void ValidateBatch()
        {
            int seed=0,count=200;
            foreach(string arg in System.Environment.GetCommandLineArgs())
            {
                if(arg.StartsWith("--topaz-seed="))seed=int.Parse(arg.Substring(13));
                if(arg.StartsWith("--topaz-count="))count=int.Parse(arg.Substring(14));
            }
            if(count<1||count>10000)throw new ArgumentOutOfRangeException(nameof(count));
            var watch=Stopwatch.StartNew();
            for(int i=0;i<count;i++)foreach(string region in new[]{"home","expedition.clearing"})
                WoodlandPlan.Generate(unchecked(seed+i),region,new WoodlandSettings()).Validate();
            var report=new Report{version=WoodlandPlan.Version,firstSeed=seed,regions=count*2,milliseconds=watch.Elapsed.TotalMilliseconds,status="passed"};
            Directory.CreateDirectory("TestResults/Generation");
            File.WriteAllText("TestResults/Generation/seed-batch.json",JsonUtility.ToJson(report,true));
            UnityEngine.Debug.Log("[Topaz] Validated "+report.regions+" generated regions in "+report.milliseconds.ToString("0")+" ms.");
        }
    }
}
