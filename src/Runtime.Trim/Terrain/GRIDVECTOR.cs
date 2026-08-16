using System.Numerics;

namespace ConquestFrontierWarsRay.Runtime.Trim.Terrain;

public class Gridvector : IEquatable<Gridvector> {
    public static int GRIDSIZE = 4096;
    public static int HALFGRID = 2048;

    protected long X;
    protected long Y;

    public Gridvector(Vector2 vec) {
        X = (((long)vec.X * 4) + ((GRIDSIZE - 1) / 2)) / GRIDSIZE;
        Y = (((long)vec.Y * 4) + ((GRIDSIZE - 1) / 2)) / GRIDSIZE;
    }

    public Gridvector(Vector3 vector3) : this(new Vector2(vector3.X, vector3.Y)) {}

    public Gridvector Init(float x, float y)
    {
        X = (long)x * 4;
        Y = (long)y * 4;
        return this;
    }

    public Gridvector Quarterpos()
    {
        X |= 1;
        Y |= 1;
        return this;
    }

    public Gridvector Centerpos() {
        X &= ~3;
        X |= 2;
        Y &= ~3;
        Y |= 2;
        return this;
    }

    public Gridvector Cornerpos()
    {
        X &= ~3;
        Y &= ~3;
        return this;
    }

    public Gridvector Zero()
    {
        X = Y = 0;
        return this;
    }

    public Vector3 AsVector3() {
        return new Vector3(X * GRIDSIZE / 4, Y * GRIDSIZE / 4, 0);
    }

    public float GetX() {
        return X * 0.25f;
    }

    public float GetY() {
        return Y * 0.25f;
    }

    public int GetIntX() {
        return (int)X >> 2;
    }

    public int GetIntY() {
        return (int)Y >> 2;
    }

    public override bool Equals(object? vec) {
        if (vec is Gridvector vec1) {
            return vec1.X == X && vec1.Y == Y;
        }

        return false;
    }

    public bool Equals(Gridvector? other)
    {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public static bool operator ==(Gridvector a, Gridvector b) {
        return a.Equals(b);
    }

    public static bool operator !=(Gridvector a, Gridvector b) {
        return !a.Equals(b);
    }

    public bool IsZero() {
        return X == 0 && Y == 0;
    }

    public bool IsMostlyEqual(Gridvector vec)
    {
        return (X & ~3) == (vec.X & ~3) && (Y & ~3) == (vec.Y & ~3);
    }

    public static Gridvector Create(Vector3 vector3) {
        return new Gridvector(vector3);
    }
}
