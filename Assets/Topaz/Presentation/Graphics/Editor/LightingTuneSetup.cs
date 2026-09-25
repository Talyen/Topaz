using System;
using Topaz.FeelStudy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Editor
{
    /// <summary>Applies the authored low-light pass without rebuilding existing scenes.</summary>
    public static class LightingTuneSetup
    {
        const string GlassPath = "Assets/Topaz/Presentation/Art/Materials/Lantern Glass.mat";
        const string CookiePath = "Assets/Topaz/Presentation/Rendering/Cookies/Lantern Rear Falloff.cubemap";
        const string Scenes = "Assets/Topaz/World/Scenes/";

        [MenuItem("Topaz/Apply Low-Light Tuning")]
        public static void Apply()
        {
            Material glass = EnsureGlass();
            TuneScene("Bootstrap", glass);
            TuneScene("Expedition", glass);
            TuneScene("Crypt", glass);
            if (AssetDatabase.LoadAssetAtPath<Cubemap>(CookiePath) != null)
                AssetDatabase.DeleteAsset(CookiePath);
            if (AssetDatabase.IsValidFolder("Assets/Topaz/Presentation/Rendering/Cookies") &&
                AssetDatabase.FindAssets("", new[] { "Assets/Topaz/Presentation/Rendering/Cookies" }).Length == 0)
                AssetDatabase.DeleteAsset("Assets/Topaz/Presentation/Rendering/Cookies");
            AssetDatabase.SaveAssets();
            Debug.Log("[Topaz] Centered lantern and wider warm lighting applied.");
        }

        static Material EnsureGlass()
        {
            Material glass = AssetDatabase.LoadAssetAtPath<Material>(GlassPath);
            if (glass != null) return glass;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
            glass = new Material(shader) { name = "Lantern Glass" };
            glass.SetColor("_BaseColor", new Color(.46f, .39f, .30f));
            glass.SetColor("_EmissionColor", Color.black);
            glass.SetFloat("_Smoothness", .2f);
            glass.DisableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(glass, GlassPath);
            return glass;
        }

        static void TuneScene(string name, Material glass)
        {
            Scene scene = EditorSceneManager.OpenScene(Scenes + name + ".unity", OpenSceneMode.Single);
            int changed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    float range = light.name switch
                    {
                        "Fire Glow" => 8f,
                        "Campfire Glow" => 10f,
                        "Home Warm Fill" => 11f,
                        "Crypt Light" when light.color.r > .95f && light.color.g > .5f => 8f,
                        _ => 0f
                    };
                    if (range <= 0f || Mathf.Approximately(light.range, range)) continue;
                    light.range = range;
                    EditorUtility.SetDirty(light);
                    changed++;
                }

            if (name == "Bootstrap")
            {
                GameObject player = GameObject.Find("Player");
                PlayerLantern lantern = player != null ? player.GetComponent<PlayerLantern>() : null;
                if (lantern == null) throw new InvalidOperationException("Player lantern is missing.");
                var serialized = new SerializedObject(lantern);
                serialized.FindProperty("glassMaterial").objectReferenceValue = glass;
                serialized.FindProperty("lightOffset").vector3Value = new Vector3(0f, 1.05f, 0f);
                serialized.FindProperty("lightRange").floatValue = 8.5f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed++;

                GameObject prop = GameObject.Find("KayKit Lantern");
                if (prop == null) throw new InvalidOperationException("Stationary lantern is missing.");
                int glassSlots = 0;
                foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
                {
                    Renderer source = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                    if (source == null) continue;
                    Material[] sourceMaterials = source.sharedMaterials;
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length && i < sourceMaterials.Length; i++)
                    {
                        if (sourceMaterials[i] == null ||
                            sourceMaterials[i].name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        materials[i] = glass;
                        glassSlots++;
                    }
                    renderer.sharedMaterials = materials;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                if (glassSlots == 0) throw new InvalidOperationException("Stationary lantern glass slot was not found.");
                changed += glassSlots;
            }

            if (changed == 0) throw new InvalidOperationException("No lights found to tune in " + name);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Topaz] Tuned {name}: {changed} lighting updates.");
        }
    }
}
