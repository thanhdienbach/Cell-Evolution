using UnityEngine;
using UnityEngine.EventSystems;


namespace CellEvol.UI
{
    [DisallowMultipleComponent]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Referent")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        [Header("Turning")]
        [Tooltip("If true, joystick auto-returns to center on release.")]
        [SerializeField] private bool snapBackOnRelease;

        public Vector2 Value { get; private set; }

        private float _radius;

        private void Reset()
        {
            background = transform as RectTransform;
        }

        private void Awake()
        {
            if (background == null) background = transform as RectTransform;

            if (background == null)
            {
                Debug.LogError($"{nameof(VirtualJoystick)}: Background is missing.", this);
                enabled = false;
                return;
            }

            if (handle == null)
            {
                Debug.LogError($"{nameof(VirtualJoystick)}: Handle is missing.", this);
                enabled = false;
                return;
            }

            var size = background.rect.size;
            _radius = Mathf.Min(size.x, size.y) * 0.5f;

            CenterHandle();
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);
        public void OnDrag(PointerEventData eventData)
        {
            if (!enabled) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out var localPos)) return;

            var clamped = Vector2.ClampMagnitude(localPos, _radius);

            handle.anchoredPosition = clamped;
            Value= clamped / _radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!enabled) return ;

            if (snapBackOnRelease)
            {
                CenterHandle();
            }
            else
            {
                Value = Vector2.zero;
            }
        }

        public void ForceCenter()
        {
            CenterHandle();
        }

        private void CenterHandle()
        {
            if ( handle != null ) handle.anchoredPosition = Vector2.zero;
            Value = Vector2.zero;
        }
    }
}
