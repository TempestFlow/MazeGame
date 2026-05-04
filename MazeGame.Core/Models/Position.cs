// Position.cs — 2D grid coordinate value type.
// ND1: [#3] IEquatable, [#11] Deconstructor, [#12] Operator overloading.

using MazeGame.Core.Enums;

namespace MazeGame.Core.Models;

/// <summary>
/// Immutable-friendly 2D grid coordinate. <c>struct</c> for stack-allocation
/// and value semantics — Position is created and copied very frequently.
/// </summary>
public struct Position : IEquatable<Position>
{
    /// <summary>Horizontal coordinate (column).</summary>
    public int X { get; set; }

    /// <summary>Vertical coordinate (row).</summary>
    public int Y { get; set; }

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>[#11] Deconstructor — enables <c>var (x, y) = pos;</c>.</summary>
    public readonly void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    // [#3] IEquatable<Position>
    public readonly bool Equals(Position other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is Position other && Equals(other);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

    // [#12] Operator overloading
    public static Position operator +(Position a, Position b) => new(a.X + b.X, a.Y + b.Y);
    public static Position operator -(Position a, Position b) => new(a.X - b.X, a.Y - b.Y);
    public static bool operator ==(Position a, Position b) => a.Equals(b);
    public static bool operator !=(Position a, Position b) => !a.Equals(b);

    /// <summary>Manhattan (taxicab) distance — used for enemy detection range.</summary>
    public readonly int ManhattanDistance(Position other) =>
        Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>Maps a <see cref="Direction"/> to a unit-vector offset.</summary>
    public static Position FromDirection(Direction dir) => dir switch
    {
        Direction.Up    => new Position(0, -1),
        Direction.Down  => new Position(0, 1),
        Direction.Left  => new Position(-1, 0),
        Direction.Right => new Position(1, 0),
        _               => new Position(0, 0)
    };

    public override readonly string ToString() => $"({X}, {Y})";
}
