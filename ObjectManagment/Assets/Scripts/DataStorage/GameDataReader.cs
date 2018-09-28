using System.IO;
using UnityEngine;

public class GameDataReader
{
    BinaryReader _reader;

    public GameDataReader(BinaryReader reader)
    {
        _reader = reader;
    }

    public float ReadFloat() => _reader.ReadSingle();

    public int ReadInt() => _reader.ReadInt32();

    public Quaternion ReadQuaternion() => new Quaternion
    {
        x = _reader.ReadSingle(),
        y = _reader.ReadSingle(),
        z = _reader.ReadSingle(),
        w = _reader.ReadSingle()
    };

    public Vector3 ReadVector3() => new Vector3
    {
        x = _reader.ReadSingle(),
        y = _reader.ReadSingle(),
        z = _reader.ReadSingle()
    };

    public Color32 ReadColor() => new Color32
    {
        r = _reader.ReadByte(),
        g = _reader.ReadByte(),
        b = _reader.ReadByte(),
        a = _reader.ReadByte()
    };

    public Random.State ReadRandomState()
    {
        return JsonUtility.FromJson<Random.State>(_reader.ReadString());
    }
}
