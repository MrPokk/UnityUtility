using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(GridVisualizerSetting), typeof(GridEditorSetting))]
public class GridProvider : MonoBehaviour
{
    [SerializeField] private GridConfig _gridConfig;

    private GridVisualizerSetting _gridVisualizerSetting;
    private GridEditorSetting _gridEditorSetting;

    public GridConfig GridConfig => _gridConfig;
    public GridVisualizerSetting GridVisualizerSetting => _gridVisualizerSetting;
    public GridEditorSetting GridEditorSetting => _gridEditorSetting;

    private void OnValidate()
    {
        _gridVisualizerSetting = GetComponent<GridVisualizerSetting>();
        _gridEditorSetting = GetComponent<GridEditorSetting>();

        _gridVisualizerSetting.Initialized(_gridConfig);
        _gridEditorSetting.Initialized(_gridConfig);
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
        _gridVisualizerSetting?.DrawGridGizmos();
    }
#endif

    private void Reset()
    {
        _gridVisualizerSetting = GetComponent<GridVisualizerSetting>();
        _gridEditorSetting = GetComponent<GridEditorSetting>();

        _gridVisualizerSetting.Initialized(_gridConfig);
        _gridEditorSetting.Initialized(_gridConfig);
    }
}
