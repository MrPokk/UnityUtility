namespace BitterECS.Core
{
    public struct ProviderComponent
    {
        public ILinkableProvider current;

        public ProviderComponent(ILinkableProvider currentValue)
        {
            current = currentValue;
        }
    }
}
