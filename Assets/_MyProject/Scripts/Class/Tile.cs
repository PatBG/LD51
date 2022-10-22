
using System.Collections.Generic;
using UnityEngine;

public class Tile
{
    public int x;
    public int z;

    public static readonly Tile Invalid = new(int.MinValue, int.MinValue);

    public Tile(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public override bool Equals(object obj) => obj is Tile other && Equals(other);

    public bool Equals(Tile p) => x == p.x && z == p.z;

    public override int GetHashCode() => (x, z).GetHashCode();

    public static bool operator ==(Tile lhs, Tile rhs) => lhs.Equals(rhs);

    public static bool operator !=(Tile lhs, Tile rhs) => !(lhs == rhs);

    public static Tile operator +(Tile a, Tile b) => new(a.x + b.x, a.z + b.z);
    public static Tile operator -(Tile a, Tile b) => new(a.x - b.x, a.z - b.z);

    public Vector3 GetPosition()
    {
        return GetPosition(x, z);
    }

    public List<Tile> GetAdjacentTiles()
    {
        List<Tile> adjacentTiles = new();
        Tile[] deltas = (x % 2) == 0 ? Tile.Adjacent0 : Tile.Adjacent1;
        foreach (Tile delta in deltas)
        {
            adjacentTiles.Add(this + delta);
        }
        return adjacentTiles;
    }

    // ------------------------------------------------------------------------
    #region Static functions
    public static Tile GetTile(Vector3 position)
    {
        return new Tile(
            Mathf.RoundToInt(position.x),
            (Mathf.RoundToInt(position.x) % 2) == 0 ? Mathf.RoundToInt(position.z) : Mathf.RoundToInt(position.z - 0.5f));
    }

    public static Vector3 GetPosition(int x, int z)
    {
        return new Vector3(
            x,
            0,
            (x % 2) == 0 ? z : z + 0.5f);
    }

    public static readonly Tile[] Adjacent0 = new Tile[6]
    {
        new Tile(0, 1),
        new Tile(1, 0),
        new Tile(1, -1),
        new Tile(0, -1),
        new Tile(-1, -1),
        new Tile(-1, 0),
    };

    public static readonly Tile[] Adjacent1 = new Tile[6]
    {
        new Tile(0, 1),
        new Tile(1, 1),
        new Tile(1, 0),
        new Tile(0, -1),
        new Tile(-1, 0),
        new Tile(-1, 1),
    };
    #endregion
    // ------------------------------------------------------------------------
}
