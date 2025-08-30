using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

[Serializable]
public class AIBehaviourTree
{
    [SerializeReference] public List<AIAbstractCondition> conditions = new();
    [SerializeReference] public List<AIAbstractAction> actions = new();

    public bool IsConditionsValid(AIBrain brain)
    {
        if (!conditions.Any())
            return false;

        foreach (var condition in conditions)
        {
            if (condition.GetResult(brain))
            {
                continue;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
