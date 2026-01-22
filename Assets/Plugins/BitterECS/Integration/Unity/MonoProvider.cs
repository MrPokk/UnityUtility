using System;
using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    public class MonoProvider : MonoBehaviour, ILinkableProvider
    {
        public SerializableType presenterType;
        public virtual Type PresenterType => presenterType.Type;

        private EcsProperty _properties;
        public EcsProperty Properties => _properties;
        public EcsEntity Entity => Properties?.Presenter.Get(Properties.Id) ?? UnLinkingRegistrationEntity();

        private ITypedComponentProvider[] _componentProviders;

        private void Init(EcsProperty property) => _properties ??= property;

        protected virtual void Registration() { }

        protected virtual void Awake()
        {
            if (PresenterType == null)
            {
                throw new Exception($"Invalid entity type: null from {gameObject.name}");
            }
            try
            {
                LinkingRegistrationEntity();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while creating entity: {ex.Message}");
            }
        }

        private EcsEntity UnLinkingRegistrationEntity() => Build.For(PresenterType)
            .Add<EcsEntity>()
            .WithPost(entity =>
            {
                RegistrationComponent(entity);
            })
            .Create();

        private EcsEntity LinkingRegistrationEntity() => Build.For(PresenterType)
            .Add<EcsEntity>()
            .WithPost(entity =>
            {
                Registration();
                RegistrationComponent(entity);
            })
            .WithLink(this)
            .Create();

        protected void RegistrationComponent(EcsEntity entity)
        {
            _componentProviders = GetComponents<ITypedComponentProvider>();
            if (_componentProviders == null)
            {
                return;
            }

            foreach (var provider in _componentProviders)
            {
                provider.Sync(entity);
                provider.Registration();
            }
        }


        private void Dispose()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            if (_properties == null)
            {
                return;
            }

            try
            {
                var entity = Entity;
                if (entity != null)
                {
                    _properties.Presenter?.Remove(entity);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error while removing entity: {ex.Message}");
            }
            finally
            {
                _properties = null;
                Destroy(gameObject);
            }

            GC.SuppressFinalize(this);
        }

        void IInitialize<EcsProperty>.Init(EcsProperty property) => Init(property);
        void IDisposable.Dispose() => Dispose();
    }

    public class MonoProvider<T> : MonoProvider where T : EcsPresenter
    {
        public override Type PresenterType => typeof(T);
    }
}
