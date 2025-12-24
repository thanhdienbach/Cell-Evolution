using System.Collections.Generic;
using UnityEngine;

namespace CellEvol.GamePlay
{
    [DisallowMultipleComponent]
    public sealed class GridNav2D : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private Vector2 gridWorldSize = new Vector2(40f, 24f);
        [SerializeField, Min(0.1f)] private float cellSize = 0.5f;

        [Header("Obstacle")]
        [SerializeField] private LayerMask obstacleMask;          // e.g. Hard/Wall ONLY
        [SerializeField, Min(0f)] private float agentRadius = 0.25f;

        [Header("Build")]
        [SerializeField] private bool autoRebuildOnStart = true;

        private int _gridX, _gridY;
        private Node[,] _nodes;
        private Vector2 _origin; // bottom-left

        private sealed class Node
        {
            public bool walkable;
            public Vector2 world;
            public int gx, gy;

            public int gCost;
            public int hCost;
            public Node parent;

            public int fCost => gCost + hCost;

            // fast reset per search
            public int searchId;
        }

        private int _searchId = 1;

        private void Awake()
        {
            if (autoRebuildOnStart) BuildGrid();
        }

        public void BuildGrid()
        {
            _gridX = Mathf.CeilToInt(gridWorldSize.x / cellSize);
            _gridY = Mathf.CeilToInt(gridWorldSize.y / cellSize);

            _nodes = new Node[_gridX, _gridY];

            _origin = (Vector2)transform.position - gridWorldSize * 0.5f;

            for (int x = 0; x < _gridX; x++)
            {
                for (int y = 0; y < _gridY; y++)
                {
                    Vector2 world = _origin + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                    bool blocked = Physics2D.OverlapCircle(world, agentRadius, obstacleMask) != null;

                    _nodes[x, y] = new Node
                    {
                        gx = x,
                        gy = y,
                        world = world,
                        walkable = !blocked
                    };
                }
            }
        }

        public bool TryFindPath(Vector2 startWorld, Vector2 targetWorld, List<Vector2> outPath)
        {
            outPath.Clear();
            if (_nodes == null) BuildGrid();

            Node start = WorldToNode(startWorld);
            Node target = WorldToNode(targetWorld);

            if (start == null || target == null) return false;

            if (!start.walkable) start = FindNearestWalkable(start);
            if (!target.walkable) target = FindNearestWalkable(target);

            if (start == null || target == null) return false;

            _searchId++;
            if (_searchId == int.MaxValue) _searchId = 1;

            var open = new List<Node>(256);
            var openSet = new HashSet<Node>();
            var closed = new HashSet<Node>();

            ResetNodeForSearch(start);
            start.gCost = 0;
            start.hCost = Heuristic(start, target);
            start.parent = null;

            open.Add(start);
            openSet.Add(start);

            while (open.Count > 0)
            {
                Node current = GetLowestFCost(open);
                open.Remove(current);
                openSet.Remove(current);
                closed.Add(current);

                if (current == target)
                {
                    Retrace(start, target, outPath);
                    return true;
                }

                foreach (var neigh in GetNeighborsNoCornerCut(current))
                {
                    if (!neigh.walkable || closed.Contains(neigh)) continue;

                    ResetNodeForSearch(neigh);

                    int newG = current.gCost + StepCost(current, neigh);
                    if (!openSet.Contains(neigh) || newG < neigh.gCost)
                    {
                        neigh.gCost = newG;
                        neigh.hCost = Heuristic(neigh, target);
                        neigh.parent = current;

                        if (!openSet.Contains(neigh))
                        {
                            open.Add(neigh);
                            openSet.Add(neigh);
                        }
                    }
                }
            }

            return false;
        }

        private void ResetNodeForSearch(Node n)
        {
            if (n.searchId == _searchId) return;
            n.searchId = _searchId;
            n.gCost = int.MaxValue / 4;
            n.hCost = 0;
            n.parent = null;
        }

        private Node WorldToNode(Vector2 world)
        {
            Vector2 local = world - _origin;
            int x = Mathf.FloorToInt(local.x / cellSize);
            int y = Mathf.FloorToInt(local.y / cellSize);

            if (x < 0 || y < 0 || x >= _gridX || y >= _gridY) return null;
            return _nodes[x, y];
        }

        private Node FindNearestWalkable(Node from)
        {
            var q = new Queue<Node>();
            var seen = new HashSet<Node>();

            q.Enqueue(from);
            seen.Add(from);

            while (q.Count > 0)
            {
                var n = q.Dequeue();
                if (n.walkable) return n;

                foreach (var nb in GetNeighborsAll(n))
                    if (seen.Add(nb)) q.Enqueue(nb);
            }

            return null;
        }

        // 8 hướng nhưng CHỐNG đi chéo xuyên góc:
        // Nếu đi chéo (dx,dy) thì 2 ô thẳng kề phải walkable
        private IEnumerable<Node> GetNeighborsNoCornerCut(Node n)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int x = n.gx + dx;
                    int y = n.gy + dy;
                    if (x < 0 || y < 0 || x >= _gridX || y >= _gridY) continue;

                    Node nb = _nodes[x, y];

                    if (dx != 0 && dy != 0)
                    {
                        Node sideA = _nodes[n.gx + dx, n.gy];
                        Node sideB = _nodes[n.gx, n.gy + dy];
                        if (!sideA.walkable || !sideB.walkable) continue;
                    }

                    yield return nb;
                }
            }
        }

        private IEnumerable<Node> GetNeighborsAll(Node n)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int x = n.gx + dx;
                    int y = n.gy + dy;
                    if (x < 0 || y < 0 || x >= _gridX || y >= _gridY) continue;
                    yield return _nodes[x, y];
                }
        }

        private static int Heuristic(Node a, Node b)
        {
            int dx = Mathf.Abs(a.gx - b.gx);
            int dy = Mathf.Abs(a.gy - b.gy);
            int diag = Mathf.Min(dx, dy);
            int straight = Mathf.Abs(dx - dy);
            return diag * 14 + straight * 10;
        }

        private static int StepCost(Node a, Node b)
        {
            int dx = Mathf.Abs(a.gx - b.gx);
            int dy = Mathf.Abs(a.gy - b.gy);
            return (dx + dy == 1) ? 10 : 14;
        }

        private static Node GetLowestFCost(List<Node> list)
        {
            Node best = list[0];
            for (int i = 1; i < list.Count; i++)
            {
                var n = list[i];
                int bf = best.fCost;
                int nf = n.fCost;
                if (nf < bf || (nf == bf && n.hCost < best.hCost))
                    best = n;
            }
            return best;
        }

        private static void Retrace(Node start, Node end, List<Vector2> outPath)
        {
            outPath.Clear();
            Node cur = end;

            while (cur != null && cur != start)
            {
                outPath.Add(cur.world);
                cur = cur.parent;
            }

            outPath.Reverse();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transform.position, gridWorldSize);

            if (_nodes == null) return;

            for (int x = 0; x < _gridX; x++)
                for (int y = 0; y < _gridY; y++)
                {
                    Gizmos.color = _nodes[x, y].walkable ? new Color(1, 1, 1, 0.05f) : new Color(1, 0, 0, 0.08f);
                    Gizmos.DrawCube(_nodes[x, y].world, Vector3.one * (cellSize * 0.95f));
                }
        }
#endif
    }
}




