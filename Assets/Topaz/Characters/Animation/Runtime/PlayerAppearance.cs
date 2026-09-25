using System;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using UnityEngine;

namespace Topaz.AnimationStudy
{
    /// <summary>Swaps only the rendered KayKit body; the player object owns gameplay.</summary>
    public sealed class PlayerAppearance : MonoBehaviour
    {
        [Serializable]
        public sealed class LookOption
        {
            public string id;
            public GameObject model;
            public Avatar avatar;
            public Material material;
        }

        [SerializeField] Transform visualRoot;
        [SerializeField] GameObject rogueVisual;
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] PlayerLantern lantern;
        [SerializeField] PlayerCombat combat;
        [SerializeField] RuntimeAnimatorController playerController;
        [SerializeField] LookOption[] looks;

        GameObject _currentVisual;
        string _currentId;
        CharacterAnimationDriver _rogueDriver;
        GameObject _swordTemplate;
        GameObject _axeTemplate;
        GameObject _pickaxeTemplate;
        GameObject _combatAxeTemplate;
        GameObject _shieldTemplate;
        GameObject _staffTemplate;
        GameObject _crossbowTemplate;

        public string CurrentId => _currentId;

        public void ShowShieldImpact()
        {
            GameObject visual = _currentVisual != null ? _currentVisual : rogueVisual;
            visual?.GetComponent<ShieldGuardPose>()?.Impact();
        }

