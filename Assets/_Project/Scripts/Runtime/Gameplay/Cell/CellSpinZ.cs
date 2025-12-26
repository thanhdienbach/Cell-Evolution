using UnityEngine;


namespace CellEvol.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CellSpinZ : MonoBehaviour
    {
        [Header("Spin")]
        [SerializeField, Tooltip("Degrees per second. Positive = clockwise (in 2D view) depending on camera.")]
        private float degressPerSecond = -45f;

        [SerializeField, Tooltip("Use unscaled time (ignores Time.timeScale)")]
        private bool useUnScaleTime = false;

        private void Update()
        {
            float dt = useUnScaleTime ? Time.unscaledDeltaTime : Time.deltaTime;
            transform.Rotate(0f, 0f, degressPerSecond * dt, Space.Self);
        }
    }
}
