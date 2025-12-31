using System;

namespace BitterECS.Core
{
    public static class EcsConditions
    {
        public static bool HasAll<T1, T2>(EcsEntity entity)
            where T1 : struct
            where T2 : struct
        {
            return entity.Has<T1>() && entity.Has<T2>();
        }

        public static bool HasAll<T1, T2, T3>(EcsEntity entity)
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            return entity.Has<T1>() && entity.Has<T2>() && entity.Has<T3>();
        }

        public static bool HasAll<T1, T2, T3, T4>(EcsEntity entity)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            return entity.Has<T1>() && entity.Has<T2>() && entity.Has<T3>() && entity.Has<T4>();
        }

        public static bool HasAny<T1, T2>(EcsEntity entity)
            where T1 : struct
            where T2 : struct
        {
            return entity.Has<T1>() || entity.Has<T2>();
        }

        public static bool ComponentValue<T>(EcsEntity entity, Func<T, bool> predicate) where T : struct
        {
            return entity.Has<T>() && predicate(entity.Get<T>());
        }

        public static bool AllComponentsValue<T1, T2>(
            EcsEntity entity,
            Func<T1, bool> predicate1,
            Func<T2, bool> predicate2)
            where T1 : struct
            where T2 : struct
        {
            return entity.Has<T1>() && entity.Has<T2>() 
                && predicate1(entity.Get<T1>()) 
                && predicate2(entity.Get<T2>());
        }

        public static bool AllComponentsValue<T1, T2, T3>(
            EcsEntity entity,
            Func<T1, bool> predicate1,
            Func<T2, bool> predicate2,
            Func<T3, bool> predicate3)
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            return entity.Has<T1>() && entity.Has<T2>() && entity.Has<T3>()
                && predicate1(entity.Get<T1>()) 
                && predicate2(entity.Get<T2>())
                && predicate3(entity.Get<T3>());
        }
    }
}
