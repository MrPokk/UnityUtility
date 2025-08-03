using System;
using System.Linq;

namespace BitterECS.Utility
{
    public static class ReflectionUtility
    {
        public static Type[] FindAllImplement<T>()
        {
            var type = typeof(T);

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(type))
                .ToArray();
        }

        public static Type[] FindAllAssignments<T>()
        {
            var type = typeof(T);

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => !t.IsAbstract && type.IsAssignableFrom(t))
                .ToArray();
        }
    }
}
