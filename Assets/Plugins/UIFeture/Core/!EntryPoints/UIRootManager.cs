#if VCONTAINER_AVAILABLE

using UnityEngine;

namespace UIFeture.Core
{
    public class UIRootManager : MonoBehaviour
    {
        private static UIRootManager s_instance;
        public static UIRootManager Instance => s_instance;

        private WindowsContainer _windowsContainer;

        public void Initialize(WindowsContainer windowsContainer)
        {
            _windowsContainer = windowsContainer;
            s_instance = this;
        }

        public static void OpenScreen<T>() where T : WindowBinder
        {
            Instance?.OpenScreenInstance<T>();
        }

        public static void CloseScreen()
        {
            Instance?.CloseScreenInstance();
        }

        public static void OpenPopup<T>() where T : WindowBinder
        {
            Instance?.OpenPopupInstance<T>();
        }

        public static void ClosePopup<T>() where T : WindowBinder
        {
            Instance?.ClosePopupInstance<T>();
        }

        public static void CloseAllPopups()
        {
            Instance?.CloseAllPopupsInstance();
        }

        private void OpenScreenInstance<T>() where T : WindowBinder
        {
            if (_windowsContainer.OpenedScreenBinder != null &&
                _windowsContainer.OpenedScreenBinder.GetType() == typeof(T))
            {
                return;
            }
            CloseScreenInstance();

            var binder = Binding<T>();
            _windowsContainer.OpenedScreenBinder = binder;
            _windowsContainer.OpenedScreenBinder?.Open();
        }

        private void CloseScreenInstance()
        {
            _windowsContainer.OpenedScreenBinder?.Close();
            _windowsContainer.OpenedScreenBinder = null;
        }

        private void OpenPopupInstance<T>() where T : WindowBinder
        {
            var binder = Binding<T>();

            _windowsContainer.OpenedBinders.TryAdd(typeof(T), binder);
            binder.Open();
        }

        private void ClosePopupInstance<T>() where T : WindowBinder
        {
            if (_windowsContainer.OpenedBinders.TryGetValue(typeof(T), out var binder))
            {
                binder.Close();
                _windowsContainer.OpenedBinders.Remove(typeof(T));
            }
        }

        private void CloseAllPopupsInstance()
        {
            foreach (var binder in _windowsContainer.OpenedBinders.Values)
            {
                binder?.Close();
            }
            _windowsContainer.OpenedBinders.Clear();
        }

        private IWindowBinder Binding<T>() where T : WindowBinder
        {
            if (!_windowsContainer.Binders.TryGetValue(typeof(T), out var binderPrefab))
            {
                Debug.LogError($"WindowBinder of type {typeof(T)} not found in binders dictionary");
                return null;
            }

            var parent = typeof(T).IsSubclassOf(typeof(UIScreen))
                ? _windowsContainer.ScreensContainer
                : _windowsContainer.PopupsContainer;

            var windowObject = Instantiate(binderPrefab.gameObject, parent);
            var binder = windowObject.GetComponent<IWindowBinder>();
            binder.Bind(_windowsContainer.RootContainer);

            return binder;
        }
    }
}
#endif
