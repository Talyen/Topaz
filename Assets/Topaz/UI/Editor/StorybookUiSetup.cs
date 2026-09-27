using System.Linq;
using TMPro;
using Topaz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Topaz.Editor
{
    public static class StorybookUiSetup
    {
        [MenuItem("Topaz/UI/Apply Storybook Journal Treatment")]
        public static void Apply()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            var hud=Object.FindFirstObjectByType<Topaz.Gameplay.LoopHud>();
            var theme=AssetDatabase.LoadAssetAtPath<TopazUiTheme>("Assets/Topaz/UI/Themes/TopazUiTheme.asset");
            var hudData=new SerializedObject(hud);hudData.FindProperty("journalHeadingFont").objectReferenceValue=theme.DisplayFont;hudData.ApplyModifiedPropertiesWithoutUndo();
            foreach(var owner in new Component[]{hud,hud.GetComponent<Topaz.Gameplay.EquipmentJournalView>(),hud.GetComponent<Topaz.Gameplay.SkillsJournalView>()})
            {
                if(owner==null)continue;
                var serialized=new SerializedObject(owner);
                foreach(string name in new[]{"inventoryPanel","craftPanel","chestPanel","equipmentPanel","rackPanel","skillsPanel"})
                {
                    var property=serialized.FindProperty(name);var panel=property?.objectReferenceValue as GameObject;if(panel==null)continue;
                    foreach(var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))JournalInk.Apply(button);
                    foreach(var label in panel.GetComponentsInChildren<TMP_Text>(true))
                        if(label.GetComponentInParent<UnityEngine.UI.Button>()==null && label.fontSize>=38 && theme.DisplayFont!=null)label.font=theme.DisplayFont;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
