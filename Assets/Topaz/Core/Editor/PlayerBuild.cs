using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Topaz.Editor
{
    /// <summary>BuildReport is authoritative: native profile builds can fail while Unity exits with code zero.</summary>
    public static class PlayerBuild
    {
        [Serializable] sealed class Receipt
        {
            public string result, output, platform, completedUtc;
            public ulong bytes;
            public int errors;
        }
        public static void BuildConfiguredProfile()
        {
            string output=null, profilePath=null;
            var args=System.Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)
            {
                if(args[i]=="-buildOutput")output=args[i+1];
                if(args[i]=="-topazBuildProfile")profilePath=args[i+1];
            }
            int exit=1;
            try
            {
                if(string.IsNullOrWhiteSpace(output))throw new BuildFailedException("Missing -buildOutput.");
                var profile=AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);
                if(profile==null)throw new BuildFailedException("Missing or invalid -topazBuildProfile.");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions { buildProfile=profile,locationPathName=output,options=BuildOptions.None });
                var summary=report.summary;
                var receipt=new Receipt {result=summary.result.ToString(),output=summary.outputPath,platform=summary.platform.ToString(),completedUtc=DateTime.UtcNow.ToString("O"),bytes=summary.totalSize,errors=(int)summary.totalErrors};
                File.WriteAllText(output+".build-report.json",JsonUtility.ToJson(receipt,true));
                if(summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Player build returned "+summary.result);
                Debug.Log("[Topaz] Verified player build: "+summary.outputPath);
                exit=0;
            }
            catch(Exception error){Debug.LogException(error);}
            finally{EditorApplication.Exit(exit);}
        }
    }
}
