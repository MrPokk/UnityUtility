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
        public void Awake()
        {
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
            return new Vector2Int((int)(pos.x / SizeSell.x + pos.x / Offset.x), (int)(pos.y / SizeSell.y + pos.y / Offset.y));
        }
        public Vector2 GridToWorld(Vector2Int pos)
        {
            return pos * SizeSell + pos * Offset + PosStart;
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
