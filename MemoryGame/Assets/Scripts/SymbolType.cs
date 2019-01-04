using UnityEngine;

public class SymbolType
{
    public int Id { get; private set; }
    public Sprite Normal { get; private set; }
    public Sprite Dropped { get; private set; }

    public SymbolType(int id, Sprite normal, Sprite dropped)
    {
        Id = id;
        Normal = normal;
        Dropped = dropped;
    }
}
