namespace BitterCMS.UnityIntegration
{
    public interface IColliderInteraction
    {
        public void UpdateCollider(ProviderCollision source, ProviderCollision collision) { }
        public void EnterCollider(ProviderCollision source, ProviderCollision collision) { }
        public void ExitCollider(ProviderCollision source, ProviderCollision collision) { }
    }
}