        void Awake()
        {
            if (rogueVisual == null || visualRoot == null || movement == null || combat == null ||
                playerController == null || looks == null || looks.Length == 0)
            {
                Debug.LogError("Player appearance references are incomplete.", this);
                enabled = false;
                return;
            }
            _rogueDriver = rogueVisual.GetComponent<CharacterAnimationDriver>();
            Transform hand = FindRightHand(rogueVisual.transform);
            _swordTemplate = hand != null ? hand.Find("Held Sword")?.gameObject : null;
            _axeTemplate = hand != null ? hand.Find("Held Axe")?.gameObject : null;
            _pickaxeTemplate = hand != null ? hand.Find("Held Pickaxe")?.gameObject : null;
            _combatAxeTemplate = hand != null ? hand.Find("Held Combat Axe")?.gameObject : null;
            _staffTemplate = hand != null ? hand.Find("Held Staff")?.gameObject : null;
            _crossbowTemplate = hand != null ? hand.Find("Held Crossbow")?.gameObject : null;
            Transform leftHand = rogueVisual.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "handslot.l");
            _shieldTemplate = leftHand != null ? leftHand.Find("Held Shield")?.gameObject : null;
            if (_rogueDriver == null || _swordTemplate == null || _axeTemplate == null ||
                _pickaxeTemplate == null ||
                _combatAxeTemplate == null || _staffTemplate == null ||
                _crossbowTemplate == null)
            {
                Debug.LogError("Rogue animation or held equipment is missing.", this);
                enabled = false;
            }
            _currentId = CharacterLooks.Rogue;
        }

        public bool Apply(string appearanceId)
        {
            if (!enabled) return false;
            string id = CharacterLooks.SupportedOrRogue(appearanceId);
            if (_currentVisual != null && _currentId == id) return true;
            LookOption option = Option(id);
            if (option == null) return false;
            if (_currentVisual != null)
            {
                _currentVisual.SetActive(false);
                Destroy(_currentVisual);
                _currentVisual = null;
            }
            if (id == CharacterLooks.Rogue)
            {
                rogueVisual.SetActive(true);
                ShieldGuardPose rogueGuard = rogueVisual.GetComponent<ShieldGuardPose>();
                if (rogueGuard == null) rogueGuard = rogueVisual.AddComponent<ShieldGuardPose>();
                rogueGuard.Bind(combat);
                movement.SetBodyRenderer(rogueVisual.GetComponentInChildren<Renderer>(true));
                lantern?.AttachToVisual(rogueVisual.transform);
                _currentId = id;
                return true;
            }

            GameObject instance = BuildModel(option, visualRoot, true);
            if (instance == null) return false;
            Transform hand = FindRightHand(instance.transform);
            if (hand == null)
            {
                Destroy(instance);
                rogueVisual.SetActive(true);
                Debug.LogError("Selected character has no right-hand equipment socket: " + id, this);
                return false;
            }
            GameObject sword = Instantiate(_swordTemplate, hand);
            GameObject axe = Instantiate(_axeTemplate, hand);
            GameObject pickaxe = Instantiate(_pickaxeTemplate, hand);
            GameObject combatAxe = Instantiate(_combatAxeTemplate, hand);
            GameObject staff = Instantiate(_staffTemplate, hand);
            GameObject crossbow = Instantiate(_crossbowTemplate, hand);
            sword.name = "Held Sword";
            axe.name = "Held Axe";
            pickaxe.name = "Held Pickaxe";
            combatAxe.name = "Held Combat Axe";
            staff.name = "Held Staff";
            crossbow.name = "Held Crossbow";
            sword.transform.localPosition = _swordTemplate.transform.localPosition;
            sword.transform.localRotation = _swordTemplate.transform.localRotation;
            sword.transform.localScale = _swordTemplate.transform.localScale;
            axe.transform.localPosition = _axeTemplate.transform.localPosition;
            axe.transform.localRotation = _axeTemplate.transform.localRotation;
            axe.transform.localScale = _axeTemplate.transform.localScale;
            pickaxe.transform.localPosition = _pickaxeTemplate.transform.localPosition;
            pickaxe.transform.localRotation = _pickaxeTemplate.transform.localRotation;
            pickaxe.transform.localScale = _pickaxeTemplate.transform.localScale;
            combatAxe.transform.localPosition = _combatAxeTemplate.transform.localPosition;
            combatAxe.transform.localRotation = _combatAxeTemplate.transform.localRotation;
            combatAxe.transform.localScale = _combatAxeTemplate.transform.localScale;
            staff.transform.localPosition = _staffTemplate.transform.localPosition;
            staff.transform.localRotation = _staffTemplate.transform.localRotation;
            staff.transform.localScale = _staffTemplate.transform.localScale;
            crossbow.transform.localPosition = _crossbowTemplate.transform.localPosition;
            crossbow.transform.localRotation = _crossbowTemplate.transform.localRotation;
            crossbow.transform.localScale = _crossbowTemplate.transform.localScale;
            sword.SetActive(false);
            axe.SetActive(false);
            pickaxe.SetActive(false);
            combatAxe.SetActive(false);
            staff.SetActive(false);
            crossbow.SetActive(false);
            GameObject shield = null;
            Transform left = instance.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "handslot.l");
            if (_shieldTemplate != null && left != null)
            {
                shield = Instantiate(_shieldTemplate, left);
                shield.name = "Held Shield";
                shield.transform.localPosition = _shieldTemplate.transform.localPosition;
                shield.transform.localRotation = _shieldTemplate.transform.localRotation;
                shield.transform.localScale = _shieldTemplate.transform.localScale;
                shield.SetActive(false);
            }
            var driver = instance.AddComponent<CharacterAnimationDriver>();
            driver.ConfigurePlayerFrom(_rogueDriver, movement, combat, sword, axe, pickaxe,
                combatAxe, staff, crossbow, shield);
            instance.AddComponent<ShieldGuardPose>().Bind(combat);
            rogueVisual.SetActive(false);
            instance.SetActive(true);
            Renderer body = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
            movement.SetBodyRenderer(body != null ? body :
                instance.GetComponentInChildren<Renderer>(true));
            _currentVisual = instance;
            _currentId = id;
            lantern?.AttachToVisual(instance.transform);
            return true;
        }

        public GameObject CreatePreview(string appearanceId, Transform parent)
        {
            LookOption option = Option(CharacterLooks.SupportedOrRogue(appearanceId));
            if (option == null || parent == null) return null;
            GameObject instance = BuildModel(option, parent, false);
            if (instance != null)
            {
                SetLayerRecursively(instance.transform, 5); // Built-in UI layer; preview camera alone sees it.
                instance.GetComponent<Animator>().updateMode = AnimatorUpdateMode.UnscaledTime;
                instance.SetActive(true);
            }
            return instance;
        }

        LookOption Option(string id) => looks.FirstOrDefault(value => value != null && value.id == id);

        GameObject BuildModel(LookOption option, Transform parent, bool playerModel)
        {
            if (option.model == null || option.avatar == null || option.material == null) return null;
            GameObject instance = Instantiate(option.model, parent);
            instance.SetActive(false);
            instance.name = "KayKit " + CharacterLooks.Label(option.id) + (playerModel ? "" : " Preview");
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = rogueVisual.transform.localScale;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = Enumerable.Repeat(option.material,
                    Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = playerController;
            animator.avatar = option.avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return instance;
        }

        static Transform FindRightHand(Transform root) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "handslot.r");

        static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }
    }
}
