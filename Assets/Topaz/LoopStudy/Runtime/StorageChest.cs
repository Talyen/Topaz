using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>One reusable chest object whose contents live in the save record.</summary>
    public sealed class StorageChest : MonoBehaviour
    {
        [SerializeField] StructureDefinition definition;
        [SerializeField] Renderer chestRenderer;

        StructureStateRecord _state;

        public bool IsPlaced => _state != null;
        public int WoodStored => _state?.woodStored ?? 0;
        public int WoodCapacity => definition != null ? definition.WoodCapacity : 0;
        public StructureStateRecord State => _state;

        public void Bind(StructureStateRecord state)
        {
            _state = state;
            if (state == null)
            {
                gameObject.SetActive(false);
                return;
            }
            transform.position = new Vector3(state.x, 0f, state.z);
            gameObject.SetActive(true);
        }

        public int Deposit(int offered)
        {
            if (_state == null || offered <= 0) return 0;
            int accepted = Mathf.Min(offered, WoodCapacity - _state.woodStored);
            _state.woodStored += accepted;
            return accepted;
        }

        public int WithdrawAll()
        {
            if (_state == null) return 0;
            int count = _state.woodStored;
            _state.woodStored = 0;
            return count;
        }
    }
}
