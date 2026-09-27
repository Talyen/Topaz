using TMPro;
using UnityEngine;

namespace Topaz.UI
{
    /// <summary>Shared live uGUI treatment over the owner's illustrated Journal.</summary>
    public static class JournalInk
    {
        public static readonly Color Text=new Color32(39,48,35,255);
        public static readonly Color Muted=new Color32(109,98,78,255);
        public static void Apply(UnityEngine.UI.Button button)
        {
            var image=button.targetGraphic as UnityEngine.UI.Image ?? button.GetComponent<UnityEngine.UI.Image>();
            if(image==null)return;
            button.targetGraphic=image;image.color=Color.white;
            button.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors=button.colors;
            colors.normalColor=new Color(.2f,.3f,.2f,.025f);
            colors.highlightedColor=new Color(.2f,.3f,.2f,.12f);
            colors.selectedColor=new Color(.2f,.3f,.2f,.16f);
            colors.pressedColor=new Color(.2f,.3f,.2f,.24f);
            colors.disabledColor=new Color(.25f,.25f,.2f,.015f);colors.fadeDuration=.1f;
            button.colors=colors;
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.color=Text;
            var outline=image.GetComponent<UnityEngine.UI.Outline>()??image.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor=new Color32(165,118,46,255);outline.effectDistance=new Vector2(2,-2);outline.useGraphicAlpha=false;outline.enabled=false;
            if(button.GetComponent<TopazFocusIndicator>()==null && button.GetComponent<Topaz.Gameplay.HomeFocusFrame>()==null)
                button.gameObject.AddComponent<TopazFocusIndicator>();
            if(button.transform.Find("Ink Rule")!=null)return;
            var rule=new GameObject("Ink Rule",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.LayoutElement));
            rule.transform.SetParent(button.transform,false);
            var rect=(RectTransform)rule.transform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=Vector2.up*2;rect.sizeDelta=new Vector2(-24,1);
            rule.GetComponent<UnityEngine.UI.Image>().color=new Color(.25f,.3f,.22f,.24f);rule.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;rule.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout=true;
        }
    }
}
