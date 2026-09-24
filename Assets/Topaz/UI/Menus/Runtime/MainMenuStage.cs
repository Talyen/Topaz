using Topaz.AnimationStudy;
using Topaz.LoopStudy;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Topaz.Menus
{
    /// <summary>Dedicated full-screen menu camera over a small authored campfire stage.</summary>
    public sealed class MainMenuStage : MonoBehaviour
    {
        [SerializeField] Camera menuCamera;
        [SerializeField] Camera gameplayCamera;
        [SerializeField] UniversalAdditionalCameraData menuCameraData;
        [SerializeField] UniversalAdditionalCameraData gameplayCameraData;
        [SerializeField] Transform volumeTrigger;
        [SerializeField] Transform actorAnchor;
        [SerializeField] PlayerAppearance appearance;

        GameObject _actor;
        string _lookId;
        float _yaw;

        public GameObject CurrentModel => _actor;
        public string CurrentLookId => _lookId;

        void Awake()
        {
            if (menuCamera == null || gameplayCamera == null || menuCameraData == null ||
                gameplayCameraData == null || actorAnchor == null || appearance == null ||
                volumeTrigger == null)
            {
                Debug.LogError("Main menu stage references are incomplete.", this);
                enabled = false;
                return;
            }
            menuCamera.enabled = false;
        }

        void LateUpdate()
        {
            if (menuCamera != null && menuCamera.enabled) SyncEffects();
        }

        public void SetVisible(bool visible)
        {
            if (!enabled) return;
            if (visible && !gameObject.activeSelf) gameObject.SetActive(true);
            if (visible) SyncEffects();
            menuCamera.enabled = visible;
            gameplayCamera.enabled = !visible;
            if (!visible && gameObject.activeSelf) gameObject.SetActive(false);
        }

        public void ShowLook(string lookId)
        {
            if (!enabled) return;
            string id = CharacterLooks.SupportedOrRogue(lookId);
            if (_actor != null && _lookId == id) return;
            if (_actor != null)
            {
                _actor.SetActive(false);
                Destroy(_actor);
            }
            _actor = appearance.CreatePreview(id, actorAnchor);
            if (_actor == null)
            {
                _actor = appearance.CreatePreview(CharacterLooks.Rogue, actorAnchor);
                id = CharacterLooks.Rogue;
            }
            _lookId = id;
            if (_actor == null) return;
            _actor.transform.localScale *= 1.45f;
            ApplyRotation();
        }

        public void ResetRotation()
        {
            _yaw = 0f;
            ApplyRotation();
        }

        public void Rotate()
        {
            _yaw = (_yaw + 45f) % 360f;
            ApplyRotation();
        }

        void ApplyRotation()
        {
            if (_actor != null)
                _actor.transform.localRotation = Quaternion.Euler(0f, 180f + _yaw, 0f);
        }

        void SyncEffects()
        {
            menuCameraData.renderPostProcessing = gameplayCameraData.renderPostProcessing;
            menuCameraData.antialiasing = gameplayCameraData.antialiasing;
            menuCameraData.antialiasingQuality = gameplayCameraData.antialiasingQuality;
            menuCameraData.volumeLayerMask = gameplayCameraData.volumeLayerMask;
            menuCameraData.volumeTrigger = volumeTrigger;
            menuCameraData.dithering = gameplayCameraData.dithering;
            menuCamera.allowHDR = gameplayCamera.allowHDR;
            menuCamera.allowMSAA = gameplayCamera.allowMSAA;
        }
    }
}
