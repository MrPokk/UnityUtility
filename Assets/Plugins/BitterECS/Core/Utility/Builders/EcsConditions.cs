using System;

namespace BitterECS.Core
{
    public static class EcsConditions
    {
        public static Predicate<T> GreaterThan<T>(T value) where T : IComparable<T> => c => c.CompareTo(value) > 0;
        public static Predicate<T> LessThan<T>(T value) where T : IComparable<T> => c => c.CompareTo(value) < 0;
        public static Predicate<T> EqualTo<T>(T value) where T : IEquatable<T> => c => c.Equals(value);
        public static Predicate<T> NotNull<T>() where T : class => c => c != null;

        public static bool HasProvider<T>(EcsEntity e) where T : ILinkableProvider => e.HasProvider<T>();

        public static bool Has<T1>(EcsEntity e) where T1 : new() => e.Has<T1>();
        public static bool Has<T1, T2>(EcsEntity e) where T1 : new() where T2 : new() => Has<T1>(e) && e.Has<T2>();
        public static bool Has<T1, T2, T3>(EcsEntity e) where T1 : new() where T2 : new() where T3 : new() => Has<T1, T2>(e) && e.Has<T3>();
        public static bool Has<T1, T2, T3, T4>(EcsEntity e) where T1 : new() where T2 : new() where T3 : new() where T4 : new() => Has<T1, T2, T3>(e) && e.Has<T4>();
        public static bool Has<T1, T2, T3, T4, T5>(EcsEntity e) where T1 : new() where T2 : new() where T3 : new() where T4 : new() where T5 : new() => Has<T1, T2, T3, T4>(e) && e.Has<T5>();

        public static bool Not<T1>(EcsEntity e) where T1 : new() => !Has<T1>(e);
        public static bool Not<T1, T2>(EcsEntity e) where T1 : new() where T2 : new() => !Has<T1, T2>(e);
        public static bool Not<T1, T2, T3>(EcsEntity e) where T1 : new() where T2 : new() where T3 : new() => !Has<T1, T2, T3>(e);
        public static bool Not<T1, T2, T3, T4>(EcsEntity e) where T1 : new() where T2 : new() where T3 : new() where T4 : new() => !Has<T1, T2, T3, T4>(e);
        public static bool Not<T1, T2, T3, T4, T5>(EcsEntity e) where T1 : new() where T2 : new() where T3 : new() where T4 : new() where T5 : new() => !Has<T1, T2, T3, T4, T5>(e);
    }
}
