using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Topaz.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Topaz.Editor
{
    public static class StorybookAudioSetup
    {
        const string Root="Assets/ThirdParty/Sonniss/Derived/Storybook/";
        const string TemporaryRoot="Assets/ThirdParty/Sonniss/EditorTemp";
        const string GrassSource="Selected/Movement/2017/Tovusound - Edward – Foleyart Co-d2064c/169_Foley_Footsteps_Grass_Sneaker_Walk_Fast_Run_Jog_Close.wav";
        const string GravelSource="Selected/Movement/2019/Studio 23 - Ultimate Footstep Co-35e37a/S23_SFX_Footsteps_Gravel_Loafers_Loops_Walk_Normal.wav";
        const string Birds="Assets/ThirdParty/Sonniss/Runtime/Woodland Birds.wav";
        const string Wind="Assets/ThirdParty/Sonniss/Runtime/Woodland Wind.wav";

        static string RawSourceRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Documents","Raw Asset Library","Sounds","SonnissGDC","ActiveSources");

        [MenuItem("Topaz/Audio/Configure Storybook Sound")]
        public static void Apply()
        {
            CleanupTemporarySources();
            Directory.CreateDirectory(Root);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(TemporaryRoot);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            try
            {
                var provenance=new StringBuilder("# Storybook sound derivatives\n\nSonniss GDC license v2; private game assets, no raw redistribution. Original masters and source archives are preserved outside Topaz in Documents/Raw Asset Library/Sounds/SonnissGDC.\n\n");
                var grassSource=ExternalSourcePath(GrassSource);var gravelSource=ExternalSourcePath(GravelSource);
                var grass=Steps(StageRawSource(GrassSource,"Grass Generator Input.wav"),grassSource,GrassSource,"Grass",provenance);
                var gravel=Steps(StageRawSource(GravelSource,"Gravel Generator Input.wav"),gravelSource,GravelSource,"Gravel",provenance);
                var birds=Import(Birds,true);var wind=Import(Wind,true);
                provenance.AppendLine("Runtime ambience working copies:\n- "+Birds+": 48 kHz, 24-bit, stereo PCM source; Streaming Vorbis q0.65, preserved sample rate, background load, no preload.\n- "+Wind+": 48 kHz, 24-bit, stereo PCM source; Streaming Vorbis q0.65, preserved sample rate, background load, no preload.\nRaw master hashes and Unity metadata are recorded in ~/Documents/Raw Asset Library/Sounds/SonnissGDC/ActiveSources/active_source_manifest.json.\n");
                File.WriteAllText(Root+"SOURCE.md",provenance.ToString());
                var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
                var player=GameObject.Find("Player");var audio=player.GetComponent<WildernessAudio>()??player.AddComponent<WildernessAudio>();
                audio.grassSteps=grass;audio.gravelSteps=gravel;audio.woodlandDay=birds;audio.woodlandWind=wind;
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally
            {
                CleanupTemporarySources();
            }
        }

        static void CleanupTemporarySources()
        {
            foreach(string name in new[]{"Grass Generator Input.wav","Gravel Generator Input.wav"})
            {
                string path=TemporaryRoot+"/"+name;
                if((File.Exists(path)||AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)!=null)&&!AssetDatabase.DeleteAsset(path))
                    Debug.LogError("Could not remove temporary Storybook audio import: "+path);
            }
            if(AssetDatabase.IsValidFolder(TemporaryRoot)&&Directory.GetFileSystemEntries(TemporaryRoot).Length==0&&!AssetDatabase.DeleteAsset(TemporaryRoot))
                Debug.LogError("Could not remove temporary Storybook audio folder: "+TemporaryRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        static string ExternalSourcePath(string relativePath)=>Path.Combine(RawSourceRoot,relativePath.Replace('/',Path.DirectorySeparatorChar));

        static string StageRawSource(string relativePath,string temporaryName)
        {
            var source=ExternalSourcePath(relativePath);
            if(!File.Exists(source))throw new FileNotFoundException("Restore the private Sonniss source in Documents/Raw Asset Library/Sounds/SonnissGDC/ActiveSources before regenerating Storybook sound.",source);
            var assetPath=TemporaryRoot+"/"+temporaryName;
            File.Copy(source,assetPath,true);AssetDatabase.ImportAsset(assetPath,ImportAssetOptions.ForceSynchronousImport);
            return assetPath;
        }

        static AudioClip Import(string path,bool streaming)
        {
            if(!File.Exists(path))throw new InvalidOperationException("Restore owned sound library: "+path);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
            importer.forceToMono=!streaming;importer.loadInBackground=streaming;
            settings.loadType=streaming?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=streaming?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
            settings.preloadAudioData=false;
            settings.quality=.65f;
            settings.sampleRateSetting=streaming?AudioSampleRateSetting.PreserveSampleRate:AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride=streaming?48000u:22050u;
            importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static AudioClip[] Steps(string source,string originalSource,string sourceLabel,string label,StringBuilder provenance)
        {
            var clip=Import(source,false);clip.LoadAudioData();
            var samples=new float[clip.samples*clip.channels];
            if(!clip.GetData(samples,0))throw new InvalidOperationException("Audio samples could not load: "+source);
            int window=256;var peaks=new List<(int index,float energy)>();
            for(int i=window;i<samples.Length-window;i+=window)
            {float energy=0;for(int j=0;j<window;j++)energy+=samples[i+j]*samples[i+j];peaks.Add((i,energy));}
            var selected=new List<int>();
            foreach(var peak in peaks.OrderByDescending(p=>p.energy))
            {
                if(selected.Any(i=>Math.Abs(i-peak.index)<clip.frequency*.4f))continue;
                selected.Add(peak.index);if(selected.Count==4)break;
            }
            if(selected.Count!=4)throw new InvalidOperationException("Not enough isolated footstep transients.");
            selected.Sort();var result=new List<AudioClip>();
            using(var sha=SHA256.Create())provenance.AppendLine("~/Documents/Raw Asset Library/Sounds/SonnissGDC/ActiveSources/"+sourceLabel+"\nSHA-256: "+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(originalSource))).Replace("-","").ToLowerInvariant());
            foreach(int peak in selected)
            {
                int start=Math.Max(0,peak-(int)(clip.frequency*.06f));int length=Math.Min((int)(clip.frequency*.32f),samples.Length-start);
                var data=new float[length];Array.Copy(samples,start,data,0,length);float maximum=data.Max(v=>Math.Abs(v));
                for(int i=0;i<length;i++)data[i]*=Mathf.Min(Mathf.Clamp01(i/(clip.frequency*.01f)),Mathf.Clamp01((length-1-i)/(clip.frequency*.035f)))*(.65f/Mathf.Max(.001f,maximum));
                string path=Root+label+" Step "+(result.Count+1)+".wav";
                WriteWave(path,data,clip.frequency);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                result.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                provenance.AppendLine($"- {path}: {start/(float)clip.frequency:0.000}s to {(start+length)/(float)clip.frequency:0.000}s; mono, normalized and edge-faded.");
            }
            return result.ToArray();
        }
        static void WriteWave(string path,float[] data,int rate)
        {
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+data.Length*2);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(data.Length*2);
                foreach(float value in data)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value,-1,1)*32767));
            }
        }
    }
}
