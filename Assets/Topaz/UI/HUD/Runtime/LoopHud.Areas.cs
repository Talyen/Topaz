using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Topaz.Gameplay
{
    public sealed partial class LoopHud
    {
        GameObject areaLoading;
        TMP_Text areaLoadingTitle,areaLoadingStage;
        public void ShowAreaLoading(string destination,string stage)
        {
            if(areaLoading==null)
            {
                areaLoading=new GameObject("Area Loading",typeof(RectTransform),typeof(Image));areaLoading.transform.SetParent(transform,false);
                var full=areaLoading.GetComponent<RectTransform>();full.anchorMin=Vector2.zero;full.anchorMax=Vector2.one;full.offsetMin=full.offsetMax=Vector2.zero;
                areaLoading.GetComponent<Image>().color=new Color(.025f,.045f,.04f,1);
                TMP_Text Label(string name,float y,float size)
                {
                    var root=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));root.transform.SetParent(areaLoading.transform,false);
                    var rect=root.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.1f,.5f);rect.anchorMax=new Vector2(.9f,.5f);rect.sizeDelta=new Vector2(0,90);rect.anchoredPosition=new Vector2(0,y);
                    var text=root.GetComponent<TextMeshProUGUI>();text.font=JournalHeadingFont??statusLabel.font;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.92f,.86f,.68f);text.raycastTarget=false;return text;
                }
                areaLoadingTitle=Label("Destination",35,48);areaLoadingStage=Label("Preparation",-45,24);
            }
            areaLoadingTitle.text=destination;
            areaLoadingStage.text=stage=="ready"?"Entering the wilderness…":"Loading…";
            areaLoading.SetActive(true);areaLoading.transform.SetAsLastSibling();
        }
        public void HideAreaLoading(){if(areaLoading!=null)areaLoading.SetActive(false);}
    }
}
