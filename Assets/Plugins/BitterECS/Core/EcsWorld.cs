using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BitterECS.Core
{
    public readonly struct RefWorldVersion : IEquatable<RefWorldVersion>
    {
        private readonly int _version;
        public int Version => _version;
        internal RefWorldVersion(int version = -1) => _version = version;
        public RefWorldVersion Increment() => new(_version + 1);
        public bool Equals(RefWorldVersion other) => _version == other._version;
        public override bool Equals(object obj) => obj is RefWorldVersion other && Equals(other);
        public override int GetHashCode() => _version;
        public static bool operator ==(RefWorldVersion left, RefWorldVersion right) => left.Equals(right);
        public static bool operator !=(RefWorldVersion left, RefWorldVersion right) => !left.Equals(right);
    }

    public static class EcsWorldStatic
    {
        private static EcsWorld s_instance;
        public static EcsWorld Instance => s_instance ??= new EcsWorld();

        public static void Dispose()
        {
            s_instance?.Dispose();
            s_instance = null;
        }
    }

    public sealed class EcsWorld : IDisposable
    {
        private RefWorldVersion _version;

        private int[] _aliveIds;
        private int[] _idToIndex;
        private EcsComponentMask[] _entityMasks;
        private int _entitiesCount;
        private int _aliveCount;

        private readonly Stack<int> _freeEntityIds;
        private object[] _poolsFast;
        private readonly Dictionary<Type, Func<EcsWorld, object>> _poolFactories;
        private readonly Dictionary<int, ILinkableProvider> _linkedProviders;

        public int CountEntity => _aliveCount;

        public EcsWorld()
        {
            _version = new(0);

            var capacity = EcsDefinitions.InitialEntitiesCapacity;
            _aliveIds = new int[capacity];
            _idToIndex = new int[capacity];
            _entityMasks = new EcsComponentMask[capacity];
            Array.Fill(_idToIndex, -1);

            _freeEntityIds = new Stack<int>(capacity);
            _poolsFast = new object[64];
            _poolFactories = new Dictionary<Type, Func<EcsWorld, object>>();
            _linkedProviders = new Dictionary<int, ILinkableProvider>(EcsDefinitions.InitialLinkedEntitiesCapacity);
        }

        public void AddCheckEvent<T>() where T : new()
        {
            _poolFactories[typeof(T)] = w => new EcsEventPool<T> { World = w };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int> GetAliveIds() => new(_aliveIds, 0, _aliveCount); [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref EcsComponentMask GetEntityMask(int id) => ref _entityMasks[id];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetMaskBit(int id, int componentId) => _entityMasks[id].Set(componentId); [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveMaskBit(int id, int componentId) => _entityMasks[id].Remove(componentId);

        public EcsEntity CreateEntity()
        {
            int id;
            if (_freeEntityIds.Count > 0)
            {
                id = _freeEntityIds.Pop();
            }
            else
            {
                id = _entitiesCount++;
                if (id >= _idToIndex.Length) ResizeIdArrays(id * 2);
            }

            _idToIndex[id] = _aliveCount;
            _aliveIds[_aliveCount] = id;
            _aliveCount++;

            IncreaseVersion();
            return new EcsEntity(this, id);
        }

        public void Remove(EcsEntity entity)
        {
            var id = entity.Id;
            if (id < 0 || id >= _idToIndex.Length) return;

            var index = _idToIndex[id];
            if (index == -1) return;
            _idToIndex[id] = -1;
            _entityMasks[id] = new EcsComponentMask();

            for (var i = 0; i < _poolsFast.Length; i++)
            {
                if (_poolsFast[i] is IPool pool) pool.Remove(id);
            }

            Unlink(entity);

            _aliveCount--;
            if (_aliveCount > 0 && index != _aliveCount)
            {
                var lastId = _aliveIds[_aliveCount];
                _aliveIds[index] = lastId;
                _idToIndex[lastId] = index;
            }

            _freeEntityIds.Push(id);
            IncreaseVersion();
        }

        private void ResizeIdArrays(int newSize)
        {
            Array.Resize(ref _aliveIds, newSize);
            Array.Resize(ref _entityMasks, newSize);
            var oldSize = _idToIndex.Length;
            Array.Resize(ref _idToIndex, newSize);
            Array.Fill(_idToIndex, -1, oldSize, newSize - oldSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsPool<T> GetPool<T>() where T : new()
        {
            var id = EcsComponentTypeId<T>.Id;
            if (id >= _poolsFast.Length) Array.Resize(ref _poolsFast, Math.Max(_poolsFast.Length * 2, id + 1));

            var pool = _poolsFast[id];
            if (pool == null)
            {
                pool = _poolFactories.TryGetValue(typeof(T), out var factory) ? factory(this) : new EcsPool<T> { World = this };
                _poolsFast[id] = pool;
            }
            return (EcsPool<T>)pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IPool GetPoolById(int id) => id < _poolsFast.Length ? _poolsFast[id] as IPool : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int id) => id >= 0 && id < _idToIndex.Length && _idToIndex[id] != -1;

        public bool HasProvider(int id) => _linkedProviders.TryGetValue(id, out _);

        public ILinkableProvider GetProvider(EcsEntity entity) => _linkedProviders.TryGetValue(entity.Id, out var w) ? w : null;

        public void Link(EcsEntity entity, ILinkableProvider provider) =>
         (_linkedProviders[entity.Id] = provider).Init(new EcsProperty(this, entity.Id));

        public void Unlink(EcsEntity entity)
        { if (_linkedProviders.Remove(entity.Id, out var provider)) provider.Dispose(); }

        public EcsEntity Get(int id) => Has(id) ? new EcsEntity(this, id) : new EcsEntity(null, -1);

        public RefWorldVersion GetVersion() => _version;
        internal void IncreaseVersion() => _version = _version.Increment();

        public void Dispose()
        {
            for (var i = 0; i < _poolsFast.Length; i++)
                if (_poolsFast[i] is IDisposable d) d.Dispose();

            if (_linkedProviders.Values.Any())
            {
                for (var i = 0; i < _linkedProviders.Values.Count; i++)
                {
                    if (_linkedProviders.TryGetValue(i, out var provider))
                        provider.Dispose();
                }
            }

            _linkedProviders.Clear();
            _freeEntityIds.Clear();
            _aliveCount = _entitiesCount = 0;
            _aliveIds = _idToIndex = Array.Empty<int>();
            _entityMasks = Array.Empty<EcsComponentMask>();
        }
    }
}
