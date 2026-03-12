using System;

[Serializable]
public struct NodeId
{
    public int id;
    public string key;

    public NodeId(int id) : this()
    {
        this.id = id;
    }

    public NodeId(string key) : this()
    {
        this.key = key;
    }
}
