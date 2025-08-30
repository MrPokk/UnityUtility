using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AIBehaviourTreeConfig), menuName = "Config/AIBehaviourTree", order = 0)]
public sealed class AIBehaviourTreeConfig : ScriptableObject
{
    public AIDifficulty difficulty = AIDifficulty.None;
    public List<AIBehaviourTree> behaviourTrees = new();
}
