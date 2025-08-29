using System;
using System.Collections.Generic;
using System.Linq;

namespace BitterECS.Core
{
    public abstract class EcsPresenter : IDisposable
    {
        private ushort _nextEntityId;
        private readonly Dictionary<ushort, EcsEntity> _entities = new(EcsConfig.InitialEntitiesCapacity);
        private readonly Dictionary<Type, object> _pools = new(EcsConfig.InitialPoolCapacity);
        private readonly Dictionary<EcsEntity, ILinkableProvider> _linkedEntities = new(EcsConfig.InitialLinkedEntitiesCapacity);
        private readonly HashSet<Type> _allowedTypes = new(EcsConfig.AllowedTypesCapacity);

        public IReadOnlyCollection<EcsEntity> GetAll() => _entities.Values;

        protected EcsPresenter() => Registration();
        protected abstract void Registration();

        protected void AddLimitedType<T>() where T : EcsEntity => _allowedTypes.Add(typeof(T));

        public bool IsTypeAllowed(Type type)
        {
            var isAllowed = _allowedTypes.Contains(type) ||
            !_allowedTypes.Any() ||
            _allowedTypes.Any(allowedType => type.IsSubclassOf(allowedType));
            return isAllowed;
        }

        public bool IsTypeAllowed<T>() where T : EcsEntity => IsTypeAllowed(typeof(T));

        public bool TryGet(ushort id, out EcsEntity entity) => _entities.TryGetValue(id, out entity);

        public EcsFilter Filter() => new(this);

        public EntityBuilder AddTo() => new(this);
        public EntityBuilder<T> AddTo<T>() where T : EcsEntity => new(this);

        public EntityDestroyer RemoveTo(EcsEntity entity) => new(this, entity);
        public EntityDestroyer<T> RemoveTo<T>(T entity) where T : EcsEntity => new(this, entity);

        public void Add(EcsEntity entity) => Create(entity);
        public EcsEntity Add(Type type) => Create(type);
        public EcsEntity Add<T>() => Create(typeof(T));
        public void Remove(EcsEntity entity) => DestroyEntity(entity);

        internal void Create(EcsEntity entity)
        {
            InitEntity(entity);
        }

        internal EcsEntity Create(Type type)
        {
            var entity = (EcsEntity)Activator.CreateInstance(type);
            return InitEntity(entity);
        }

        internal T Create<T>() where T : EcsEntity
        {
            var entity = Activator.CreateInstance<T>();
            return (T)InitEntity(entity);
        }

        private EcsEntity InitEntity(EcsEntity entity)
        {
            if (!IsTypeAllowed(entity.GetType()))
                throw new Exception($"Can't create entity of type {entity.GetType().Name}");

            entity.Init(new(this, ++_nextEntityId));
            entity.Registration();
            return _entities[_nextEntityId] = entity;
        }

        internal void DestroyEntity(EcsEntity entity)
        {
            if (entity == null || entity.Properties == null)
                return;

            if (_entities.Remove(entity.Properties.Id, out _))
                entity.Dispose();
        }

        public EcsPool<T> GetPool<T>() where T : struct
        {
            return (EcsPool<T>)(_pools.TryGetValue(typeof(T), out var pool) ? pool : _pools[typeof(T)] = new EcsPool<T>());
        }

        public void Link(EcsEntity entity, ILinkableProvider Provider)
        {
            if (entity == null || Provider == null)
                return;

            if (!entity.Has<ProviderComponent>())
            {
                entity.Add(new ProviderComponent(Provider));
            }
            else
            {
                ref var ProviderComponent = ref entity.Get<ProviderComponent>();
                ProviderComponent.current = Provider;
            }

            Provider.Init(new EcsProviderProperty(this));
            _linkedEntities[entity] = Provider;
        }

        public void Unlink(EcsEntity entity)
        {
            if (entity == null)
                return;

            if (entity.Has<ProviderComponent>())
            {
                entity.Remove<ProviderComponent>();
                _linkedEntities.Remove(entity);
            }
        }

        public ILinkableProvider GetProvider(EcsEntity entity)
        {
            return entity == null || !_linkedEntities.TryGetValue(entity, out var Provider) ? default : Provider;
        }

        public T GetProvider<T>(EcsEntity entity) where T : ILinkableProvider
        {
            return entity == null || !_linkedEntities.TryGetValue(entity, out var Provider) ? default : (T)Provider;
        }

        public EcsEntity GetEntity(ILinkableProvider Provider)
        {
            return _linkedEntities.FirstOrDefault(x => x.Value == Provider).Key;
        }

        public void Dispose()
        {
            foreach (var entity in _entities.Values) entity.Dispose();
            foreach (var pool in _pools.Values) (pool as IDisposable)?.Dispose();
            foreach (var Provider in _linkedEntities.Values) Provider.Dispose();

            _entities.Clear();
            _pools.Clear();
            _allowedTypes.Clear();
            _linkedEntities.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
