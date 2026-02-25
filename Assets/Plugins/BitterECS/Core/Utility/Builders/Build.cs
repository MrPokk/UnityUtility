using System;

namespace BitterECS.Core
{
    public static class Build
    {
        public static Builder For<T>() where T : EcsPresenter, new() => new(EcsWorld.Get(typeof(T)));
        public static Builder For(Type presenterType) => new(EcsWorld.Get(presenterType));
    }

    public readonly ref struct Builder
    {
        private readonly EcsPresenter _presenter;
        public Builder(EcsPresenter presenter) => _presenter = presenter;

        public EcsEvent Event(Priority priority = Priority.Medium) => new(_presenter, priority);
        public EcsFilter Filter() => new(_presenter);
        public EntityBuilder Add() => new(_presenter);
        public EntityBuilderGeneric<T> Add<T>() where T : struct => new(_presenter);
        public EntityDestroyer Remove(EcsEntity entity) => new(_presenter, entity);
        public EntityDestroyer Remove<T>(T entity) where T : struct => entity is EcsEntity e ? new(_presenter, e) : throw new ArgumentException("Provided type is not an EcsEntity");
    }
}
