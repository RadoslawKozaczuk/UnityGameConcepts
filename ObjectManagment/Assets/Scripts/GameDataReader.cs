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

    public Quaternion ReadQuaternion()
    {
        Quaternion value;
        value.x = _reader.ReadSingle();
        value.y = _reader.ReadSingle();
        value.z = _reader.ReadSingle();
        value.w = _reader.ReadSingle();
        return value;
    }

    public Vector3 ReadVector3()
    {
        Vector3 value;
        value.x = _reader.ReadSingle();
        value.y = _reader.ReadSingle();
        value.z = _reader.ReadSingle();
        return value;
    }

    public Color32 ReadColor() => new Color32
        {
            r = _reader.ReadByte(),
            g = _reader.ReadByte(),
            b = _reader.ReadByte(),
            a = _reader.ReadByte()
        };
}
