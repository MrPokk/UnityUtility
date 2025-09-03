using System;
using System.Runtime.CompilerServices;
using BitterECS.Core;

namespace BitterECS.Extra
{
    public static class EcsEventPresenterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsEventPool<T> GetEventPool<T>(this EcsPresenter presenter) where T : struct
        {
            var poolType = typeof(T);

            if (!presenter.TryGetPool<T>(out var pool))
            {
                presenter.RegisterPoolFactory<T>(() => new EcsEventPool<T>());

                pool = presenter.GetPool<T>();
            }

            if (pool is EcsEventPool<T> eventPool)
            {
                return eventPool;
            }

            throw new InvalidOperationException($"Pool for {typeof(T).Name} is not an event pool");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetEventPool<T>(this EcsPresenter presenter, out EcsEventPool<T> pool) where T : struct
        {
            pool = null;

            if (presenter.TryGetPool<T>(out var basePool) && basePool is EcsEventPool<T> eventPool)
            {
                pool = eventPool;
                return true;
            }

            return false;
        }

        public static void EnsureEventPools<T1, T2>(this EcsPresenter presenter)
            where T1 : struct where T2 : struct
        {
            presenter.GetEventPool<T1>();
            presenter.GetEventPool<T2>();
        }

        public static void EnsureEventPools<T1, T2, T3>(this EcsPresenter presenter)
            where T1 : struct where T2 : struct where T3 : struct
        {
            presenter.GetEventPool<T1>();
            presenter.GetEventPool<T2>();
            presenter.GetEventPool<T3>();
        }
    }
}
