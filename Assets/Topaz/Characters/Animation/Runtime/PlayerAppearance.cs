using System;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using Topaz.LoopStudy;
using UnityEngine;

namespace Topaz.AnimationStudy
{
    /// <summary>Swaps a configured Topaz character visual prefab; the player object owns gameplay.</summary>
    public sealed class PlayerAppearance : MonoBehaviour
    {
        [Serializable]
        public sealed class LookOption
        {
            public string id;
            public GameObject model;
            // Legacy extraction inputs; runtime appearance comes entirely from the prefab.
            [HideInInspector] public Avatar avatar;
            [HideInInspector] public Material material;
        }

        [SerializeField] Transform visualRoot;
        [SerializeField] GameObject rogueVisual;
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] PlayerLantern lantern;
        [SerializeField] PlayerCombat combat;
        [SerializeField, HideInInspector] RuntimeAnimatorController playerController;
        [SerializeField] LookOption[] looks;

        GameObject _currentVisual;
        string _currentId;
        public string CurrentId => _currentId;

        public void ShowShieldImpact()
        {
            GameObject visual = _currentVisual != null ? _currentVisual : rogueVisual;
            visual?.GetComponentInChildren<ShieldGuardPose>(true)?.Impact();
        }

        void Awake()
        {
            if (rogueVisual == null || visualRoot == null || movement == null || combat == null ||
                looks == null || looks.Length == 0)
            {
                Debug.LogError("Player appearance references are incomplete.", this);
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
            GameObject replacement = id == CharacterLooks.Rogue ? rogueVisual :
                BuildModel(option, visualRoot, true);
            CharacterVisual bindings = replacement != null ?
                replacement.GetComponentInChildren<CharacterVisual>(true) : null;
            if (bindings == null || !bindings.HasPlayerBindings)
            {
                if (replacement != null && replacement != rogueVisual) Destroy(replacement);
                Debug.LogError("Selected character has incomplete visual bindings: " + id, this);
                return false;
            }
            if (_currentVisual != null && _currentVisual != replacement)
            {
                _currentVisual.SetActive(false);
                Destroy(_currentVisual);
            }
            rogueVisual.SetActive(replacement == rogueVisual);
            replacement.SetActive(true);
            ShieldGuardPose guard = replacement.GetComponentInChildren<ShieldGuardPose>(true);
            guard?.Bind(combat);
            movement.SetBodyRenderer(bindings.BodyRenderer);
            lantern?.AttachToVisual(replacement.transform);
            _currentVisual = replacement == rogueVisual ? null : replacement;
            _currentId = id;
            return true;
        }

        public GameObject CreatePreview(string appearanceId, Transform parent)
        {
            LookOption option = Option(CharacterLooks.SupportedOrRogue(appearanceId));
            if (option == null || parent == null) return null;
            GameObject instance = BuildModel(option, parent, false);
            if (instance != null)
            {
                SetLayerRecursively(instance.transform, 5);
                foreach (CharacterAnimationDriver driver in instance.GetComponentsInChildren<CharacterAnimationDriver>(true))
                    driver.enabled = false;
                foreach (ShieldGuardPose guard in instance.GetComponentsInChildren<ShieldGuardPose>(true))
                    guard.enabled = false;
                instance.GetComponentInChildren<CharacterVisual>(true).Animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                instance.SetActive(true);
            }
            return instance;
        }

        LookOption Option(string id) => looks.FirstOrDefault(value => value != null && value.id == id);

        GameObject BuildModel(LookOption option, Transform parent, bool playerModel)
        {
            if (option.model == null) return null;
            GameObject instance = Instantiate(option.model, parent);
            instance.name = CharacterLooks.Label(option.id) + (playerModel ? "" : " Preview");
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = rogueVisual.transform.localScale;
            return instance;
        }

        static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root) SetLayerRecursively(child, layer);
        }
    }
}
