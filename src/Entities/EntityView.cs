using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.Views
{
    public abstract class EntityView : MonoBehaviour
    {
        [SerializeField, ReadOnly] private uint _entityId;

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