using System;
using System.Collections.Generic;
using System.Linq;
using Topaz.Characters;
using Topaz.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Topaz.Generation.Editor
{
    public static class StorybookCharacterSetup
    {
        const string Root=SyntySampleSetup.CharacterRoot;
        const string Motion="Assets/ThirdParty/KayKitMotion/";
        const string ClipRoot="Assets/Topaz/Characters/Animation/Clips/";
        const string Basic="Assets/Kevin Iglesias/Human Animations/Animations/Male/";
        static AnimationClip Clip(string file,string name=null)
        {
            var clips=AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"));
            var clip=name==null?clips.FirstOrDefault():clips.FirstOrDefault(c=>c.name==name);
            if(clip==null)throw new InvalidOperationException("Missing motion "+file+" / "+name);
            return clip;
        }
        static AnimationClip Kay(string file,string name)
        {
            if(file=="General")
            {
                var baked=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipRoot+name+".anim");
                if(baked!=null)return baked;
            }
            return Clip(Motion+"Rig_Medium_"+file+".fbx",name);
        }
        public static void ExportValidatedGeneralMotion()
        {
            System.IO.Directory.CreateDirectory(ClipRoot);AssetDatabase.Refresh();
            foreach(string name in new[]{"Hit_A","Death_A"})
            {
                var original=Clip(Motion+"Rig_Medium_General.fbx",name);
                foreach(var binding in AnimationUtility.GetCurveBindings(original))
                    foreach(var key in AnimationUtility.GetEditorCurve(original,binding).keys)
                        if(float.IsNaN(key.value)||float.IsInfinity(key.value))throw new InvalidOperationException("Invalid motion: "+name);
                string path=ClipRoot+name+".anim";var copy=Object.Instantiate(original);copy.name=name;
                var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if(existing==null)AssetDatabase.CreateAsset(copy,path);
                else {EditorUtility.CopySerialized(copy,existing);Object.DestroyImmediate(copy);EditorUtility.SetDirty(existing);}
            }
            AssetDatabase.SaveAssets();Apply();
            var dependency=AssetDatabase.FindAssets("t:AnimatorController",new[]{Root.TrimEnd('/')}).Select(AssetDatabase.GUIDToAssetPath)
                .Any(path=>AssetDatabase.GetDependencies(path,true).Contains(Motion+"Rig_Medium_General.fbx"));
            if(dependency)throw new InvalidOperationException("A controller still depends on the retired General source.");
            AssetDatabase.DeleteAsset(Motion+"Rig_Medium_General.fbx");AssetDatabase.SaveAssets();
        }
        [MenuItem("Topaz/Characters/Configure Storybook Motion")]
        public static void Apply()
        {
            ConfigureHumanoids();
            var playerController=Controller(false);var enemyController=Controller(true);
            var player=CreatePlayer(playerController);
            var enemy=PrefabUtility.LoadPrefabContents(Root+"Skeleton.prefab");
            ConfigureVisual(enemy,enemyController);
            PrefabUtility.SaveAsPrefabAsset(enemy,Root+"Skeleton.prefab");PrefabUtility.UnloadPrefabContents(enemy);
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Root.TrimEnd('/')}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);
                if(!path.Contains("Wilderness Skeleton"))continue;
                var actor=PrefabUtility.LoadPrefabContents(path);
                foreach(var visual in actor.GetComponentsInChildren<CharacterVisual>(true))ConfigureVisual(visual.gameObject,enemyController);
                PrefabUtility.SaveAsPrefabAsset(actor,path);PrefabUtility.UnloadPrefabContents(actor);
            }
            var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            foreach(var appearance in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PlayerAppearance>(true)))
            {
                var so=new SerializedObject(appearance);var parent=(Transform)so.FindProperty("visualRoot").objectReferenceValue;
                foreach(var old in parent.GetComponentsInChildren<CharacterVisual>(true).Select(v=>v.gameObject).ToArray())
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(old))
                        PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(old),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    Object.DestroyImmediate(old);
                }
                so.Update();
                so.FindProperty("rogueVisual").objectReferenceValue=SyntySampleSetup.Instance(player,parent);
                var looks=so.FindProperty("looks");for(int i=0;i<looks.arraySize;i++)looks.GetArrayElementAtIndex(i).FindPropertyRelative("model").objectReferenceValue=player;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void ConfigureHumanoids()
        {
            var mapping=new Dictionary<string,string>{{"Hips","hips"},{"Spine","spine"},{"Chest","chest"},{"Head","head"},
                {"LeftUpperArm","upperarm.l"},{"LeftLowerArm","lowerarm.l"},{"LeftHand","hand.l"},{"RightUpperArm","upperarm.r"},{"RightLowerArm","lowerarm.r"},{"RightHand","hand.r"},
                {"LeftUpperLeg","upperleg.l"},{"LeftLowerLeg","lowerleg.l"},{"LeftFoot","foot.l"},{"LeftToes","toes.l"},{"RightUpperLeg","upperleg.r"},{"RightLowerLeg","lowerleg.r"},{"RightFoot","foot.r"},{"RightToes","toes.r"}};
            foreach(var file in new[]{"CombatMelee","CombatRanged","MovementAdvanced","Tools","General"})
            {
                if(file=="General" && AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipRoot+"Hit_A.anim")!=null && AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipRoot+"Death_A.anim")!=null)continue;
                string path=Motion+"Rig_Medium_"+file+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var description=importer.humanDescription;
                description.human=mapping.Select(p=>new HumanBone {humanName=p.Key,boneName=p.Value,limit=new HumanLimit {useDefaultValues=true}}).ToArray();
                description.skeleton=model.GetComponentsInChildren<Transform>().Select(t=>new SkeletonBone {name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
                description.upperArmTwist=.5f;description.lowerArmTwist=.5f;description.upperLegTwist=.5f;description.lowerLegTwist=.5f;description.armStretch=.05f;description.legStretch=.05f;
                importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.humanDescription=description;
                string[] required=file=="General"?new[]{"Hit_A","Death_A"}:
                    file=="Tools"?new[]{"Chop","Pickaxe"}:
                    file=="MovementAdvanced"?new[]{"Dodge_Forward","Dodge_Backward","Dodge_Left","Dodge_Right"}:
                    file=="CombatRanged"?new[]{"Ranged_2H_Aiming","Ranged_2H_Reload","Ranged_Magic_Spellcasting"}:
                    new[]{"Melee_1H_Attack_Chop","Melee_1H_Attack_Slice_Horizontal","Melee_2H_Attack_Chop","Melee_Blocking"};
                var clips=importer.defaultClipAnimations.Where(c=>required.Contains(c.name)).ToArray();
                if(clips.Length!=required.Length)throw new InvalidOperationException("Missing required source motion in "+file);
                foreach(var clip in clips){clip.loopTime=clip.name.Contains("Idle")||clip.name=="Melee_Blocking";clip.lockRootRotation=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.keepOriginalPositionY=true;}
                importer.clipAnimations=clips;importer.SaveAndReimport();
                var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                if(avatar==null||!avatar.isValid||!avatar.isHuman)throw new InvalidOperationException("Invalid humanoid retargeting: "+file);
            }
        }
        static AnimatorController Controller(bool enemy)
        {
            string path=Root+(enemy?"Skeleton Motion":"Wanderer Motion")+".controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.parameters=Array.Empty<AnimatorControllerParameter>();
            foreach(var name in new[]{"Move","MoveX","MoveY"})controller.AddParameter(name,AnimatorControllerParameterType.Float);
            var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;
            var sm=controller.layers[0].stateMachine;
            foreach(var old in sm.states)sm.RemoveState(old.state);
            foreach(var old in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimatorState>().ToArray())Object.DestroyImmediate(old,true);
            foreach(var old in AssetDatabase.LoadAllAssetsAtPath(path).OfType<BlendTree>().ToArray())Object.DestroyImmediate(old,true);
            var locomotion=sm.AddState("Locomotion");sm.defaultState=locomotion;
            var blend=new BlendTree {name="Travel",blendType=enemy?BlendTreeType.Simple1D:BlendTreeType.FreeformDirectional2D,blendParameter=enemy?"Move":"MoveX",blendParameterY="MoveY",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(blend,controller);
            var idle=Clip(Basic+"Idles/HumanM@Idle01.fbx");
            if(enemy){blend.AddChild(idle,0);blend.AddChild(Clip(Basic+"Movement/Run/HumanM@Run01_Forward.fbx"),1);}
            else
            {
                blend.AddChild(idle,Vector2.zero);
                foreach(var d in new[]{("Forward",new Vector2(0,1)),("Backward",new Vector2(0,-1)),("Left",new Vector2(-1,0)),("Right",new Vector2(1,0)),("ForwardLeft",new Vector2(-1,1)),("ForwardRight",new Vector2(1,1)),("BackwardLeft",new Vector2(-1,-1)),("BackwardRight",new Vector2(1,-1))})
                {
                    blend.AddChild(Clip(Basic+"Movement/Walk/HumanM@Walk01_"+d.Item1+".fbx"),d.Item2*.5f);
                    blend.AddChild(Clip(Basic+"Movement/Run/HumanM@Run01_"+d.Item1+".fbx"),d.Item2);
                }
            }
            locomotion.motion=blend;
            void State(string name,AnimationClip clip){var state=sm.AddState(name);state.motion=clip;state.writeDefaultValues=true;}
            foreach(var d in new[]{"Forward","Backward","Left","Right"})State("Dodge"+d,Kay("MovementAdvanced","Dodge_"+d));
            State("Jump",Clip(Basic+"Movement/Jump/HumanM@Jump01.fbx"));
            State("Sword",Kay("CombatMelee","Melee_1H_Attack_Slice_Horizontal"));State("Axe",Kay("Tools","Chop"));State("Pickaxe",Kay("Tools","Pickaxe"));
            State("CombatAxe",Kay("CombatMelee","Melee_2H_Attack_Chop"));State("Staff",Kay("CombatRanged","Ranged_Magic_Spellcasting"));
            State("Crossbow",Kay("CombatRanged","Ranged_2H_Aiming"));State("CrossbowReload",Kay("CombatRanged","Ranged_2H_Reload"));
            State("Hit",Kay("General","Hit_A"));State("Attack",Kay("CombatMelee","Melee_1H_Attack_Chop"));State("Death",Kay("General","Death_A"));State("Guard",Kay("CombatMelee","Melee_Blocking"));
            EditorUtility.SetDirty(controller);return controller;
        }
        static GameObject CreatePlayer(AnimatorController controller)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/SidekickCharacters/Characters/Starter/Starter_01/Starter_01.prefab");
            var go=Object.Instantiate(prefab);go.name="Storybook Knight";
            var animator=go.GetComponent<Animator>();if(animator==null)animator=go.GetComponentInChildren<Animator>();
            var visual=go.AddComponent<CharacterVisual>();SyntySampleSetup.Set(visual,"animator",animator);
            var body=go.GetComponentInChildren<SkinnedMeshRenderer>();SyntySampleSetup.Set(visual,"bodyRenderer",body);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Sidekick Knight.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,Root+"Sidekick Knight.mat");}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Synty/SidekickCharacters/Characters/Starter/Starter_01/Textures/T_Starter_01ColorMap.png"));material.SetFloat("_Smoothness",.18f);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
            Transform Bone(HumanBodyBones bone)=>animator.GetBoneTransform(bone);
            SyntySampleSetup.Set(visual,"rightHand",Bone(HumanBodyBones.RightHand));SyntySampleSetup.Set(visual,"leftHand",Bone(HumanBodyBones.LeftHand));
            SyntySampleSetup.Set(visual,"guardUpperArm",Bone(HumanBodyBones.LeftUpperArm));SyntySampleSetup.Set(visual,"guardLowerArm",Bone(HumanBodyBones.LeftLowerArm));
            var lantern=new GameObject("Lantern Anchor").transform;lantern.SetParent(Bone(HumanBodyBones.Hips),false);lantern.localPosition=new Vector3(.22f,0,0);SyntySampleSetup.Set(visual,"lanternAnchor",lantern);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Wanderer.prefab").GetComponent<PrototypeHumanoidMotion>();
            var motion=go.AddComponent<PrototypeHumanoidMotion>();
            motion.equipment=source.equipment.Select(item=>new PrototypeHumanoidMotion.HeldVisual{id=item.id,model=HeldItem(item.id,Bone(HumanBodyBones.RightHand),item.model)}).ToArray();
            var shield=new GameObject("Synty Shield");shield.transform.SetParent(Bone(HumanBodyBones.LeftHand),false);
            AddProp(shield.transform,"Assets/Synty/PolygonStarter/Prefabs/SM_Wep_Shield_04.prefab",new Vector3(.55f,.7f,.12f),Vector3.down*.35f);
            shield.SetActive(false);SyntySampleSetup.Set(visual,"shield",shield);
            ConfigureVisual(go,controller);EditorUtility.SetDirty(material);
            return SyntySampleSetup.Save(go,"Storybook Knight");
        }
        static GameObject HeldItem(string id,Transform hand,GameObject fallback)
        {
            var held=new GameObject(id);held.transform.SetParent(hand,false);
            if(id=="sword")AddProp(held.transform,"Assets/Synty/PolygonStarter/Prefabs/SM_PolygonPrototype_Prop_Sword_01.prefab",new Vector3(.13f,.9f,.04f),Vector3.down*.12f);
            else if(id=="staff")
            {
                AddProp(held.transform,"Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_02.prefab",new Vector3(.065f,1.45f,.065f),Vector3.down*.45f);
                AddProp(held.transform,"Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Rock_03.prefab",Vector3.one*.2f,Vector3.up*.92f);
            }
            else if(id=="crossbow")
            {
                string plank="Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Plank_01.prefab";
                AddProp(held.transform,plank,new Vector3(.09f,.55f,.1f),Vector3.down*.12f);
                AddProp(held.transform,plank,new Vector3(.55f,.04f,.055f),Vector3.up*.32f);
            }
            else {Object.DestroyImmediate(held);held=Object.Instantiate(fallback,hand,false);}
            held.SetActive(false);return held;
        }
        internal static GameObject AddProp(Transform parent,string path,Vector3 size,Vector3 bottom)
        {
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(original==null)throw new InvalidOperationException("Missing owned prop: "+path);
            var model=Object.Instantiate(original,parent,false);
            foreach(var collider in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            SyntySampleSetup.Fit(model,size,bottom);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var originalMaterial=renderer.sharedMaterial;
                string target=SyntySampleSetup.WorldRoot+"Prop "+originalMaterial.name+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(target);
                if(material==null)
                {
                    material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    var map=originalMaterial.HasProperty("_Albedo_Map")?originalMaterial.GetTexture("_Albedo_Map"):originalMaterial.HasProperty("_MainTex")?originalMaterial.GetTexture("_MainTex"):null;
                    material.SetTexture("_BaseMap",map);material.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(material,target);
                }
                renderer.sharedMaterial=material;
            }
            return model;
        }
        static void ConfigureVisual(GameObject go,AnimatorController controller)
        {
            var visual=go.GetComponent<CharacterVisual>();var animator=visual.Animator;
            animator.enabled=true;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            var driver=go.GetComponent<CharacterAnimationDriver>()??go.AddComponent<CharacterAnimationDriver>();
            void Set(string field,Object value)=>SyntySampleSetup.Set(driver,field,value);
            var old=go.GetComponent<PrototypeHumanoidMotion>();
            if(old!=null)
            {
                foreach(var item in old.equipment)
                {
                    string field=item.id=="combat-axe"?"combatAxeVisual":item.id+"Visual";Set(field,item.model);
                }
                Object.DestroyImmediate(old);
            }
            Set("shieldVisual",visual.Shield);
            foreach(var d in new[]{"Forward","Backward","Left","Right"})Set("dodge"+d+"Clip",Kay("MovementAdvanced","Dodge_"+d));
            Set("jumpClip",Clip(Basic+"Movement/Jump/HumanM@Jump01.fbx"));Set("swordClip",Kay("CombatMelee","Melee_1H_Attack_Slice_Horizontal"));
            Set("axeClip",Kay("Tools","Chop"));Set("pickaxeClip",Kay("Tools","Pickaxe"));Set("combatAxeClip",Kay("CombatMelee","Melee_2H_Attack_Chop"));
            Set("staffClip",Kay("CombatRanged","Ranged_Magic_Spellcasting"));Set("crossbowClip",Kay("CombatRanged","Ranged_2H_Aiming"));Set("reloadClip",Kay("CombatRanged","Ranged_2H_Reload"));
            Set("hitClip",Kay("General","Hit_A"));Set("enemyAttackClip",Kay("CombatMelee","Melee_1H_Attack_Chop"));
        }
    }
}
