using UnityEngine;

namespace Topaz.Characters
{
    /// <summary>Authored presentation contract. Bone names and imported hierarchy stay inside this prefab.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterVisual : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] Transform rightHand;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform lanternAnchor;
        [SerializeField] Transform guardUpperArm;
        [SerializeField] Transform guardLowerArm;
        [SerializeField] GameObject shield;

        Topaz.Gameplay.WorldSession session;
        Topaz.Combat.PlayerCombat combat;
        GameObject placeholderWeapon;
        Transform shaft;
        GameObject toolHead;
        void Start()
        {
            int layer=LayerMask.NameToLayer("Character Visibility");
            if(layer>=0 && (GetComponentInParent<Topaz.Player.PlayerController>()!=null || GetComponentInParent<Topaz.Combat.EnemyCombatant>()!=null))
                foreach(var renderer in GetComponentsInChildren<Renderer>(true))
                {renderer.gameObject.layer=layer;renderer.renderingLayerMask|=0x80000000u;}
            if (GetComponent<PrototypeHumanoidMotion>() != null ||
                (animator != null && animator.runtimeAnimatorController != null)) return;
            session=GetComponentInParent<Topaz.Gameplay.WorldSession>();
            combat=GetComponentInParent<Topaz.Combat.PlayerCombat>();
            if(session!=null && rightHand!=null)
            {
                placeholderWeapon=new GameObject("Prototype Tool");
                placeholderWeapon.transform.SetParent(rightHand,false);
                shaft=Part("Shaft",placeholderWeapon.transform).transform;
                toolHead=Part("Tool Head",placeholderWeapon.transform);

            }
        }
        void LateUpdate()
        {
            if(session==null)return;
            if(shield!=null)shield.SetActive(session.HasShield);
            if(placeholderWeapon!=null)
            {
                string tool=session.SelectedManualTool;
                var skill=session.CurrentWeapon?.Skill ?? Topaz.Combat.WeaponSkill.Swords;
                bool axe=tool=="axe" || (tool=="sword" && skill==Topaz.Combat.WeaponSkill.Axes);
                bool pick=tool=="pickaxe";
                bool staff=tool=="sword" && skill==Topaz.Combat.WeaponSkill.Staff;
                bool bow=tool=="sword" && skill==Topaz.Combat.WeaponSkill.Crossbows;
                placeholderWeapon.SetActive(tool=="axe" ? session.HasAxe : pick ? session.HasPickaxe : session.HasWeapon);
                shaft.localPosition=new Vector3(0,staff?.5f:.3f,.1f);
                shaft.localScale=new Vector3(.07f,staff?1.2f:bow?.45f:.7f,.07f);
                toolHead.SetActive(axe||pick||staff||bow);
                toolHead.transform.localPosition=new Vector3(axe?.13f:0,staff?1.05f:bow?.25f:.58f,.1f);
                toolHead.transform.localScale=staff?Vector3.one*.18f:bow?new Vector3(.65f,.07f,.12f):pick?new Vector3(.6f,.08f,.1f):new Vector3(.3f,.25f,.1f);
                placeholderWeapon.transform.localRotation=Quaternion.Euler(combat!=null&&combat.IsStrikeActive?70:0,0,0);
            }
        }

        GameObject Part(string label,Transform parent)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name=label;part.transform.SetParent(parent,false);
            Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial=bodyRenderer.sharedMaterial;
            return part;
        }

        public Animator Animator => animator;
        public Renderer BodyRenderer => bodyRenderer;
        public Transform RightHand => rightHand;
        public Transform LeftHand => leftHand;
        public Transform LanternAnchor => lanternAnchor;
        public Transform GuardUpperArm => guardUpperArm;
        public Transform GuardLowerArm => guardLowerArm;
        public GameObject Shield => shield;
        public bool HasPlayerBindings => animator != null && bodyRenderer != null &&
            rightHand != null && leftHand != null && lanternAnchor != null &&
            guardUpperArm != null && guardLowerArm != null;
    }
}
