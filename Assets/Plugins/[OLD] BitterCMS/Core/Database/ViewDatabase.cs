using BitterCMS.CMSSystem.Exceptions;
using BitterCMS.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BitterCMS.CMSSystem
{
    /// <summary>
    /// Database for managing CMS Providers
    /// </summary>
    public class ProviderDatabase : CMSDatabaseCore
    {
        private readonly static Dictionary<Type, CMSProviderCore> AllProvider = new Dictionary<Type, CMSProviderCore>();

        /// <summary>
        /// Gets a Provider by its type
        /// </summary>
        /// <param name="ProviderType">Type of the Provider</param>
        /// <returns>The Provider instance</returns>
        /// <exception cref="KeyNotFoundException">Thrown when Provider type not found</exception>
        /// <exception cref="ArgumentNullException">Thrown when Provider is null</exception>
        public static CMSProviderCore Get(Type ProviderType)
        {
            EnsureInitialized(() => new ProviderDatabase());

            if (!AllProvider.TryGetValue(ProviderType, out var ProviderResult))
                throw new ProviderNotFoundException($"Provider of type {ProviderType.Name} not found in database");

            if (!ProviderResult)
                throw new InvalidProviderException($"Provider is null. Check path: Resources/{PathProject.CMS_ProviderS}");

            return ProviderResult;
        }

        /// <summary>
        /// Gets a Provider by its generic type
        /// </summary>
        /// <typeparam name="T">Type of the Provider</typeparam>
        /// <returns>The Provider instance</returns>
        public static T Get<T>() where T : CMSProviderCore => Get(typeof(T)) as T;

        /// <summary>
        /// Gets all registered Providers
        /// </summary>
        /// <returns>Collection of all Providers</returns>
        public static ICollection<CMSProviderCore> GetAll()
        {
            EnsureInitialized(() => new ProviderDatabase());
            return AllProvider.Values;
        }

        public override void Initialize(bool forceUpdate = false)
        {
            if (IsInit && !forceUpdate)
                return;

            try
            {
                if (forceUpdate)
                    AllProvider.Clear();

                var allGameObjects = Resources.LoadAll<GameObject>(PathProject.CMS_ProviderS);
                if (allGameObjects == null || allGameObjects.Length == 0)
                {
                    Debug.LogWarning($"No Providers found at path: {PathProject.CMS_ProviderS}");
                    return;
                }

                foreach (var Provider in allGameObjects)
                {
                    if (!Provider)
                        continue;

                    var component = Provider.GetComponent<CMSProviderCore>();
                    if (!component)
                    {
                        Debug.LogError($"BaseProvider missing in prefab: {Provider.name}");
                        continue;
                    }

                    AllProvider.TryAdd(component.ID, component);
                }

                IsInit = true;
            }
            catch (Exception ex)
            {
                throw new ProviderDatabaseInitializationException(
                    $"Database initialization failed: {ex.Message}", ex);
            }
        }
    }
}
