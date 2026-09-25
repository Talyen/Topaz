using UnityEngine;

namespace Topaz.AnimationStudy
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
