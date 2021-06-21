using Commands;
using HECSFramework.Core;

namespace HECSFramework.Network
{
    public static class EntityExtention
    {
        public static void AddHecsNetworkComponent<T>(this IEntity entity, T component) where T : IComponent
        {
            entity.AddHecsComponent(component);
            EntityManager.Command(new AddNetWorkComponentCommand { Component = component });
        }

        public static void RemoveHecsNetworkComponent<T>(this IEntity entity, T component) where T : IComponent
        {
            entity.RemoveHecsComponent(component);
            EntityManager.Command(new RemoveNetWorkComponentCommand { Component = component });
        }
    }
}