// Position.cs — A value type representing a 2D grid coordinate.
// Demonstrates: [#3] IEquatable<T>, [#11] Deconstructor, [#12] Operator overloading.
// Position is a struct because it is small (two ints), immutable-friendly,
// and used very frequently throughout the game — value semantics avoid heap allocations.

using MazeGame.Core.Enums;

namespace MazeGame.Core.Models;

/// <summary>
/// Represents a 2D grid coordinate (X, Y) in the game world.
/// Implements IEquatable for proper value comparison, supports deconstruction
/// into (x, y) tuples, and overloads arithmetic/equality operators for convenience.
/// </summary>
public struct Position : IEquatable<Position>
{
    /// <summary>The horizontal coordinate (column) in the grid.</summary>
    public int X { get; set; }

    /// <summary>The vertical coordinate (row) in the grid.</summary>
    public int Y { get; set; }

    /// <summary>
    /// Creates a new Position at the given grid coordinates.
    /// </summary>
    /// <param name="x">Horizontal (column) coordinate.</param>
    /// <param name="y">Vertical (row) coordinate.</param>
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    // ---------------------------------------------------------------
    // [#11] Deconstructor — enables: var (x, y) = position;
    // ---------------------------------------------------------------

    /// <summary>
    /// Deconstructs this Position into its X and Y components.
    /// Usage: <c>var (x, y) = somePosition;</c>
    /// </summary>
    public readonly void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }

    // ---------------------------------------------------------------
    // [#3] IEquatable<Position> — proper value equality
    // ---------------------------------------------------------------

    /// <summary>
    /// Determines whether two positions represent the same grid cell.
    /// </summary>
    public readonly bool Equals(Position other)
    {
        return X == other.X && Y == other.Y;
    }

    /// <summary>
    /// Override of object.Equals to ensure consistent equality behavior.
    /// </summary>
    public override readonly bool Equals(object? obj)
    {
        return obj is Position other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code combining X and Y for use in dictionaries and hash sets.
    /// </summary>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    // ---------------------------------------------------------------
    // [#12] Operator overloading — arithmetic and equality operators
    // ---------------------------------------------------------------

    /// <summary>Adds two positions (vector addition).</summary>
    public static Position operator +(Position a, Position b)
    {
        return new Position(a.X + b.X, a.Y + b.Y);
    }

    /// <summary>Subtracts one position from another (vector subtraction).</summary>
    public static Position operator -(Position a, Position b)
    {
        return new Position(a.X - b.X, a.Y - b.Y);
    }

    /// <summary>Checks if two positions are equal (same grid cell).</summary>
    public static bool operator ==(Position a, Position b)
    {
        return a.Equals(b);
    }

    /// <summary>Checks if two positions are not equal (different grid cells).</summary>
    public static bool operator !=(Position a, Position b)
    {
        return !a.Equals(b);
    }

    // ---------------------------------------------------------------
    // Utility methods
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns the Manhattan distance (taxicab distance) between this position
    /// and another. Used for enemy detection range checks.
    /// </summary>
    public readonly int ManhattanDistance(Position other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }

    /// <summary>
    /// Converts a Direction enum into a unit-vector Position offset.
    /// For example, Direction.Up returns (0, -1) because Y decreases upward.
    /// </summary>
    /// <param name="dir">The direction to convert.</param>
    /// <returns>A Position representing the unit offset for that direction.</returns>
    public static Position FromDirection(Direction dir)
    {
        // Each direction maps to a single-step offset on the grid
        return dir switch
        {
            Direction.Up    => new Position(0, -1),
            Direction.Down  => new Position(0, 1),
            Direction.Left  => new Position(-1, 0),
            Direction.Right => new Position(1, 0),
            _               => new Position(0, 0)   // None = no movement
        };
    }

    /// <summary>
    /// Returns a human-readable string like "(3, 5)".
    /// </summary>
    public override readonly string ToString()
    {
        return $"({X}, {Y})";
    }
}
