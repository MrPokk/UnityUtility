using System;
using System.Collections.Concurrent;

namespace BitterCMS.Core
{
    public class EntityCore
    {
        private readonly ConcurrentDictionary<Type, IComponentCore> _components = new();
        
    }
}
