using BitterCMS.CMSSystem;
using BitterCMS.Utility.Interfaces;
using System;

namespace BitterCMS.Component
{
    public class ProviderComponent : IEntityComponent, IInitializable<ProviderComponent.ProviderProperty>
    {
        public ProviderProperty Properties { get; set; }

        public void Init(ProviderProperty property)
        {
            if (property == null)
                return;

            var ProviderBase = ProviderDatabase.Get(property.ProviderType);
            Properties = new ProviderProperty(ProviderBase);
        }

        public class ProviderProperty : InitializableProperty
        {
            public Type ProviderType { get; private set; }
            [NonSerialized] private CMSProviderCore _current;
            public CMSProviderCore Current { get => _current ??= ProviderDatabase.Get(ProviderType); set => _current = value; }
            public CMSProviderCore Original => ProviderDatabase.Get(ProviderType);

            public ProviderProperty(CMSProviderCore variableProvider)
            {
                ProviderType = variableProvider.ID;
                _current = variableProvider;
            }
        }
    }
}
