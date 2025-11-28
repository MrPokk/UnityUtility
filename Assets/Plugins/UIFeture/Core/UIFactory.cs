#if VCONTAINER_AVAILABLE

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Object = UnityEngine.Object;

namespace UIFeture.Core
{
    public static class UIFactory
    {
        public static UIRootManager CreateRootManager(Dictionary<Type, WindowBinder> binders, IObjectResolver container)
        {
            var rootManager = new GameObject("[UIRoot]").AddComponent<UIRootManager>();
            var canvas = CreateCanvas(rootManager.transform);

            var screens = CreateUIContainer("UIScreens", canvas);
            var popups = CreateUIContainer("UIPopups", canvas);

            rootManager.Initialize(new(container, popups, screens, binders));
            Object.DontDestroyOnLoad(rootManager);

            return rootManager;
        }

        private static Transform CreateCanvas(Transform parent)
        {
            var canvas = new GameObject("UIRootCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)).transform;

            canvas.SetParent(parent);
            var c = canvas.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            canvas.GetComponent<CanvasScaler>()
                .SetupScaleMode(CanvasScaler.ScaleMode.ScaleWithScreenSize, new Vector2(960, 540));

            return canvas;
        }

        private static Transform CreateUIContainer(string name, Transform parent)
        {
            var container = new GameObject(name).AddComponent<RectTransform>();
            container.SetParent(parent);
            container.FullStretch();
            return container;
        }

        private static void FullStretch(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.localPosition = Vector3.zero;
        }

        private static void SetupScaleMode(this CanvasScaler scaler, CanvasScaler.ScaleMode mode, Vector2 resolution)
        {
            scaler.uiScaleMode = mode;
            scaler.referenceResolution = resolution;
        }
    }
}
#endif
