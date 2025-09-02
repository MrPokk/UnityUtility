using UnityEngine;

public class GridView : MonoBehaviour
{
    private GameObject _node;

    public void Instantiate(GridConfig gridConfig)
    {
        if (gridConfig.nodePrefab == null)
        {
            Debug.LogAssertion("NodePrefab is null");
            return;
        }

        transform.position = gridConfig.position;
        transform.rotation = gridConfig.RotationQuaternion;

        _node = gridConfig.nodePrefab;
        var size = gridConfig.size;
        var cellSize = gridConfig.cellSize;
        var cellOffset = gridConfig.cellOffset;
        var totalCellSize = new Vector2(cellSize, cellSize) + cellOffset;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                var node = Instantiate(_node, transform);
                node.transform.localPosition = new Vector3(
                    x * totalCellSize.x + cellSize * 0.5f,
                    y * totalCellSize.y + cellSize * 0.5f,
                    0
                );
            }
        }
    }
}
