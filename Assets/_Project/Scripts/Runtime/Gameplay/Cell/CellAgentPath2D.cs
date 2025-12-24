using System.Collections.Generic;
using UnityEngine;

namespace CellEvol.GamePlay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class CellAgentPath2D : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GridNav2D navGrid;
        [SerializeField] private LayerMask obstacleMask; // Hard/Wall ONLY (không include Cell)

        [Header("Move")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0.01f)] private float stoppingDistance = 0.15f;

        [Header("Arrive (anti false-cancel)")]
        [SerializeField, Min(0f)] private float arriveSpeedThreshold = 0.25f;

        [Header("Repath")]
        [SerializeField, Min(0.05f)] private float repathInterval = 0.35f;

        [Header("Stuck (no progress)")]
        [SerializeField, Min(0.01f)] private float progressEpsilon = 0.01f;
        [SerializeField, Min(0.05f)] private float noProgressTimeToRepath = 0.35f;

        [Header("Avoid (slide)")]
        [SerializeField, Min(0.05f)] private float lookAhead = 0.6f;
        [SerializeField, Min(0f)] private float avoidStrength = 1.0f;

        [Header("Collision -> force repath")]
        [SerializeField, Min(0f)] private float forceRepathWindow = 0.15f;

        private Rigidbody2D _rb;
        private Collider2D _col;

        private bool _hasDestination;
        private Vector2 _destination;

        private readonly List<Vector2> _path = new();
        private int _pathIndex;

        private float _repathTimer;

        // no-progress tracking
        private float _lastDist;
        private float _noProgressTimer;

        // collision repath
        private float _forceRepathTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();

            if (navGrid == null) navGrid = FindObjectOfType<GridNav2D>();
        }

        public void SetDestination(Vector2 worldPos)
        {
            _destination = worldPos;
            _hasDestination = true;

            _lastDist = Vector2.Distance(_rb.position, _destination);
            _noProgressTimer = 0f;

            Repath(); // tính đường ngay
        }

        public void CancelDestination()
        {
            _hasDestination = false;
            _path.Clear();
            _pathIndex = 0;
            _rb.velocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (!_hasDestination) return;

            // Ep repath nếu vừa va chạm
            if (_forceRepathTimer > 0f)
            {
                _forceRepathTimer -= Time.fixedDeltaTime;
                Repath();
                _repathTimer = 0f;
            }

            // Chỉ arrive khi thật sự hợp lý (không cancel nhầm)
            if (ShouldArrive())
            {
                CancelDestination();
                return;
            }

            // repath theo chu kỳ
            _repathTimer += Time.fixedDeltaTime;
            if (_repathTimer >= repathInterval)
            {
                _repathTimer = 0f;
                RepathIfNeeded();
            }

            Vector2 desiredDir = GetDesiredDirection();
            desiredDir = ApplyAvoidance(desiredDir);

            if (desiredDir.sqrMagnitude < 0.0001f)
            {
                // Không clear destination, chỉ đứng lại chờ repath
                _rb.velocity = Vector2.zero;
                TrackNoProgress();
                return;
            }

            _rb.velocity = desiredDir.normalized * moveSpeed;

            TrackNoProgress();
        }

        private void TrackNoProgress()
        {
            float d = Vector2.Distance(_rb.position, _destination);

            if (d < _lastDist - progressEpsilon)
            {
                _lastDist = d;
                _noProgressTimer = 0f;
            }
            else
            {
                _noProgressTimer += Time.fixedDeltaTime;
                if (_noProgressTimer >= noProgressTimeToRepath)
                {
                    _noProgressTimer = 0f;
                    Repath();
                }
            }
        }

        private bool ShouldArrive()
        {
            if (Vector2.Distance(_rb.position, _destination) > stoppingDistance)
                return false;

            // nếu đích bị tường chắn, tuyệt đối không cancel
            if (!HasLineOfSight(_rb.position, _destination))
                return false;

            // nếu còn waypoint, để đi nốt
            if (_path.Count > 0 && _pathIndex < _path.Count)
                return false;

            // chỉ dừng hẳn khi gần như đứng yên
            return _rb.velocity.magnitude <= arriveSpeedThreshold;
        }

        private Vector2 GetDesiredDirection()
        {
            // đi theo waypoint
            if (_path.Count > 0 && _pathIndex < _path.Count)
            {
                Vector2 wp = _path[_pathIndex];

                if (Vector2.Distance(_rb.position, wp) <= stoppingDistance)
                    _pathIndex++;

                if (_pathIndex < _path.Count)
                    return (_path[_pathIndex] - _rb.position);
            }

            // fallback: đi thẳng tới đích
            return (_destination - _rb.position);
        }

        private void RepathIfNeeded()
        {
            // Nếu nhìn thấy đích (không bị tường chắn) thì bỏ path cho mượt
            if (HasLineOfSight(_rb.position, _destination))
            {
                _path.Clear();
                _pathIndex = 0;
                return;
            }

            // Nếu đang hết path thì tính lại
            if (_path.Count == 0 || _pathIndex >= _path.Count)
                Repath();
        }

        private void Repath()
        {
            if (navGrid == null) return;

            _path.Clear();
            _pathIndex = 0;

            // Nếu không tìm được path: vẫn giữ destination, dùng fallback + avoid + repath
            navGrid.TryFindPath(_rb.position, _destination, _path);
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            float r = GetApproxRadius();
            Vector2 dir = (to - from);
            float dist = dir.magnitude;
            if (dist <= 0.001f) return true;
            dir /= dist;

            RaycastHit2D hit = Physics2D.CircleCast(from, r, dir, dist, obstacleMask);
            return hit.collider == null;
        }

        private Vector2 ApplyAvoidance(Vector2 desired)
        {
            if (desired.sqrMagnitude < 0.0001f) return desired;

            float r = GetApproxRadius();
            Vector2 dir = desired.normalized;

            RaycastHit2D hit = Physics2D.CircleCast(_rb.position, r, dir, lookAhead, obstacleMask);
            if (hit.collider == null) return desired;

            // slide theo tường
            Vector2 n = hit.normal;
            Vector2 slide = Vector2.Perpendicular(n);
            if (Vector2.Dot(slide, dir) < 0) slide = -slide;

            return (dir + slide * avoidStrength);
        }

        private float GetApproxRadius()
        {
            if (_col is CircleCollider2D cc)
                return cc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

            return 0.25f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Bất kể va vào cái gì (tường/cell), ép repath 1 khoảng thời gian ngắn
            _forceRepathTimer = forceRepathWindow;
        }
    }
}


