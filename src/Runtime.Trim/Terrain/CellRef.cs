namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public class CellRef(int x, int y) : IEquatable<CellRef> {
    public static readonly CellRef[] CellDirections = {
        new(0, 1),
        new(1, 0),
        new(0, -1),
        new(-1, 0),
        new(1, 1),
        new(1, -1),
        new(-1, -1),
        new(-1, 1)
    };

    public static readonly float[] CellCosts = [1.0f, 1.41421f];

    public int X = x;
    public int Y = y;

    public CellRef() : this(0, 0) {
    }

    public static CellRef operator +(CellRef rhs, CellRef lhs)
    {
        return new CellRef(rhs.X + lhs.X, rhs.Y + lhs.Y);
    }

    public static bool operator ==(CellRef? lhs, CellRef? rhs)
    {
        return Equals(lhs, rhs);
    }

    public static bool operator !=(CellRef? lhs, CellRef? rhs)
    {
        return !Equals(lhs, rhs);
    }

    public bool Equals(CellRef? other)
    {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) {
            return false;
        }

        if (ReferenceEquals(this, obj)) {
            return true;
        }

        if (obj.GetType() != GetType()) {
            return false;
        }

        return Equals((CellRef)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}
