using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(GridVisualizerSetting), typeof(GridEditorSetting))]
public class GridProvider : MonoBehaviour
{
    [SerializeField] private GridConfig _gridConfig;

    private GridVisualizerSetting _GridVisualizerSetting;
    private GridEditorSetting _GridEditorSetting;

    public GridConfig GridConfig => _gridConfig;
    public GridVisualizerSetting GridVisualizerSetting => _GridVisualizerSetting;
    public GridEditorSetting GridEditorSetting => _GridEditorSetting;

    private void OnValidate()
    {
        _GridVisualizerSetting = GetComponent<GridVisualizerSetting>() ?? gameObject.AddComponent<GridVisualizerSetting>();
        _GridEditorSetting = GetComponent<GridEditorSetting>() ?? gameObject.AddComponent<GridEditorSetting>();

        _GridVisualizerSetting.Initialized(_gridConfig);
        _GridEditorSetting.Initialized(_gridConfig);
    }

    private void Start()
    {
#if !UNITY_EDITOR
        Destroy(this);
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        _GridVisualizerSetting?.DrawGridGizmos();
    }
#endif

    private void Reset()
    {
        _GridVisualizerSetting = GetComponent<GridVisualizerSetting>() ?? gameObject.AddComponent<GridVisualizerSetting>();
        _GridEditorSetting = GetComponent<GridEditorSetting>() ?? gameObject.AddComponent<GridEditorSetting>();

        _GridVisualizerSetting.Initialized(_gridConfig);
        _GridEditorSetting.Initialized(_gridConfig);
    }
}
