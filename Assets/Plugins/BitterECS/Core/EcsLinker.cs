using System.Collections.Generic;
using System.Linq;

namespace BitterECS.Core
{
    public static class EcsLinker
    {
        private static readonly Dictionary<ILinkableEntity, ILinkableView> s_linkedEntities = new();

        public static void Link(ILinkableEntity entity, ILinkableView view)
        {
            if (entity == null || view == null)
            {
                return;
            }

            if (!entity.Has<ViewComponent>())
            {
                entity.Add(new ViewComponent(view));
            }
            else
            {
                ref var viewComponent = ref entity.Get<ViewComponent>();
                viewComponent.current = view;
            }

            view.Init(new EcsViewProperty(entity.Properties.Presenter));
            s_linkedEntities[entity] = view;
        }

        public static void Unlink(ILinkableEntity entity)
        {
            if (entity == null)
                return;

            if (entity.Has<ViewComponent>())
            {
                entity.Remove<ViewComponent>();
                s_linkedEntities.Remove(entity);
            }
        }

        public static ILinkableView GetView(ILinkableEntity entity)
        {
            return entity == null || !s_linkedEntities.TryGetValue(entity, out var view) ? null : view;
        }

        public static ILinkableEntity GetEntity(ILinkableView view)
        {
            return s_linkedEntities.FirstOrDefault(x => x.Value == view).Key;
        }
    }
}
