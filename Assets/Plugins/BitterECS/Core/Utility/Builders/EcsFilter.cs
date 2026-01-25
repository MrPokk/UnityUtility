using System;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public struct EcsFilter
    {
        private readonly EcsPresenter _presenter;
        private ICondition[] _includeConditions;
        private ICondition[] _excludeConditions;
        private ICondition[] _orConditions;
        private int _includeCount;
        private int _excludeCount;
        private int _orCount;

        private RefWorldVersion _refWorld;
        private EcsEntity[] _filteredCache;
        private int _filteredLength;

        public readonly ReadOnlySpan<ICondition> IncludeSpan => new(_includeConditions, 0, _includeCount);
        public readonly ReadOnlySpan<ICondition> ExcludeSpan => new(_excludeConditions, 0, _excludeCount);
        public readonly ReadOnlySpan<ICondition> OrSpan => new(_orConditions, 0, _orCount);

        public EcsFilter(EcsPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _includeConditions = new ICondition[EcsConfig.FilterConditionInclude];
            _excludeConditions = new ICondition[EcsConfig.FilterConditionExclude];
            _orConditions = new ICondition[EcsConfig.FilterConditionInclude];
            _includeCount = 0;
            _excludeCount = 0;
            _orCount = 0;

            _refWorld = new();

            _filteredCache = new EcsEntity[GetRequiredCapacity(presenter)];
            _filteredLength = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>() where T : struct
        {
            AddCondition(ref _includeConditions, new HasComponentCondition<T>(), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<T>(Predicate<T> predicate) where T : struct
        {
            AddCondition(ref _includeConditions, new ComponentPredicateCondition<T>(predicate), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Where(Predicate<EcsEntity> predicate)
        {
            AddCondition(ref _includeConditions, new EntityPredicateCondition(predicate), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereType<T>(Predicate<T> predicate) where T : EcsEntity
        {
            AddCondition(ref _includeConditions, new EntityTypePredicateCondition<T>(predicate), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<T>(Predicate<T> predicate) where T : class, ILinkableProvider
        {
            AddCondition(ref _includeConditions, new TypeProviderPredicateCondition<T>(predicate), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<T>() where T : class, ILinkableProvider
        {
            AddCondition(ref _includeConditions, new TypeProviderPredicateCondition<T>(static _ => true), ref _includeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Exclude<T>() where T : struct
        {
            AddCondition(ref _excludeConditions, new NotHasComponentCondition<T>(), ref _excludeCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Or<T>() where T : struct
        {
            AddCondition(ref _orConditions, new HasComponentCondition<T>(), ref _orCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Or<T>(Predicate<T> predicate) where T : struct
        {
            AddCondition(ref _orConditions, new ComponentPredicateCondition<T>(predicate), ref _orCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Or(Predicate<EcsEntity> predicate)
        {
            AddCondition(ref _orConditions, new EntityPredicateCondition(predicate), ref _orCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter OrType<T>(Predicate<T> predicate) where T : EcsEntity
        {
            AddCondition(ref _orConditions, new EntityTypePredicateCondition<T>(predicate), ref _orCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter OrProvider<T>(Predicate<T> predicate) where T : class, ILinkableProvider
        {
            AddCondition(ref _orConditions, new TypeProviderPredicateCondition<T>(predicate), ref _orCount);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter OrProvider<T>() where T : class, ILinkableProvider
        {
            AddCondition(ref _orConditions, new TypeProviderPredicateCondition<T>(static _ => true), ref _orCount);
            return this;
        }

        private void AddCondition(ref ICondition[] conditions, ICondition newCondition, ref int count)
        {
            if (count >= conditions.Length)
            {
                Array.Resize(ref conditions, conditions.Length * 2);
            }

            conditions[count] = newCondition;
            count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool MatchesAllConditions(EcsEntity entity)
        {
            for (var i = 0; i < IncludeSpan.Length; i++)
            {
                if (!IncludeSpan[i].Check(entity))
                {
                    return false;
                }
            }

            for (var i = 0; i < ExcludeSpan.Length; i++)
            {
                if (!ExcludeSpan[i].Check(entity))
                {
                    return false;
                }
            }

            if (OrSpan.Length <= 0)
            {
                return true;
            }

            for (var i = 0; i < OrSpan.Length; i++)
            {
                if (!OrSpan[i].Check(entity))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static int GetRequiredCapacity(EcsPresenter presenter) => presenter.CountEntity + (presenter.CountEntity / 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureCacheCapacity()
        {
            var requiredCapacity = GetRequiredCapacity(_presenter);
            var requiredCapacityMax = requiredCapacity + (requiredCapacity / 2);
            if (_filteredCache.Length >= requiredCapacity && _filteredCache.Length < requiredCapacityMax)
            {
                return;
            }

            Array.Resize(ref _filteredCache, requiredCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<EcsEntity> ValidationCacheOnFilter()
        {
            EnsureCacheCapacity();

            if (_refWorld != EcsWorld.GetRefWorld())
            {
                RebuildCache();
                _refWorld = EcsWorld.GetRefWorld();
            }

            var filteringSpan = new ReadOnlySpan<EcsEntity>(_filteredCache, 0, _filteredLength);
            return filteringSpan;
        }

        private void RebuildCache()
        {
            var aliveEntities = _presenter.GetAliveEntities();
            ResetFilteredCache();

            for (var i = 0; i < aliveEntities.Count; i++)
            {
                var entity = aliveEntities[i];
                if (!MatchesAllConditions(entity))
                {
                    continue;
                }

                AddToFilteredCache(entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetFilteredCache() => _filteredLength = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToFilteredCache(EcsEntity entity = default)
        {
            _filteredCache[_filteredLength] = entity;
            _filteredLength++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Filter Entities() => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Filter<T> Providers<T>() where T : class, ILinkableProvider => new(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Filter<ILinkableProvider> Providers() => Providers<ILinkableProvider>();

        public ReadOnlySpan<EcsEntity>.Enumerator GetEnumerator() => ValidationCacheOnFilter().GetEnumerator();
        public int Count() => ValidationCacheOnFilter().Length;

        public ref struct Filter
        {
            private EcsFilter _filter;
            public Filter(in EcsFilter filter) => _filter = filter;
            public ReadOnlySpan<EcsEntity>.Enumerator GetEnumerator() => _filter.ValidationCacheOnFilter().GetEnumerator();
            public readonly int Count() => _filter.ValidationCacheOnFilter().Length;
        }

        public ref struct Filter<T> where T : class, ILinkableProvider
        {
            private EcsFilter _filter;

            public Filter(in EcsFilter filter) => _filter = filter;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() => new(_filter.ValidationCacheOnFilter());

            public ref struct Enumerator
            {
                private ReadOnlySpan<EcsEntity>.Enumerator _entityEnumerator;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Enumerator(ReadOnlySpan<EcsEntity> entities) => _entityEnumerator = entities.GetEnumerator();

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext() => _entityEnumerator.MoveNext();

                public T Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => _entityEnumerator.Current.GetProvider<T>();
                }
            }
        }
    }

    public struct EcsFilter<T> where T : EcsPresenter, new()
    {
        private EcsFilter? _filter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (_filter != null) return;
            _filter = new EcsFilter(EcsWorld.Get<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<TComponent>() where TComponent : struct
        {
            EnsureInitialized();
            return _filter.Value.Include<TComponent>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Include<TComponent>(Predicate<TComponent> predicate) where TComponent : struct
        {
            EnsureInitialized();
            return _filter.Value.Include(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Exclude<TComponent>() where TComponent : struct
        {
            EnsureInitialized();
            return _filter.Value.Exclude<TComponent>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Where(Predicate<EcsEntity> predicate)
        {
            EnsureInitialized();
            return _filter.Value.Where(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereType<TComponent>(Predicate<TComponent> predicate) where TComponent : EcsEntity
        {
            EnsureInitialized();
            return _filter.Value.WhereType(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter WhereProvider<TComponent>(Predicate<TComponent> predicate) where TComponent : class, ILinkableProvider
        {
            EnsureInitialized();
            return _filter.Value.WhereProvider(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Or<TComponent>() where TComponent : struct
        {
            EnsureInitialized();
            return _filter.Value.Or<TComponent>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter Or<TComponent>(Predicate<TComponent> predicate) where TComponent : struct
        {
            EnsureInitialized();
            return _filter.Value.Or(predicate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter.Filter Entities()
        {
            EnsureInitialized();
            return _filter.Value.Entities();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsFilter.Filter<TProvider> Providers<TProvider>() where TProvider : class, ILinkableProvider
        {
            EnsureInitialized();
            return _filter.Value.Providers<TProvider>();
        }
    }

    public interface ICondition
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool Check(EcsEntity entity);
    }

    public readonly struct HasComponentCondition<T> : ICondition where T : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => entity.Has<T>();
    }

    public readonly struct EntityPredicateCondition : ICondition
    {
        private readonly Predicate<EcsEntity> _predicate;

        public EntityPredicateCondition(Predicate<EcsEntity> predicate) => _predicate = predicate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => _predicate(entity);
    }

    public readonly struct EntityTypePredicateCondition<T> : ICondition where T : EcsEntity
    {
        private readonly Predicate<T> _predicate;

        public EntityTypePredicateCondition(Predicate<T> predicate) => _predicate = predicate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => entity is T typeEntity && _predicate(typeEntity);
    }

    public readonly struct TypeProviderPredicateCondition<T> : ICondition where T : class, ILinkableProvider
    {
        private readonly Predicate<T> _predicate;

        public TypeProviderPredicateCondition(Predicate<T> predicate) => _predicate = predicate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => entity.TryGetProvider<T>(out var provider) && _predicate(provider);
    }

    public readonly struct NotHasComponentCondition<T> : ICondition where T : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => !entity.Has<T>();
    }

    public readonly struct ComponentPredicateCondition<T> : ICondition where T : struct
    {
        private readonly Predicate<T> _predicate;

        public ComponentPredicateCondition(Predicate<T> predicate) => _predicate = predicate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(EcsEntity entity) => entity.Has<T>() && _predicate(entity.Get<T>());
    }
}
