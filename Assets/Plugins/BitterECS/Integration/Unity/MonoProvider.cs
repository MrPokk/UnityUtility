using System;
using BitterECS.Core;
using UnityEngine;

namespace BitterECS.Integration
{
    public class MonoProvider : MonoBehaviour, ILinkableProvider
    {
        [SerializeField]
        public SerializableType presenterType;
        public Type PresenterType => presenterType.Type;

        public EcsProviderProperty Properties { get; protected set; }
        public EcsPresenter Presenter => Properties.Presenter;
        public EcsEntity Entity => Properties.Presenter.Get(Properties.Id);
        public ushort Id => Properties.Id;

        private ILinkableProvider _linkableProvider;
        private ITypedComponentProvider[] _componentProviders;

        protected virtual void Awake()
        {
            if (PresenterType == null)
            {
                throw new Exception($"Invalid entity type: null from {gameObject.name}");
            }

            _componentProviders = GetComponents<ITypedComponentProvider>();
            _linkableProvider = this;
            EcsWorld.Get(PresenterType)
            .AddTo<EcsEntity>()
            .WithPreInitCallback(ApplyComponent)
            .WithLink(_linkableProvider)
            .WithForce()
            .Create();

            var count = EcsWorld.GetToEntityType(PresenterType).EntityCount;
        }

        protected void ApplyComponent(EcsEntity entity)
        {
            foreach (var provider in _componentProviders)
            {
                provider.Apply(entity);
            }
        }

        protected void SyncComponent(EcsEntity entity)
        {
            foreach (var provider in _componentProviders)
            {
                provider.Sync(entity);
            }
        }

        public void Init(EcsProviderProperty property) => Properties = property;

        protected virtual void OnDestroy() => Properties?.Presenter.Remove(_linkableProvider.Entity);

        public void Dispose()
        {
            Properties = null;
            Destroy(gameObject);

            GC.SuppressFinalize(this);
        }
    }

    public class MonoProvider<T> : MonoProvider where T : EcsPresenter
    {
        protected override void Awake()
        {
            presenterType = new SerializableType(typeof(T));
            base.Awake();
        }
    }
}
