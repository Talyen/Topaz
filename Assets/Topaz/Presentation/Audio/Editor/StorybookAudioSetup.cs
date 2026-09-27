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
        const string Grass="Assets/ThirdParty/Sonniss/Selected/Movement/2017/Tovusound - Edward – Foleyart Co-d2064c/169_Foley_Footsteps_Grass_Sneaker_Walk_Fast_Run_Jog_Close.wav";
        const string Gravel="Assets/ThirdParty/Sonniss/Selected/Movement/2019/Studio 23 - Ultimate Footstep Co-35e37a/S23_SFX_Footsteps_Gravel_Loafers_Loops_Walk_Normal.wav";
        const string Birds="Assets/ThirdParty/Sonniss/Selected/Creatures/2016/Mindful Audio - Woodland Atmosph-e28f65/MAFX001 dew drops wind birds woodpecker.wav";
        const string Wind="Assets/ThirdParty/Sonniss/Selected/World/2015/Soundopolis - Natures Fury_Wind_-21131f/Wind_Forest_Fienup_001.wav";
        [MenuItem("Topaz/Audio/Configure Storybook Sound")]
        public static void Apply()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var provenance=new StringBuilder("# Storybook sound derivatives\n\nSonniss GDC license v2; private game assets, no raw redistribution. Original files and archives are unchanged.\n\n");
            var grass=Steps(Grass,"Grass",provenance);var gravel=Steps(Gravel,"Gravel",provenance);
            var birds=Import(Birds,true);var wind=Import(Wind,true);
            File.WriteAllText(Root+"SOURCE.md",provenance.ToString());
            var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            var player=GameObject.Find("Player");var audio=player.GetComponent<WildernessAudio>()??player.AddComponent<WildernessAudio>();
            audio.grassSteps=grass;audio.gravelSteps=gravel;audio.woodlandDay=birds;audio.woodlandWind=wind;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static AudioClip Import(string path,bool streaming)
        {
            if(!File.Exists(path))throw new InvalidOperationException("Restore owned sound library: "+path);
            var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
            importer.forceToMono=!streaming;importer.loadInBackground=false;
            settings.loadType=streaming?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=streaming?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
            settings.quality=.65f;settings.sampleRateSetting=AudioSampleRateSetting.OverrideSampleRate;settings.sampleRateOverride=22050;
            importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static AudioClip[] Steps(string source,string label,StringBuilder provenance)
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
            using(var sha=SHA256.Create())provenance.AppendLine(source+"\nSHA-256: "+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(source))).Replace("-","").ToLowerInvariant());
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
