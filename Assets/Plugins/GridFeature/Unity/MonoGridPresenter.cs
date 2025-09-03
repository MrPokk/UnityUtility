using System.Collections.Generic;
using UnityEngine;

public class MonoGridPresenter : GridPresenter<GameObject>
{
    public MonoGridPresenter(GridConfig gridConfig) : base(gridConfig)
    { }

    public bool InitializeGameObject(Vector2Int index, GameObject prefab, Transform parent = null)
    {
        if (!IsWithinGrid(index) || prefab == null)
            return false;

        RemoveGameObject(index);

        var worldPosition = ConvertingPosition(index);
        var gameObject = Object.Instantiate(prefab, worldPosition, GetRotation(), parent);

        SetValueInGrid(index, gameObject);
        return true;
    }

    public bool InitializeGameObject(Vector3 worldPosition, GameObject prefab, Transform parent = null)
    {
        var index = GetGridIndex(worldPosition);
        return InitializeGameObject(index, prefab, parent);
    }

    public bool MoveGameObject(Vector2Int fromIndex, Vector2Int toIndex)
    {
        if (!IsWithinGrid(fromIndex) || !IsWithinGrid(toIndex))
            return false;

        var gameObject = GetByIndex(fromIndex);
        if (gameObject == null)
            return false;

        gameObject.transform.position = ConvertingPosition(toIndex);

        SetValueInGrid(fromIndex, null);
        SetValueInGrid(toIndex, gameObject);

        return true;
    }

    public bool MoveGameObject(Vector3 fromWorldPosition, Vector3 toWorldPosition)
    {
        var fromIndex = GetGridIndex(fromWorldPosition);
        var toIndex = GetGridIndex(toWorldPosition);
        return MoveGameObject(fromIndex, toIndex);
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

    public GameObject ExtractGameObject(Vector2Int index)
    {
        if (!IsWithinGrid(index))
            return null;

        var gameObject = GetByIndex(index);
        SetValueInGrid(index, null);
        return gameObject;
    }

    public GameObject ExtractGameObject(Vector3 worldPosition)
    {
        var index = GetGridIndex(worldPosition);
        return ExtractGameObject(index);
    }

    public bool TrySetGameObject(Vector2Int index, GameObject gameObject)
    {
        if (!IsWithinGrid(index) || gameObject == null)
            return false;

        RemoveGameObject(index);

        gameObject.transform.position = ConvertingPosition(index);
        gameObject.transform.rotation = GetRotation();

        SetValueInGrid(index, gameObject);
        return true;
    }

    public bool TrySetGameObject(Vector3 worldPosition, GameObject gameObject)
    {
        var index = GetGridIndex(worldPosition);
        return TrySetGameObject(index, gameObject);
    }

    public GameObject GetGameObject(Vector2Int index)
    {
        return IsWithinGrid(index) ? GetByIndex(index) : null;
    }

    public GameObject GetGameObject(Vector3 worldPosition)
    {
        var index = GetGridIndex(worldPosition);
        return GetGameObject(index);
    }

    public bool HasGameObject(Vector2Int index)
    {
        return IsWithinGrid(index) && GetByIndex(index) != null;
    }

    public IEnumerable<GameObject> GetAllGameObjects()
    {
        var array = GetArray();
        var result = new List<GameObject>();

        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                var gameObject = array[x, y];
                if (gameObject != null)
                {
                    result.Add(gameObject);
                }
            }
        }

        return result;
    }

    public void ClearAllGameObjects()
    {
        var array = GetArray();
        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                var gameObject = array[x, y];
                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                }
                array[x, y] = null;
            }
        }
    }

    private Quaternion GetRotation()
    {
        return Quaternion.identity;
    }

    public void FillAreaWithPrefab(Vector2Int pointA, Vector2Int pointB, GameObject prefab, Transform parent = null)
    {
        var area = GetRectangularArea(pointA, pointB);
        foreach (var index in area.Keys)
        {
            InitializeGameObject(index, prefab, parent);
        }
    }

    public GameObject FindNearestGameObject<TComponent>(Vector3 worldPosition) where TComponent : Component
    {
        var nearestIndex = FindNearestExpanding(worldPosition, go => go != null && go.GetComponent<TComponent>() != null);
        return nearestIndex.HasValue ? GetGameObject(nearestIndex.Value) : null;
    }

    public List<GameObject> GetGameObjectsWithComponent<TComponent>() where TComponent : Component
    {
        var result = new List<GameObject>();
        var array = GetArray();

        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                var gameObject = array[x, y];
                if (gameObject != null && gameObject.GetComponent<TComponent>() != null)
                {
                    result.Add(gameObject);
                }
            }
        }

        return result;
    }
}
