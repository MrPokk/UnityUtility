using System;
using UnityEngine;

[Serializable]
public abstract class AIAbstractCondition
{
    [SerializeField] private bool _inversion = false;

    protected abstract bool IsCondition(AIBrain brain);

    public bool GetResult(AIBrain brain)
    {
        return _inversion ? IsCondition(brain) : !IsCondition(brain);
    }
}
