using BitterCMS.CMSSystem;
using BitterCMS.Component;
using UnityEngine;

namespace BitterCMS.UnityIntegration
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public abstract class ProviderCollision : CMSProviderCore
    {
        private void OnCollisionEnter2D(Collision2D other)
        {
            var ProviderCollision = other.collider.gameObject.GetComponent<ProviderCollision>();
            if (!ProviderCollision)
                return;

            GetModel().GetComponent<ColliderComponent>().OtherCollision = ProviderCollision;
            foreach (var interaction in InteractionCache<IColliderInteraction>.AllInteraction)
            {
                interaction.EnterCollider(GetComponent<ProviderCollision>(), ProviderCollision);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var ProviderCollision = other.gameObject.GetComponent<ProviderCollision>();
            if (!ProviderCollision)
                return;

            GetModel().GetComponent<ColliderComponent>().OtherCollision = ProviderCollision;
            foreach (var interaction in InteractionCache<IColliderInteraction>.AllInteraction)
            {
                interaction.EnterCollider(GetComponent<ProviderCollision>(), ProviderCollision);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var ProviderCollision = other.gameObject.GetComponent<ProviderCollision>();
            if (!ProviderCollision)
                return;

            GetModel().GetComponent<ColliderComponent>().OtherCollision = null;
            foreach (var interaction in InteractionCache<IColliderInteraction>.AllInteraction)
            {
                interaction.ExitCollider(GetComponent<ProviderCollision>(), ProviderCollision);
            }
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            var ProviderCollision = other.collider.gameObject.GetComponent<ProviderCollision>();
            if (!ProviderCollision)
                return;

            GetModel().GetComponent<ColliderComponent>().OtherCollision = null;
            foreach (var interaction in InteractionCache<IColliderInteraction>.AllInteraction)
            {
                interaction.ExitCollider(GetComponent<ProviderCollision>(), ProviderCollision);
            }
        }
    }
}
