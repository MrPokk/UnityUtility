using System;
using UnityEngine;

public class SelectImplementationAttribute : PropertyAttribute
{
    public Type FieldType { get; }
    public SelectImplementationAttribute(Type fieldType) => FieldType = fieldType;
}
