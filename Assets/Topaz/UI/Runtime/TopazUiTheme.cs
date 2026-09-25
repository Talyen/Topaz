using UnityEngine;
using TMPro;

namespace Topaz.UI
{
    /// <summary>Shared authored colors for Topaz's warm-refuge uGUI language.</summary>
    [CreateAssetMenu(menuName = "Topaz/UI Theme", fileName = "TopazUiTheme")]
    public sealed class TopazUiTheme : ScriptableObject
    {
        [SerializeField] Color backdrop = new Color32(23, 19, 18, 255);
        [SerializeField] Color panel = new Color32(41, 33, 29, 255);
        [SerializeField] Color raised = new Color32(49, 37, 30, 255);
        [SerializeField] Color text = new Color32(255, 241, 216, 255);
        [SerializeField] Color mutedText = new Color32(224, 198, 163, 255);
        [SerializeField] Color copper = new Color32(239, 199, 132, 255);
        [SerializeField] Color seaGlass = new Color32(170, 181, 158, 255);
        [SerializeField] Color warning = new Color32(255, 188, 170, 255);
        [SerializeField] Color parchment = new Color32(231, 213, 180, 255);
        [SerializeField] Color ink = new Color32(55, 39, 31, 255);
        [SerializeField] Color iron = new Color32(51, 42, 36, 255);
        [SerializeField] TMP_FontAsset displayFont;

        public Color Backdrop => backdrop;
        public Color Panel => panel;
        public Color Raised => raised;
        public Color Text => text;
        public Color MutedText => mutedText;
        public Color Copper => copper;
        public Color SeaGlass => seaGlass;
        public Color Warning => warning;
        public Color Parchment => parchment;
        public Color Ink => ink;
        public Color Iron => iron;
        public TMP_FontAsset DisplayFont => displayFont;
    }
}
