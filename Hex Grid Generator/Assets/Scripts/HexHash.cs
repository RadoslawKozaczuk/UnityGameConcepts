using UnityEngine;

public struct HexHash
{
    public float ExistanceProbability, Rotation, Scale, Choice;

    public static HexHash Create()
    {
        HexHash hash;
        hash.ExistanceProbability = Random.value * 0.999f;
        hash.Rotation = Random.value * 0.999f;
        hash.Scale = Random.value * 0.999f;
        hash.Choice = Random.value * 0.999f;
        return hash;
    }
}