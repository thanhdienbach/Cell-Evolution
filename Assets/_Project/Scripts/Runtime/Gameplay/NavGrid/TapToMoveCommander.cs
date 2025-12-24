using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CellEvol.GamePlay
{
    [DisallowMultipleComponent]
    public sealed class TapToMoveCommander2D : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GridNav2D navGrid;

        [Header("Masks")]
        [SerializeField] private LayerMask cellMask;   // Layer của Cell
        [SerializeField] private LayerMask groundMask; // Layer của Ground

        private Camera _cam;
        private CellAgentPath2D _selected;

        private void Awake()
        {
            _cam = Camera.main;
            if (navGrid == null) navGrid = FindObjectOfType<GridNav2D>();
        }

        private void Update()
        {
            if (!PointerDownThisFrame(out int pointerId)) return;

            // Không nhận input khi bấm UI
            if (IsPointerOverUI(pointerId)) return;

            Vector2 world = ScreenToWorld(GetPointerPosition());

            // 1) Tap cell => select
            if (TryPickCell(world, out var cell))
            {
                _selected = cell;
                return;
            }

            // 2) Tap ground => move
            if (_selected != null && TryPickGround(world, out var point))
            {
                _selected.SetDestination(point);
            }
        }

        // ===== Input System =====

        private static bool PointerDownThisFrame(out int pointerId)
        {
            pointerId = -1;

            if (Touchscreen.current != null)
            {
                var t = Touchscreen.current.primaryTouch;
                if (t.press.wasPressedThisFrame)
                {
                    pointerId = t.touchId.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerId = -1; // mouse
                return true;
            }

            return false;
        }

        private static Vector2 GetPointerPosition()
        {
            if (Touchscreen.current != null)
            {
                var t = Touchscreen.current.primaryTouch;
                if (t.press.isPressed) return t.position.ReadValue();
            }

            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            return Vector2.zero;
        }

        private static bool IsPointerOverUI(int pointerId)
        {
            if (EventSystem.current == null) return false;

            // mouse
            if (Mouse.current != null) return EventSystem.current.IsPointerOverGameObject();

            // touch
            if (pointerId >= 0) return EventSystem.current.IsPointerOverGameObject(pointerId);

            return false;
        }

        // ===== Picking =====

        private bool TryPickCell(Vector2 world, out CellAgentPath2D cell)
        {
            var col = Physics2D.OverlapPoint(world, cellMask);
            if (col != null && col.TryGetComponent(out cell)) return true;

            cell = null;
            return false;
        }

        private bool TryPickGround(Vector2 world, out Vector2 point)
        {
            var col = Physics2D.OverlapPoint(world, groundMask);
            if (col != null)
            {
                point = world;
                return true;
            }

            point = default;
            return false;
        }

        private Vector2 ScreenToWorld(Vector2 screen)
        {
            var w = _cam.ScreenToWorldPoint(screen);
            return new Vector2(w.x, w.y);
        }
    }
}




