using UnityEngine;

namespace CellEvol.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class KeepUprightZ : MonoBehaviour
    {
        [Header("Upright")]
        [SerializeField, Tooltip("If set, keeps this object upright relative to this reference (usually the parent).")]
        private Transform reference;

        [SerializeField, Tooltip("Look local Z rotation to 0.")]
        private bool lockLocalZ = true;

        private void Reset()
        {
            reference = transform.parent;
        }
        private void Awake()
        {
            if (reference == null) reference = transform.parent;
        }
        private void LateUpdate()
        {
            if (reference == null) return;
            if (lockLocalZ)
            {
                transform.localRotation = Quaternion.Inverse(reference.rotation);
                return;
            }
        }
    }
}
