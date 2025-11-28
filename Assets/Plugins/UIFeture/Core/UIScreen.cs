#if VCONTAINER_AVAILABLE

namespace UIFeture.Core
{
    public abstract class UIScreen : WindowBinder
    {
        public override void Close()
        {
            base.Close();
        }

        public override void Open()
        {
            base.Open();
        }
    }
}
#endif
