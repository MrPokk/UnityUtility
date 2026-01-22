using System;
using System.Collections.Generic;

namespace BitterECS.Core
{
    public static class PriorityUtility
    {
        public static Comparison<IEcsPriority> Comparison()
        {
            return (left, right) =>
            {
                var priorityComparison = left.Priority.CompareTo(right.Priority);
                return priorityComparison != 0
                ? priorityComparison
                : GetHash(left).CompareTo(GetHash(right));
            };
        }

        private static int GetHash(IEcsPriority system) => system.GetHashCode();
        public static Comparer<IEcsPriority> Sort() => Comparer<IEcsPriority>.Create(Comparison());
    }
}
