#if VCONTAINER_AVAILABLE

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using VContainer;

namespace UIFeture.Core
{
    public class WindowsContainer
    {
        public readonly IObjectResolver RootContainer;

        public readonly Transform ScreensContainer;
        public readonly Transform PopupsContainer;

        public Dictionary<Type, IWindowBinder> OpenedBinders { get; set; } = new();
        public IWindowBinder OpenedScreenBinder { get; set; }

        public ReadOnlyDictionary<Type, WindowBinder> Binders => new(_allBinders);

        private Dictionary<Type, WindowBinder> _allBinders;

        public WindowsContainer(
            IObjectResolver rootContainer,
            Transform popupsContainer,
            Transform screensContainer,
            Dictionary<Type, WindowBinder> binders)
        {
            RootContainer = rootContainer;
            PopupsContainer = popupsContainer;
            ScreensContainer = screensContainer;
            _allBinders = binders;
        }
    }
}
#endif
