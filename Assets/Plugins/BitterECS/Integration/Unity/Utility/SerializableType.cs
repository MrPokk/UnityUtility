using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public class SerializableType : ISerializationCallbackReceiver
{
    [SerializeField, HideInInspector]
    private string _typeName;

    [SerializeField, HideInInspector]
    private string _assemblyName;

    [NonSerialized]
    private Type _type;

    public Type Type
    {
        get => _type;
        set
        {
            _type = value;
            if (value != null)
            {
                _typeName = value.FullName;
                _assemblyName = value.Assembly.FullName;
            }
            else
            {
                _typeName = string.Empty;
                _assemblyName = string.Empty;
            }
        }
    }

    public SerializableType() { }

    public SerializableType(Type type)
    {
        Type = type;
    }

    public void OnBeforeSerialize()
    { }

    public void OnAfterDeserialize()
    {
        if (string.IsNullOrEmpty(_typeName) || string.IsNullOrEmpty(_assemblyName))
        {
            _type = null;
            return;
        }

        try
        {
            var assembly = Assembly.Load(_assemblyName);
            _type = assembly.GetType(_typeName);
        }
        catch
        {
            _type = null;
        }
    }

    public static implicit operator Type(SerializableType serializableType) => serializableType?.Type;
    public static implicit operator SerializableType(Type type) => new(type);
}
