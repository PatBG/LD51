
public struct Tile
{
    public int x;
    public int z;

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
}
