using System.Collections.Generic;
using UnityEngine;

public class MonoGridPresenter<T> : GridPresenter<T> where T : MonoBehaviour
{
    public MonoGridPresenter(GridConfig gridConfig) : base(gridConfig)
    { }

    public bool InitializeGameObject(Vector2Int index, T prefab, Transform parent = null)
    {
        if (!IsWithinGrid(index) || prefab == null)
            return false;

        RemoveGameObject(index);

        var worldPosition = ConvertingPosition(index);
        var gameObject = Object.Instantiate(prefab, worldPosition, GetRotation(), parent);

        SetValueInGrid(index, gameObject);
        return true;
    }

    public bool InitializeGameObject(Vector3 worldPosition, T prefab, Transform parent = null)
    {
        var index = GetGridIndex(worldPosition);
        if (!IsWithinGrid(index))
            return false;

        return InitializeGameObject(index, prefab, parent);
    }

    public bool SwapGameObjects(Vector2Int indexA, Vector2Int indexB)
    {
        if (!SwapValues(indexA, indexB))
            return false;

        var gameObjectA = GetValue(indexA);
        var gameObjectB = GetValue(indexB);

        if (gameObjectA != null)
            gameObjectA.transform.position = ConvertingPosition(indexA);

        if (gameObjectB != null)
            gameObjectB.transform.position = ConvertingPosition(indexB);

        return true;
    }

    public bool SwapGameObjects(Vector3 worldPositionA, Vector3 worldPositionB)
    {
        var indexA = GetGridIndex(worldPositionA);
        var indexB = GetGridIndex(worldPositionB);
        return SwapGameObjects(indexA, indexB);
    }

    public bool MoveGameObject(Vector2Int fromIndex, Vector2Int toIndex, T fromValue)
    {
        if (!IsWithinGrid(fromIndex) || !IsWithinGrid(toIndex))
            return false;

        var gameObjectFrom = GetValue(fromIndex);
        var gameObjectTo = GetValue(toIndex);

        if (gameObjectFrom == null)
            return false;

        gameObjectFrom.transform.position = ConvertingPosition(toIndex);

        SetValueInGrid(fromIndex, fromValue);
        SetValueInGrid(toIndex, gameObjectFrom);

        return true;
    }

    public bool MoveGameObject(Vector2Int fromIndex, Vector2Int toIndex)
    {
        return MoveGameObject(fromIndex, toIndex, null);
    }

    public bool MoveGameObject(Vector3 fromWorldPosition, Vector3 toWorldPosition, T fromValue)
    {
        var fromIndex = GetGridIndex(fromWorldPosition);
        var toIndex = GetGridIndex(toWorldPosition);
        return MoveGameObject(fromIndex, toIndex, fromValue);
    }

    public bool MoveGameObject(Vector3 fromWorldPosition, Vector3 toWorldPosition)
    {
        return MoveGameObject(fromWorldPosition, toWorldPosition, null);
    }

    public bool RemoveGameObject(Vector2Int index)
    {
        if (!IsWithinGrid(index))
            return false;

        var gameObject = GetValue(index);
        if (gameObject != null)
        {
            Object.Destroy(gameObject.gameObject);
            SetValueInGrid(index, null);
            return true;
        }

        return false;
    }

    public bool RemoveGameObject(Vector3 worldPosition)
    {
        var index = GetGridIndex(worldPosition);
        return RemoveGameObject(index);
    }

    public T ExtractGameObject(Vector2Int index)
    {
        if (!IsWithinGrid(index))
            return null;

        var gameObject = GetValue(index);
        SetValueInGrid(index, null);
        return gameObject;
    }

    public T ExtractGameObject(Vector3 worldPosition)
    {
        var index = GetGridIndex(worldPosition);
        return ExtractGameObject(index);
    }

    public bool TrySetGameObject(Vector2Int index, T gameObject)
    {
        if (!IsWithinGrid(index) || gameObject == null)
            return false;

        RemoveGameObject(index);

        gameObject.transform.position = ConvertingPosition(index);
        gameObject.transform.rotation = GetRotation();

        SetValueInGrid(index, gameObject);
        return true;
    }

    public bool TrySetGameObject(Vector3 worldPosition, T gameObject)
    {
        var index = GetGridIndex(worldPosition);
        if (!IsWithinGrid(index))
            return false;

        return TrySetGameObject(index, gameObject);
    }

    public T GetGameObject(Vector2Int index)
    {
        return IsWithinGrid(index) ? GetValue(index) : null;
    }

    public T GetGameObject(Vector3 worldPosition)
    {
        var index = GetGridIndex(worldPosition);
        return GetGameObject(index);
    }

    public bool HasGameObject(Vector2Int index)
    {
        return IsWithinGrid(index) && GetValue(index) != null;
    }

    public IEnumerable<T> GetAllGameObjects()
    {
        var result = new List<T>();
        var dictionary = GetGridDictionary();

        foreach (var kvp in dictionary)
        {
            var gameObject = kvp.Value;
            if (gameObject != null)
            {
                result.Add(gameObject);
            }
        }

        return result;
    }

    public void ClearAllGameObjects()
    {
        var dictionary = GetGridDictionary();
        var keysToRemove = new List<Vector2Int>();

        foreach (var kvp in dictionary)
        {
            var gameObject = kvp.Value;
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject.gameObject);
            }
            keysToRemove.Add(kvp.Key);
        }

        _grid.GridDictionary.Clear();
    }

    private Quaternion GetRotation()
    {
        return Quaternion.identity;
    }

    public void FillAreaWithPrefab(Vector2Int pointA, Vector2Int pointB, T prefab, Transform parent = null)
    {
        var minX = Mathf.Min(pointA.x, pointB.x);
        var maxX = Mathf.Max(pointA.x, pointB.x);
        var minY = Mathf.Min(pointA.y, pointB.y);
        var maxY = Mathf.Max(pointA.y, pointB.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                var index = new Vector2Int(x, y);
                if (!IsWithinGrid(index))
                {
                    AddGridCell(index);
                }
            }
        }

        var area = GetRectangularArea(pointA, pointB);
        foreach (var index in area.Keys)
        {
            InitializeGameObject(index, prefab, parent);
        }
    }

    public T FindNearestGameObject<TComponent>(Vector3 worldPosition) where TComponent : Component
    {
        var nearestIndex = FindNearestExpanding(worldPosition, go => go != null && go.GetComponent<TComponent>() != null);
        return nearestIndex.HasValue ? GetGameObject(nearestIndex.Value) : null;
    }

    public List<T> GetGameObjectsWithComponent<TComponent>() where TComponent : Component
    {
        var result = new List<T>();
        var dictionary = GetGridDictionary();

        foreach (var kvp in dictionary)
        {
            var gameObject = kvp.Value;
            if (gameObject != null && gameObject.GetComponent<TComponent>() != null)
            {
                result.Add(gameObject);
            }
        }

        return result;
    }

    private void AddGridCell(Vector2Int index)
    {
        if (!IsWithinGrid(index))
        {
            SetValueInGrid(index, null);
        }
    }
}
