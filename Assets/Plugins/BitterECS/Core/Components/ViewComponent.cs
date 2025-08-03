namespace BitterECS.Core
{
    public struct ViewComponent
    {
        public ILinkableView current;

        public ViewComponent(ILinkableView currentValue)
        {
            current = currentValue;
        }
    }
}
