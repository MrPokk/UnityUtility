using UnityEngine;
using Utility.Grid;
public class ObjectGridMono : GridMonoBehaviour<MonoBehaviour>
{
    public bool AutoPos = true;
    private void SetPosObj(MonoBehaviour value, Vector2Int pos, bool res)
    {
        if (res && AutoPos)
        {
            value.transform.position = GridToWorldCentre(pos);
        }
    }

    public void Clear()
    {
        foreach (MonoBehaviour obj in Grid.GetDictionary().Values)
        {
            Destroy(obj);
        }
        Grid.Clear();
    }

    public bool Add(MonoBehaviour value, Vector2Int pos) => Add(value, pos, AutoPos);
    public bool Add(MonoBehaviour value, Vector2Int pos, bool setPos)
    {
        bool res = Grid.Add(value, pos);
        SetPosObj(value, pos, res); 
        return res;
    }

    public bool AddNearest(MonoBehaviour value) => AddNearest(value, out Vector2Int _null);
    public bool AddNearest(MonoBehaviour value, out Vector2Int pos)
    {
        bool res = Grid.AddNearest(value, out pos);
        SetPosObj(value, pos, res);
        return res;
    }

    public bool AddRandomPos(MonoBehaviour value) => AddRandomPos(value, out Vector2Int _null);
    public bool AddRandomPos(MonoBehaviour value, out Vector2Int pos)
    {
        bool res = Grid.AddRandomPos(value, out pos);
        SetPosObj(value, pos, res);
        return res;
    }

    public bool Remove(Vector2Int pos) => Grid.Remove(pos);
    public bool Delete(Vector2Int pos)
    {
        bool res = Grid.TryGetAtPos(pos, out MonoBehaviour value);
        if (res)
        {
            Destroy(value);
            Grid.Remove(pos);
        }
        return res;
    }
    public bool TryGetAtPos(Vector2Int pos, out MonoBehaviour value) => Grid.TryGetAtPos(pos, out value);
}
