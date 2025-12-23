using UnityEngine;
using CellEvol.UI;
using static UnityEditor.PlayerSettings;


namespace CellEvol.System
{

    [DisallowMultipleComponent]
    public class CameraPanZoom : MonoBehaviour
    {
        [Header("Referent")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private VirtualJoystick joystick;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 6.0f;
        [SerializeField] private bool clampToBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-20f, -12f);
        [SerializeField] private Vector2 maxBounds = new Vector2(20f, 12f);

        [Header("Zoom (Orthograpic)")]
        [SerializeField] private float zoomStep = 0.5f;
        [SerializeField] private float minOrthoSize = 3.5f;
        [SerializeField] private float maxOrthoSize = 9.0f;


        private void Reset()
        {
            targetCamera = Camera.main;
        }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            if (targetCamera == null)
            {
                Debug.LogError($"{nameof(CameraPanZoom)}: targetCamera is missing.", this);
                enabled = false;
                return;
            }

            if (!targetCamera.orthographic)
            {
                Debug.LogWarning($"{nameof(CameraPanZoom)}: targetCamera is not Orthographic. Setting it now.", this);
                targetCamera.orthographic = true;
            }

            if (joystick == null)
            {
                Debug.LogError($"{nameof(CameraPanZoom)}: joystick reference is missing.", this);
                enabled = false;
                return;
            }

            if (minOrthoSize > maxOrthoSize)
            {
                (minOrthoSize, maxOrthoSize) = (maxOrthoSize, minOrthoSize);
            }

            targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize, minOrthoSize, maxOrthoSize);
        }

        private void Update()
        {
            Debug.Log(joystick.Value);
            if (!enabled) return;

            Vector2 input = joystick.Value;
            if (input.sqrMagnitude < 0.0001f) return;

            Vector3 delta = new Vector3(input.x, input.y, 0) * (panSpeed * Time.deltaTime);
            Vector3 pos = targetCamera.transform.position + delta;
            
            if (clampToBounds)
            {
                pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
                pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            }

            targetCamera.transform.position = pos;
        }

        public void ZoomIn()
        {
            if (!enabled) return;

            float size = targetCamera.orthographicSize - zoomStep;
            targetCamera.orthographicSize = Mathf.Clamp(size, minOrthoSize, maxOrthoSize);
        }
        public void ZoomOut()
        {
            if (!enabled) return;

            float size = targetCamera.orthographicSize + zoomStep;
            targetCamera.orthographicSize = Mathf.Clamp(size, minOrthoSize, maxOrthoSize);
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
        }
    }
}
