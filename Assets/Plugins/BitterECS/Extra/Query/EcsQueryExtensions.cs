using System;
using System.Runtime.CompilerServices;
using BitterECS.Core;

namespace BitterECS.Extra
{
    public static class EcsQueryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsQuery CreateQuery(this EcsPresenter presenter, Func<EcsFilter, EcsFilter> filterBuilder)
        {
            return new EcsQuery(presenter, filterBuilder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsQuery Query(this EcsPresenter presenter, Func<EcsFilter, EcsFilter> filterBuilder)
        {
            return presenter.CreateQuery(filterBuilder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsQuery QueryInclude<T>(this EcsPresenter presenter) where T : struct
        {
            return presenter.Query(filter => filter.Include<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsQuery QueryInclude<T1, T2>(this EcsPresenter presenter) where T1 : struct where T2 : struct
        {
            return presenter.Query(filter => filter.Include<T1>().Include<T2>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsQuery QueryInclude<T1, T2, T3>(this EcsPresenter presenter) where T1 : struct where T2 : struct where T3 : struct
        {
            return presenter.Query(filter => filter.Include<T1>().Include<T2>().Include<T3>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsQuery QueryExclude<T>(this EcsPresenter presenter) where T : struct
        {
            return presenter.Query(filter => filter.Exclude<T>());
        }
    }
}
