using UnityEngine;

public class MonoGridModel<T> : GridModel<T> where T : MonoBehaviour
{
    public MonoGridModel(GridConfig gridConfig) : base(gridConfig)
    { }
}
