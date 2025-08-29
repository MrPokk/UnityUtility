using BitterCMS.Component;
using BitterCMS.UnityIntegration.Utility;
using BitterCMS.Utility.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BitterCMS.CMSSystem
{
    public abstract class CMSPresenterCore : InteractionCore, IEnterInLateUpdate
    {
        private readonly Dictionary<CMSEntityCore, CMSProviderCore> _entitiesWithProviders = new Dictionary<CMSEntityCore, CMSProviderCore>();
        private readonly List<CMSEntityCore> _entitiesWithoutProviders = new List<CMSEntityCore>();
        private readonly HashSet<Type> _allowedEntityTypes = new HashSet<Type>();

        private readonly static HashSet<CMSEntityCore> AllDestroy = new HashSet<CMSEntityCore>();

        protected CMSPresenterCore(params Type[] allowedTypes)
        {
            foreach (var type in allowedTypes)
            {
                if (!typeof(CMSEntityCore).IsAssignableFrom(type))
                    throw new ArgumentException($"Type {type.Name} must inherit from CMSEntity");

                _allowedEntityTypes.Add(type);
            }
        }

        #region [Additionally Data]

        public sealed class CMSPresenterProperty : InitializableProperty
        {
            public readonly CMSPresenterCore PresenterCore;
            public CMSPresenterProperty(CMSPresenterCore presenterCore)
            {
                PresenterCore = presenterCore;
            }
        }

        private struct CMSPresenterInfo
        {
            public readonly Type Type;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Transform Parent;

            public CMSPresenterInfo(Type type, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
            {
                Type = type;
                Position = position;
                Rotation = rotation;
                Parent = parent;
            }
        }

        #endregion

        #region [SpawnEntity]

        /// <summary>
        /// Spawn entity from database if the type is [Serializable], other creates of type
        /// </summary>
        public virtual CMSEntityCore SpawnFromDB(
            Type type, Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            var info = new CMSPresenterInfo(type, position, rotation, parent);

            if (!type.IsDefined(typeof(SerializableAttribute)))
                return Create(info);

            var newEntity = EntityDatabase.GetEntity(type);
            return Create(newEntity, info);
        }

        /// <summary>
        /// Spawn entity from database if the type is [Serializable], other creates of type
        /// </summary>
        public virtual T SpawnFromDB<T>(
            Vector3 position = default, Quaternion rotation = default, Transform parent = null
        ) where T : CMSEntityCore
        {
            return SpawnFromDB(typeof(T), position, rotation, parent) as T;
        }

        /// <summary>
        /// Spawn a new entity of type
        /// </summary>
        public virtual CMSEntityCore SpawnEntity(
            Type type, Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            return Create(new CMSPresenterInfo(type, position, rotation, parent));
        }

        /// <summary>
        /// Spawn a new entity of type
        /// </summary>
        public virtual T SpawnEntity<T>(
            Vector3 position = default, Quaternion rotation = default, Transform parent = null
        ) where T : CMSEntityCore
        {
            return SpawnEntity(typeof(T), position, rotation, parent) as T;
        }

        /// <summary>
        /// Spawn an entity from an existing CMSEntity instance
        /// </summary>
        public virtual CMSEntityCore SpawnEntity(
            CMSEntityCore valueEntityCore, Vector3 position = default, Quaternion rotation = default, Transform parent = null
        )
        {
            return Create(valueEntityCore, new CMSPresenterInfo(valueEntityCore.GetType(), position, rotation, parent));
        }

        /// <summary>
        /// Spawn an entity from an existing CMSEntity instance
        /// </summary>
        public virtual T SpawnEntity<T>(
            T valueEntityCore, Vector3 position = default, Quaternion rotation = default, Transform parent = null
        ) where T : CMSEntityCore
        {
            return SpawnEntity((CMSEntityCore)valueEntityCore, position, rotation, parent) as T;
        }

        #endregion

        #region [CreateEntity]

        private CMSEntityCore Create(CMSPresenterInfo info)
        {
            if (info.Type.IsAbstract)
                throw new TypeAccessException($"Type {info.Type.Name} is Abstract!");

            if (!IsTypeAllowed(info.Type))
                throw new InvalidOperationException($"Type {info.Type.Name} is not allowed for this presenter");

            if (!typeof(CMSEntityCore).IsAssignableFrom(info.Type))
                throw new TypeAccessException($"Type {info.Type.Name} is not a CMSEntity!");

            if (Activator.CreateInstance(info.Type) is not CMSEntityCore newEntityCore)
                return null;

            newEntityCore.Init(new CMSPresenterProperty(this));

            if (!newEntityCore.TryGetComponent<ProviderComponent>(out var Provider))
            {
#if DEBUG || UNITY_EDITOR
                Debug.LogWarning($"Not found ProviderComponent in {newEntityCore}");
#endif
                _entitiesWithoutProviders.Add(newEntityCore);
                return newEntityCore;
            }

            var newProvider = LinkingMonobehaviour(newEntityCore, Provider, info.Position, info.Rotation, info.Parent);

            if (newProvider)
                _entitiesWithProviders.Add(newEntityCore, newProvider);
            else
                _entitiesWithoutProviders.Add(newEntityCore);

            newProvider?.Init(new CMSPresenterProperty(this));

            return newEntityCore;
        }

        private CMSEntityCore Create(CMSEntityCore cmsEntityCore, CMSPresenterInfo info)
        {
            cmsEntityCore.Init(new CMSPresenterProperty(this));

            if (!cmsEntityCore.TryGetComponent<ProviderComponent>(out var Provider))
            {
#if DEBUG || UNITY_EDITOR
                Debug.LogWarning($"Not found ProviderComponent in {cmsEntityCore}");
#endif
                _entitiesWithoutProviders.Add(cmsEntityCore);
                return cmsEntityCore;
            }

            var newProvider = LinkingMonobehaviour(cmsEntityCore, Provider, info.Position, info.Rotation, info.Parent);

            if (newProvider)
                _entitiesWithProviders.Add(cmsEntityCore, newProvider);
            else
                _entitiesWithoutProviders.Add(cmsEntityCore);

            newProvider?.Init(new CMSPresenterProperty(this));

            return cmsEntityCore;
        }

        #endregion

        #region [Entity Management]

        private CMSProviderCore LinkingMonobehaviour(
            CMSEntityCore entityCore, ProviderComponent Provider,
            Vector3 position, Quaternion rotation, Transform parent
        )
        {
            if (!Provider?.Properties.Original || entityCore == null)
                return null;

            var newProvider = Object.Instantiate(Provider.Properties.Original, position, rotation, parent);
            newProvider.name = $"{entityCore.ID.Name} [NEW]";

            Provider.Properties.Current = newProvider;
            return newProvider;
        }

        #endregion

        #region [GetEntity]

        /// <summary>
        /// Filters entities based on required and excluded component types
        /// </summary>
        public CMSEntityCore[] FilterEntities(
            Type[] requiredComponents = null,
            Type[] excludedComponents = null
        )
        {
            var allEntity = GetAllEntities();

            return allEntity.Where(entity =>
            {
                var hasRequired = requiredComponents == null ||
                                  requiredComponents.All(entity.HasComponent);

                var hasExcluded = excludedComponents != null &&
                                  excludedComponents.Any(entity.HasComponent);

                return hasRequired && !hasExcluded;
            }).ToArray();
        }

        /// <summary>
        /// Filters entities that have all specified component types
        /// </summary>
        public CMSEntityCore[] FilterEntities(params Type[] typeComponent)
        {
            var allEntity = GetAllEntities();

            return (from entity in allEntity
                    let hasAllComponents = typeComponent.All(entity.HasComponent)
                    where hasAllComponents
                    select entity).ToArray();
        }

        /// <summary>
        /// Filters entities that have TRequired component but don't have TExcluded component
        /// </summary>
        public IReadOnlyCollection<CMSEntityCore> FilterEntities<TRequired, TExcluded>()
            where TRequired : IEntityComponent
            where TExcluded : IEntityComponent
        {
            return GetAllEntities()
                .Where(entity => entity.HasComponent<TRequired>() && !entity.HasComponent<TExcluded>())
                .ToArray();
        }

        /// <summary>
        /// Filters entities that have the specified component type
        /// </summary>
        public IReadOnlyCollection<CMSEntityCore> FilterEntities<TRequire>() where TRequire : IEntityComponent
        {
            return GetAllEntities().Where(entity => entity.HasComponent<TRequire>()).ToArray();
        }

        /// <summary>
        /// Gets entity of specific type by its Provider 
        /// </summary>
        public T GetEntityByProvider<T>(CMSProviderCore ProviderCore) where T : CMSEntityCore => GetEntityByProvider(ProviderCore) as T;

        /// <summary>
        /// Gets entity by its Provider 
        /// </summary>
        public CMSEntityCore GetEntityByProvider(CMSProviderCore ProviderCore) => !ProviderCore ? null : _entitiesWithProviders.FirstOrDefault(pair => pair.Value == ProviderCore).Key;

        /// <summary>
        /// Gets Provider by its entity
        /// </summary>
        public CMSProviderCore GetProviderByEntity(CMSEntityCore entityCore) => entityCore == null ? null : _entitiesWithProviders.GetValueOrDefault(entityCore);

        /// <summary>
        /// Gets entity of specific type by its type
        /// </summary>
        public T GetEntityByType<T>() where T : CMSEntityCore => GetEntityByType(typeof(T)) as T;

        /// <summary>
        /// Gets entity by its type
        /// </summary>
        public CMSEntityCore GetEntityByType(Type type)
        {
            return GetAllEntities().FirstOrDefault(entity => entity.ID == type);
        }

        /// <summary>
        /// Gets all entities with their Provider associations
        /// </summary>
        public IReadOnlyDictionary<CMSEntityCore, CMSProviderCore> GetEntitiesWithProviders() => _entitiesWithProviders;

        /// <summary>
        /// Gets all entities without Providers
        /// </summary>
        public IReadOnlyCollection<CMSEntityCore> GetEntitiesWithoutProviders() => _entitiesWithoutProviders;

        /// <summary>
        /// Gets all entities (both with and without Providers)
        /// </summary>
        public IReadOnlyCollection<CMSEntityCore> GetAllEntities()
        {
            var allEntities = new List<CMSEntityCore>(_entitiesWithProviders.Count + _entitiesWithoutProviders.Count);
            allEntities.AddRange(_entitiesWithProviders.Keys);
            allEntities.AddRange(_entitiesWithoutProviders);
            return allEntities;
        }

        /// <summary>
        /// Gets all Provider instances of loaded entities
        /// </summary>
        public IReadOnlyCollection<CMSProviderCore> GetProviderEntities() => _entitiesWithProviders.Values;

        #endregion

        #region [DestroyEntity]

        #region [DestroyEntity]

        public void LateUpdate(float timeDelta)
        {
            if (!AllDestroy.Any())
                return;

            var entitiesToDestroy = new List<CMSEntityCore>(AllDestroy);
            AllDestroy.Clear();

            foreach (var entityToDestroy in entitiesToDestroy)
            {
                var entityPresenter = entityToDestroy?.Properties?.PresenterCore;
                if (entityPresenter == null)
                    continue;

                if (entityPresenter._entitiesWithProviders.TryGetValue(entityToDestroy, out var Provider))
                {
                    if (Provider && Provider.gameObject)
                        Object.Destroy(Provider.gameObject);
                    entityPresenter._entitiesWithProviders.Remove(entityToDestroy);
                }
                else if (entityPresenter._entitiesWithoutProviders.Contains(entityToDestroy))
                    entityPresenter._entitiesWithoutProviders.Remove(entityToDestroy);
            }
        }

        public virtual void DestroyEntity(CMSEntityCore entityCore)
        {
            if (entityCore == null || !AllDestroy.Add(entityCore))
                return;
        }

        public virtual void DestroyAllEntities()
        {
            CoroutineUtility.StopAll();

            foreach (var Provider in _entitiesWithProviders.Values)
            {
                if (Provider != null && Provider.gameObject != null)
                    Object.Destroy(Provider.gameObject);
            }

            _entitiesWithProviders.Clear();
            _entitiesWithoutProviders.Clear();
        }

        #endregion 

        #endregion

        #region [Helper Methods]

        public bool IsTypeAllowed(Type type)
        {
            return _allowedEntityTypes.Count == 0
                ? typeof(CMSEntityCore).IsAssignableFrom(type)
                : _allowedEntityTypes.Any(allowedType => allowedType.IsAssignableFrom(type));
        }

        #endregion
    }
}
