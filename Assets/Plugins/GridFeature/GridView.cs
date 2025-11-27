using UnityEngine;

public class GridView : MonoBehaviour
{
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Material _gridMaterial;
    private Texture2D _gridTexture;
    private Vector2Int _size;

    public void Instantiate(GridConfig gridConfig)
    {
        if (gridConfig.NodePrefab == null)
        {
            Debug.LogWarning("NodePrefab is null");
            return;
        }

        transform.position = gridConfig.Position;
        transform.rotation = gridConfig.RotationQuaternion;

        // Сохраняем размер для использования в других методах
        // _size = gridConfig.size;
        var cellSize = gridConfig.CellSize;
        var cellOffset = gridConfig.CellOffset;
        var totalCellSize = new Vector2(cellSize, cellSize) + cellOffset;

        // Получаем материал и текстуру из префаба
        ExtractVisualComponentsFromPrefab(gridConfig.NodePrefab);

        // Создаем меш для всей сетки (всего 2 треугольника)
        CreateGridMeshWithTwoTriangles(_size, totalCellSize);

        // Настраиваем материал
        SetupMaterial();
    }

    private void ExtractVisualComponentsFromPrefab(GameObject NodePrefab)
    {
        // Добавляем компоненты для рендеринга
        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Пытаемся получить материал из префаба
        var meshRenderer = NodePrefab.GetComponent<MeshRenderer>();
        var spriteRenderer = NodePrefab.GetComponent<SpriteRenderer>();

        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            // Используем материал из MeshRenderer
            _gridMaterial = new Material(meshRenderer.sharedMaterial);

            // Пытаемся получить текстуру из свойства _MainTex (Diffuse)
            if (_gridMaterial.HasProperty("_MainTex") && _gridMaterial.GetTexture("_MainTex") != null)
            {
                _gridTexture = _gridMaterial.GetTexture("_MainTex") as Texture2D;
            }
        }
        else if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // Используем материал из SpriteRenderer или создаем новый
            if (spriteRenderer.sharedMaterial != null)
            {
                _gridMaterial = new Material(spriteRenderer.sharedMaterial);
            }
            else
            {
                _gridMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            // Получаем текстуру из спрайта
            _gridTexture = spriteRenderer.sprite.texture;

            // Устанавливаем текстуру в свойство _MainTex
            if (_gridTexture != null && _gridMaterial.HasProperty("_MainTex"))
            {
                _gridMaterial.SetTexture("_MainTex", _gridTexture);
            }
        }
        else
        {
            // Создаем стандартный материал если ничего не найдено
            _gridMaterial = new Material(Shader.Find("Standard"));
            Debug.LogWarning("No visual components found in node prefab, using default material");
        }
    }

    private void CreateGridMeshWithTwoTriangles(Vector2Int size, Vector2 totalCellSize)
    {
        var mesh = new Mesh();
        mesh.name = "GridMesh";

        // Всего 4 вершины для прямоугольника
        var vertices = new Vector3[4];
        var uv = new Vector2[4];
        var triangles = new int[6]; // 2 треугольника = 6 индексов

        // Рассчитываем общий размер сетки
        var gridWidth = size.x * totalCellSize.x;
        var gridHeight = size.y * totalCellSize.y;

        // Вершины прямоугольника
        vertices[0] = new Vector3(0, 0, 0); // нижний-левый
        vertices[1] = new Vector3(0, gridHeight, 0); // верхний-левый
        vertices[2] = new Vector3(gridWidth, gridHeight, 0); // верхний-правый
        vertices[3] = new Vector3(gridWidth, 0, 0); // нижний-правый

        // UV координаты - текстура будет повторяться по количеству ячеек
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(0, size.y);
        uv[2] = new Vector2(size.x, size.y);
        uv[3] = new Vector2(size.x, 0);

        // Первый треугольник (0-1-2)
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        // Второй треугольник (0-2-3)
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        _meshFilter.mesh = mesh;
    }

    private void SetupMaterial()
    {
        if (_gridMaterial != null)
        {
            _meshRenderer.material = _gridMaterial;

            // Настраиваем повторение текстуры для свойства _MainTex
            if (_gridTexture != null && _gridMaterial.HasProperty("_MainTex"))
            {
                _gridMaterial.SetTextureScale("_MainTex", new Vector2(_size.x, _size.y));
                _gridMaterial.SetTexture("_MainTex", _gridTexture);

                // Устанавливаем режим повторения текстуры
                if (_gridTexture != null)
                {
                    _gridTexture.wrapMode = TextureWrapMode.Repeat;
                }
            }
        }
    }

    // Метод для обновления текстуры
    public void UpdateTexture(Texture2D newTexture)
    {
        if (_meshRenderer != null && _meshRenderer.material != null &&
            _meshRenderer.material.HasProperty("_MainTex"))
        {
            _meshRenderer.material.SetTexture("_MainTex", newTexture);
            _gridTexture = newTexture;

            // Обновляем масштаб текстуры
            _meshRenderer.material.SetTextureScale("_MainTex", new Vector2(_size.x, _size.y));

            // Устанавливаем режим повторения
            if (newTexture != null)
            {
                newTexture.wrapMode = TextureWrapMode.Repeat;
            }
        }
    }

    // Метод для обновления материала
    public void UpdateMaterial(Material newMaterial)
    {
        if (newMaterial != null)
        {
            _meshRenderer.material = newMaterial;
            _gridMaterial = newMaterial;

            // Обновляем текстуру если есть в свойстве _MainTex
            if (newMaterial.HasProperty("_MainTex") && newMaterial.GetTexture("_MainTex") != null)
            {
                _gridTexture = newMaterial.GetTexture("_MainTex") as Texture2D;

                // Обновляем масштаб текстуры
                newMaterial.SetTextureScale("_MainTex", new Vector2(_size.x, _size.y));
            }
        }
    }

    // Метод для изменения цвета сетки
    public void SetColor(Color color)
    {
        if (_meshRenderer != null && _meshRenderer.material != null &&
            _meshRenderer.material.HasProperty("_Color"))
        {
            _meshRenderer.material.SetColor("_Color", color);
        }
        else if (_meshRenderer != null && _meshRenderer.material != null)
        {
            _meshRenderer.material.color = color;
        }
    }

    // Метод для показа/скрытия сетки
    public void SetVisible(bool visible)
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = visible;
        }
    }

    // Метод для получения текущей текстуры из Diffuse
    public Texture2D GetCurrentDiffuseTexture()
    {
        if (_gridMaterial != null && _gridMaterial.HasProperty("_MainTex"))
        {
            return _gridMaterial.GetTexture("_MainTex") as Texture2D;
        }
        return null;
    }

    // Метод для получения границ сетки
    public Bounds GetGridBounds()
    {
        if (_meshFilter != null && _meshFilter.mesh != null)
        {
            return _meshFilter.mesh.bounds;
        }
        return new Bounds();
    }
}
