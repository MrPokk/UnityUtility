#if VCONTAINER_AVAILABLE

using System;
using System.Collections.Generic;
using BitterECS.Extra;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace UIFeture.Core
{
    public class UIEntryPoint : IStartable
    {
        private readonly IObjectResolver _container;

        [Inject]
        public UIEntryPoint(IObjectResolver container)
        {
            _container = container;
        }

        public void Start()
        {
            var allBinders = new Dictionary<Type, WindowBinder>();
            var allPrefabs = Resources.LoadAll<GameObject>(PathProject.UI);

            foreach (var prefab in allPrefabs)
            {
                var container = prefab.GetComponent<WindowBinder>();
                if (container != null)
                {
                    var type = container.GetType();
                    allBinders.TryAdd(type, container);
                }
            }

            UIFactory.CreateRootManager(allBinders, _container);
        }
    }
}
#endif
