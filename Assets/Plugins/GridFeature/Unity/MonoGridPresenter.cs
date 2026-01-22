using System.Collections.Generic;
using BitterECS.Integration;
using UnityEngine;

public class MonoGridPresenter : GridPresenter<ProviderEcs>
{
    public MonoGridPresenter(GridConfig gridConfig) : base(gridConfig)
    { }

    public bool InitializeGameObject(Vector2Int index, ProviderEcs prefab, out ProviderEcs instantiateObject, Transform parent = null)
    {
        instantiateObject = null;
        if (!IsWithinGrid(index) || prefab == null)
            return false;

        if (GetByIndex(index) != null)
            return false;

        var worldPosition = ConvertingPosition(index);
        var gameObject = Object.Instantiate(prefab, worldPosition, GetRotation(), parent);

        SetValueInGrid(index, gameObject);
        instantiateObject = gameObject;
        return true;
    }

    public bool InitializeGameObject(Vector3 worldPosition, ProviderEcs prefab, out ProviderEcs instantiateObject, Transform parent = null)
    {
        return InitializeGameObject(ConvertingPosition(worldPosition), prefab, out instantiateObject, parent);
    }

    public bool MoveGameObject(Vector2Int fromIndex, Vector2Int toIndex)
    {
        if (!IsWithinGrid(fromIndex) || !IsWithinGrid(toIndex))
            return false;

        var objectToIndex = GetByIndex(toIndex);
        if (objectToIndex != null)
            return false;

        var objectFromIndex = GetByIndex(fromIndex);
        objectFromIndex.transform.position = ConvertingPosition(toIndex);

        SetValueInGrid(fromIndex, null);
        SetValueInGrid(toIndex, objectFromIndex);

        return true;
    }

    public bool MoveGameObject(Vector3 fromWorldPosition, Vector3 toWorldPosition)
    {
        var fromIndex = ConvertingPosition(fromWorldPosition);
        var toIndex = ConvertingPosition(toWorldPosition);
        return MoveGameObject(fromIndex, toIndex);
    }

    public bool MoveGameObject(Transform fromWorldPosition, Transform toWorldPosition)
    {
        return MoveGameObject(fromWorldPosition.position, toWorldPosition.position);
    }

    public bool MoveGameObject(ProviderEcs gameObject, Vector3 toWorldPosition)
    {
        return MoveGameObject(gameObject.transform.position, toWorldPosition);
    }

    public bool MoveGameObject(Transform fromWorldPosition, Vector2Int toIndex)
    {
        return MoveGameObject(ConvertingPosition(fromWorldPosition.position), toIndex);
    }

    public bool RemoveGameObject(Vector2Int index)
    {
        if (!IsWithinGrid(index))
            return false;

        var gameObject = GetByIndex(index);
        if (gameObject != null)
        {
            Object.Destroy(gameObject);
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

    public ProviderEcs ExtractGameObject(Vector2Int index)
    {
        if (!IsWithinGrid(index))
            return null;

        var gameObject = GetByIndex(index);
        SetValueInGrid(index, null);
        return gameObject;
    }

    public ProviderEcs ExtractGameObject(Vector3 worldPosition)
    {
        var index = GetGridIndex(worldPosition);
        return ExtractGameObject(index);
    }

    public bool TrySetGameObject(Vector2Int index, ProviderEcs gameObject)
    {
        if (!IsWithinGrid(index) || gameObject == null)
            return false;

        RemoveGameObject(index);

        gameObject.transform.position = ConvertingPosition(index);
        gameObject.transform.rotation = GetRotation();

        SetValueInGrid(index, gameObject);
        return true;
    }

    public bool TrySetGameObject(Vector3 worldPosition, ProviderEcs gameObject)
    {
        var index = GetGridIndex(worldPosition);
        if (!IsWithinGrid(index))
            return false;

        return TrySetGameObject(index, gameObject);
    }

    public ProviderEcs GetGameObject(Vector2Int index)
    {
        return IsWithinGrid(index) ? GetByIndex(index) : null;
    }

    public ProviderEcs GetGameObject(Vector3 worldPosition)
    {
        var index = GetGridIndex(worldPosition);
        return GetGameObject(index);
    }

    public bool HasGameObject(Vector2Int index)
    {
        return IsWithinGrid(index) && GetByIndex(index) != null;
    }

    public IEnumerable<ProviderEcs> GetAllGameObjects()
    {
        var result = new List<ProviderEcs>();
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
                UnityEngine.Object.Destroy(gameObject);
            }
            keysToRemove.Add(kvp.Key);
        }

        // Remove all entries from dictionary
        foreach (var key in keysToRemove)
        {
            dictionary.Remove(key);
        }
    }

    private Quaternion GetRotation()
    {
        return Quaternion.identity;
    }

    public void FillAreaWithPrefab(Vector2Int pointA, Vector2Int pointB, ProviderEcs prefab, Transform parent = null)
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
            InitializeGameObject(index, prefab, out _, parent);
        }
    }

    public ProviderEcs FindNearestGameObject<TComponent>(Vector3 worldPosition) where TComponent : Component
    {
        var nearestIndex = FindNearestExpanding(worldPosition, go => go != null && go.GetComponent<TComponent>() != null);
        return nearestIndex.HasValue ? GetGameObject(nearestIndex.Value) : null;
    }

    public List<ProviderEcs> GetGameObjectsWithComponent<TComponent>() where TComponent : Component
    {
        var result = new List<ProviderEcs>();
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
