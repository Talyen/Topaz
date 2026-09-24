using UnityEngine;
using TMPro;

namespace Topaz.UI
{
    /// <summary>Shared authored colors for the first Topaz uGUI design pass.</summary>
    [CreateAssetMenu(menuName = "Topaz/UI Theme", fileName = "TopazUiTheme")]
    public sealed class TopazUiTheme : ScriptableObject
    {
        [SerializeField] Color backdrop = new Color32(16, 27, 29, 255);
        [SerializeField] Color panel = new Color32(28, 43, 45, 255);
        [SerializeField] Color raised = new Color32(41, 60, 62, 255);
        [SerializeField] Color text = new Color32(243, 235, 221, 255);
        [SerializeField] Color mutedText = new Color32(189, 203, 196, 255);
        [SerializeField] Color copper = new Color32(224, 166, 108, 255);
        [SerializeField] Color seaGlass = new Color32(140, 203, 193, 255);
        [SerializeField] Color warning = new Color32(232, 142, 121, 255);
        [SerializeField] Color parchment = new Color32(234, 212, 178, 255);
        [SerializeField] Color ink = new Color32(57, 43, 33, 255);
        [SerializeField] Color iron = new Color32(48, 43, 38, 255);
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
