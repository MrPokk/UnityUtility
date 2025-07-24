using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Utility.Grid
{
    public class Grid<T>
    {
        private Dictionary<Vector2Int, T> _values;
        private Vector2Int _size;
        public int MaxCount { private set; get; }
        public T this[Vector2Int pos]
        {
            get
            {
                return _values[pos];
            }
            set
            {
                _values[pos] = value;
            }
        }
        public Grid(Vector2Int size)
        {
            Init(size);
        }

        public void Init(Vector2Int size)
        {
            if (size.x <= 0) throw new Exception("The size of the grid in x cannot be < 1");
            if (size.y <= 0) throw new Exception("The size of the grid in y cannot be < 1");
            _size = size;
            MaxCount = size.x * size.y;
            _values = new();
        }
        public void Clear() => _values.Clear();
        public int Count => _values.Count;
        public Vector2Int Size => _size;

        public bool CheckInSize(Vector2Int pos)
        {
            return pos.x >= 0 && pos.y >= 0 && pos.x < _size.x && pos.y < _size.y;
        }

        public bool Add(T value, Vector2Int pos)
        {
            if (CheckInSize(pos) && !_values.ContainsKey(pos))
            {
                _values.Add(pos, value);
                return true;
            }
            return false;
        }
        public bool AddNearest(T value)
        {
            if (Count == MaxCount) return false;
            Vector2Int pos = new();
            for (pos.y = 0; pos.y < _size.y; pos.y++)
                for (pos.x = 0; pos.x < _size.x; pos.x++)
                {
                    if (!IsContains(pos))
                    {
                        this[pos] = value;
                        return true;
                    }
                }
            return false;
        }
        /// <summary>
        /// Warning: not optimized, as it is not cached between operations.
        /// </summary>
        public bool AddRandomPos(T value)
        {
            if (Count == MaxCount) return false;
            int count = 0;
            Vector2Int[] allPos = new Vector2Int[MaxCount-Count];
            Vector2Int pos = new();
            for (pos.y = 0; pos.y < _size.y; pos.y++)
                for (pos.x = 0; pos.x < _size.x; pos.x++)
                {
                    if (!IsContains(pos))
                    {
                        allPos[count] = pos;
                        count++;
                    }
                }

            this[allPos[Random.Range(0, count)]] = value;
            return true;
        }
        public bool Set(T value, Vector2Int pos)
        {
            if (CheckInSize(pos))
            {
                this[pos] = value;
                return true;
            }
            return false;
        }

        public bool Remove(Vector2Int pos)
        {
            return _values.Remove(pos);
        }

        public bool IsContains(Vector2Int pos)
        {
            return _values.ContainsKey(pos);
        }
        public bool TryGetAtPos(Vector2Int pos, out T value)
        {
            if (_values.TryGetValue(pos, out value))
                return true;
            return false;
        }
    }
}
