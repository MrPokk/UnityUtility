using System;
using System.Collections.Generic;
using System.IO;

namespace BitterCMS.CMSSystem.Exceptions
{
    public class ProviderDatabaseInitializationException : InvalidOperationException
    {
        public ProviderDatabaseInitializationException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    public class ProviderNotFoundException : KeyNotFoundException
    {
        public ProviderNotFoundException(string message) : base(message) { }
    }

    public class InvalidProviderException : ArgumentNullException
    {
        public InvalidProviderException(string message) : base(message) { }
    }

    public class EntityDatabaseInitializationException : InvalidOperationException
    {
        public EntityDatabaseInitializationException(string message, Exception inner)
            : base(message, inner)
        { }
    }

    public class EntityNotFoundException : FileNotFoundException
    {
        public EntityNotFoundException(string message) : base(message) { }
    }

    public class ComponentDatabaseInitializationException : InvalidOperationException
    {
        public ComponentDatabaseInitializationException(string message, Exception inner)
            : base(message, inner)
        { }
    }
}
