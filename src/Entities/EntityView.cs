using UnityEngine;

#if USING_NFGO
    using Unity.Netcode;
#endif

namespace npg.tomatoecs.Editor
{
    public abstract class EntityView
#if USING_NFGO
        : NetworkBehaviour
#else
        : MonoBehaviour
#endif
    {
        [SerializeField] private uint _entityId;

        private Transform _transform;
        public uint EntityId => _entityId;
        public Vector3 Position => _transform.position;
        public Vector3 LocalPosition => _transform.localPosition;
        public Transform Transform => _transform;

        public void SetupData(uint id)
        {
            _transform = transform;
            _entityId = id;
        }

        public abstract void Dispose();
    }
}