using System;
using UnityEngine;
namespace BitterECS.Integration.Unity
{
    public class SelectImplementationAttribute : PropertyAttribute
    {
        public Type FieldType { get; }
        public SelectImplementationAttribute(Type fieldType) => FieldType = fieldType;
    }
}