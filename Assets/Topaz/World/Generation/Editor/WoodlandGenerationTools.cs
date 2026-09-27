using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Topaz.Generation.Editor
{
    public static class WoodlandGenerationTools
    {
        [Serializable] sealed class Report { public int version; public int firstSeed; public int worlds; public double milliseconds; public string status; }
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
            for(int i=0;i<count;i++)new WildernessPlan(unchecked(seed+i)).Validate();
            var report=new Report{version=WildernessPlan.Version,firstSeed=seed,worlds=count,milliseconds=watch.Elapsed.TotalMilliseconds,status="passed"};
            Directory.CreateDirectory("TestResults/Generation");
            File.WriteAllText("TestResults/Generation/seed-batch.json",JsonUtility.ToJson(report,true));
            UnityEngine.Debug.Log("[Topaz] Validated "+report.worlds+" generated worlds in "+report.milliseconds.ToString("0")+" ms.");
        }
    }
}
