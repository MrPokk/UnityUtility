using UnityEngine;

namespace Utility.Grid
{
    public abstract class GridMonoBehaviour <T> : MonoBehaviour
    {
        public Grid<T> Grid { get; private set; }
        public Vector2Int Size;
        public Vector2 SizeSell;
        public Vector2 Offset;
        protected virtual Vector2 PosStart => transform.position;

#if UNITY_EDITOR
        [SerializeField]
        private bool _Draw = true;
        [SerializeField]
        private Color _Color = Color.white;
        [SerializeField]
        private bool _DrawFont = false;
        [SerializeField]
        private Color _ColorFont = Color.white;
        [SerializeField]
        private int _SizeFont = 20;
#endif
        public void Awake()
        {
            Grid = new Grid<T>(Size);
            Init();
        }
        public virtual void Init() => Grid.Init(Size);
        public Vector2Int MouseToGrid()
        {
            return WorldToGrid(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        }
        public Vector2Int WorldToGrid(Vector2 pos)
        {
            pos -= PosStart;
            return new Vector2Int(Mathf.FloorToInt(pos.x / (SizeSell.x + Offset.x)), Mathf.FloorToInt(pos.y / (SizeSell.y + Offset.y)));
        }
        public Vector2 GridToWorld(Vector2Int pos)
        {
            return pos * SizeSell + pos * Offset + PosStart;
        }
        /// <summary>
        /// Warning: method uses TryWorldToGrid
        /// </summary>
        public bool IsContains(Vector2 pos)
        {
            return TryWorldToGrid(pos, out Vector2Int _null);
        }
        public bool TryMouseToGrid(out Vector2Int grid)
        {
            return TryWorldToGrid(Camera.main.ScreenToWorldPoint(Input.mousePosition), out grid);
        }
        public bool TryWorldToGrid(Vector2 pos, out Vector2Int grid)
        {
            IsContains(pos);
            grid = WorldToGrid(pos);
            if (grid.x < 0 || grid.y < 0 || grid.x >= Grid.Size.x || grid.y >= Grid.Size.y) return false;
            if (Offset == Vector2.zero) return true;
            else
            {
                pos -= GridToWorld(grid);
                if (Offset.x > 0 && pos.x > SizeSell.x) return false;
                if (Offset.y > 0 && pos.y > SizeSell.y) return false;
            }
            return true;
        }
#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (!_Draw) return;
            GUIStyle style = new()
            {
                normal = { textColor = _ColorFont },
                fontSize = (int)(Mathf.Min(SizeSell.x, SizeSell.y) * _SizeFont),
                fixedHeight = SizeSell.y,
                fixedWidth = SizeSell.x,
                alignment = TextAnchor.MiddleCenter,
            };

            Gizmos.color = _Color;
            Vector2Int select = new();
            for (select.y = 0; select.y < Size.y; select.y++)
                for (select.x = 0; select.x < Size.x; select.x++)
                {
                    Vector2 point = GridToWorld(select);

                    Vector2 topLeft = point + new Vector2(0, SizeSell.y);
                    Vector2 topRight = point + SizeSell;
                    Vector2 bottomRight = point + new Vector2(SizeSell.x, 0);
                    Vector2 bottomLeft = point;

                    Gizmos.DrawLine(topLeft, topRight);
                    Gizmos.DrawLine(topRight, bottomRight);
                    Gizmos.DrawLine(bottomRight, bottomLeft);
                    Gizmos.DrawLine(bottomLeft, topLeft);

                    if (_DrawFont)
                    {
                        UnityEditor.Handles.Label(point + SizeSell / 2, $"[{select.x},{select.y}]", style);
                    }
                }
        }
#endif
    }
}
