using System.Collections;
using UnityEditor;
using UnityEngine;


namespace CellEvol.GamePlay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class CellBounceDeform2D : MonoBehaviour
    {
        [Header("Hard Collision Filter")]
        [SerializeField] private LayerMask hardMask = ~0;
        [Tooltip("Minimum relative collision speed to trigger bounce/deform.")]
        [SerializeField, Min(0f)] private float minImpactSpeed = 0.4f;

        [Header("Boundce")]
        [SerializeField, Min(0f)] private float bounceImpuse = 1.0f;
        [Tooltip("How bouncy the reflection is (1 = keep speed, <1 lose speed).")]
        [SerializeField, Range(0f, 1.2f)] private float restitution = 0.9f;
        [Tooltip("Clamp speed after bounce to avoid crazy velocities.")]
        [SerializeField, Min(0f)] private float maxSpeedAfterBounce = 12f;

        [Header("Deform (Shell Squash)")]
        [Tooltip("How strong the squash is (0.08 = subtle).")]
        [SerializeField, Range(0, 0.3f)] private float squashAmount = 0.10f;
        [Tooltip("Optional: apply deform on this visual child only (recommended). If null, uses this transform.")]
        [SerializeField] private Transform visual;

        [Header("Deform Multi-Phase")]
        [SerializeField, Min(1)] private int deformPhases = 4;
        [SerializeField, Min(0f)] private float totalDeformTime = 0.35f;
        [SerializeField, Range(0.1f, 0.95f)] private float phaseDecay = 0.55f;
        [SerializeField, Range(0f, 0.3f)] private float overshoot = 0.10f;


        private Rigidbody2D _rb;
        private Vector3 _visualBaseScale;
        private Coroutine _deformRoutine;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (visual == null ) visual = transform;

            _visualBaseScale = visual.localScale;
        }

        private void OnDisable()
        {
            if (visual != null) visual.localScale = _visualBaseScale;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsHard(collision.gameObject)) return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minImpactSpeed) return;

            ContactPoint2D cp = collision.GetContact(0);
            Vector2 nomal = cp.normal;
            Vector2 v = _rb.velocity;

            if (Vector2.Dot(v, nomal) > 0) return;

            Vector2 reflected = Vector2.Reflect(v, nomal) * restitution;

            if (bounceImpuse > 0)
            {
                Vector2 impuse = nomal * (bounceImpuse * Mathf.Clamp01(impactSpeed / 5));
                _rb.AddForce(impuse, ForceMode2D.Impulse);
            }

            _rb.velocity = ClampMagnitude(reflected, maxSpeedAfterBounce);

            StartDeform(nomal);
        }

        private bool IsHard(GameObject go)
        {
            return (hardMask.value & (1 << go.layer)) != 0;
        }
        private static Vector2 ClampMagnitude(Vector2 v, float max)
        {
            if (max <= 0f) return v;
            float sqr = v.sqrMagnitude;
            if (sqr <= max * max) return v;
            return v.normalized * max;
        }
        private void StartDeform(Vector2 worldNomal)
        {
            if (visual == null) return;

            Vector3 localN = visual.InverseTransformDirection(worldNomal.normalized);
            Vector2 n2 = new Vector2(localN.x, localN.y);
            if (n2.sqrMagnitude < 0.0001f) n2 = Vector2.up;
            n2.Normalize();

            if (_deformRoutine != null) StopCoroutine(_deformRoutine);
            _deformRoutine = StartCoroutine(DeformRoutine(n2));
        }

        private IEnumerator DeformRoutine(Vector2 localNormal)
        {
            float ax = Mathf.Abs(localNormal.x);
            float ay = Mathf.Abs(localNormal.y);

            int phases = Mathf.Max(1, deformPhases);
            float phaseTime = (totalDeformTime > 0f) ? (totalDeformTime / phases) : 0.08f;

            Vector3 baseScale = _visualBaseScale;

            // amplitude mỗi pha giảm dần
            float amp = squashAmount;

            for (int i = 0; i < phases; i++)
            {
                // Target squash theo normal
                float k = amp;

                float squashX = Mathf.Lerp(1f, 1f - k, ax);
                float squashY = Mathf.Lerp(1f, 1f - k, ay);

                float expandX = Mathf.Lerp(1f + k * 0.6f, 1f, ax);
                float expandY = Mathf.Lerp(1f + k * 0.6f, 1f, ay);

                Vector3 squashTarget = new Vector3(
                    baseScale.x * squashX * expandX,
                    baseScale.y * squashY * expandY,
                    baseScale.z
                );

                // Overshoot (bật lại nhẹ theo hướng ngược)
                float ok = k * overshoot;
                Vector3 overshootTarget = new Vector3(
                    baseScale.x * (1f + ok),
                    baseScale.y * (1f + ok),
                    baseScale.z
                );

                // Chia thời gian pha thành 3 đoạn: squash -> overshoot -> return
                float t1 = phaseTime * 0.45f;
                float t2 = phaseTime * 0.25f;
                float t3 = phaseTime * 0.30f;

                yield return LerpScale(visual.localScale, squashTarget, t1);
                yield return LerpScale(visual.localScale, overshootTarget, t2);
                yield return LerpScale(visual.localScale, baseScale, t3);

                amp *= phaseDecay; // giảm biên độ cho pha sau
            }

            // đảm bảo về đúng base
            visual.localScale = baseScale;
            _deformRoutine = null;
        }
        private IEnumerator LerpScale(Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                visual.localScale = to;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                // Smoothstep for nicer feel.
                a = a * a * (3f - 2f * a);
                visual.localScale = Vector3.LerpUnclamped(from, to, a);
                yield return null;
            }

            visual.localScale = to;
        }
    }
}
