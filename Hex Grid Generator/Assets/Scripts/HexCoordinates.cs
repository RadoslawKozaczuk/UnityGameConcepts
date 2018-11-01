using System;
using UnityEngine;

[Serializable]
public struct HexCoordinates
{
    // If you add all three coordinates together you will always get zero. 
    // If you increment one coordinate, you have to decrement another. 
    // Indeed, this produces six possible directions of movement. 
    // These coordinates are typically known as cube coordinates, as they are three-dimensional and the topology resembles a cube.
    public int X
    {
        get
        {
            return _x;
        }
    }

    public int Z
    {
        get
        {
            return _z;
        }
    }

    // As we already store the X and Z coordinates, we don't need to store the Y coordinate. 
    // We can include a property that computes it on demand.
    public int Y
    {
        get
        {
            return -X - Z;
        }
    }

    [SerializeField]
    int _x, _z;

    public HexCoordinates(int x, int z)
    {
        _x = x;
        _z = z;
    }

    // Let's fix out those X coordinates so they are aligned along a straight axis. 
    // We can do this by undoing the horizontal shift.
    // The result is typically know as axial coordinates.
    public static HexCoordinates FromOffsetCoordinates(int x, int z) => new HexCoordinates(x - z / 2, z);

    public override string ToString() => "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";

    public string ToStringOnSeparateLines() => X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
}