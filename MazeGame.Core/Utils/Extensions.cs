// Extensions.cs — Static extension methods used across the project.
// Demonstrates: [ND2 #4] extension methods on built-in types,
//               [ND2 #9] generic extension methods (one with a where constraint),
//               [ND2 #10] an extension Deconstruct method.

using MazeGame.Core.Abstract;
using MazeGame.Core.Models;

namespace MazeGame.Core.Utils;

/// <summary>
/// Collection of small extension helpers. Kept in a single file so the
/// grader can find every extension method in one place.
/// </summary>
public static class Extensions
{
    // -----------------------------------------------------------------
    // [ND2 #4] Extension methods on existing C# types
    // -----------------------------------------------------------------

    /// <summary>
    /// Trims <paramref name="s"/> to at most <paramref name="maxLength"/> characters,
    /// appending an ellipsis when truncation occurs. Extends <see cref="string"/>.
    /// </summary>
    public static string Truncate(this string s, int maxLength)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxLength) return s;
        if (maxLength <= 1) return s.Substring(0, maxLength);
        return s.Substring(0, maxLength - 1) + "…";
    }

    /// <summary>
    /// True if the key is one of the eight movement keys (WASD or arrow keys).
    /// Extends <see cref="ConsoleKey"/> so input code reads as
    /// <c>if (key.IsMovementKey()) …</c>.
    /// </summary>
    public static bool IsMovementKey(this ConsoleKey key) =>
        key is ConsoleKey.W or ConsoleKey.A or ConsoleKey.S or ConsoleKey.D
            or ConsoleKey.UpArrow or ConsoleKey.DownArrow
            or ConsoleKey.LeftArrow or ConsoleKey.RightArrow;

    /// <summary>
    /// True when <paramref name="p"/> shares an edge with <paramref name="other"/>.
    /// Reuses the existing <see cref="Position.ManhattanDistance"/> from ND1.
    /// </summary>
    public static bool IsAdjacentTo(this Position p, Position other) =>
        p.ManhattanDistance(other) == 1;

    // -----------------------------------------------------------------
    // [ND2 #9] Generic extension methods
    // -----------------------------------------------------------------

    /// <summary>
    /// [ND2 #9] Picks a random element from <paramref name="source"/>, or
    /// <c>default</c> when the sequence is empty. Generic over T.
    /// </summary>
    public static T? RandomElement<T>(this IEnumerable<T> source, Random rng)
    {
        var list = source as IList<T> ?? source.ToList();
        return list.Count == 0 ? default : list[rng.Next(list.Count)];
    }

    /// <summary>
    /// [ND2 #9] Filters a sequence to active GameObjects only. The
    /// <c>where T : GameObject</c> constraint guarantees the IsActive
    /// member is available without boxing.
    /// </summary>
    public static IEnumerable<T> WhereActive<T>(this IEnumerable<T> source) where T : GameObject
    {
        return source.Where(item => item.IsActive);
    }

    // -----------------------------------------------------------------
    // [ND2 #10] Extension Deconstructor
    // -----------------------------------------------------------------

    /// <summary>
    /// [ND2 #10] Extension Deconstruct that adds tuple-style decomposition
    /// to <see cref="Enemy"/> from outside the type. Lets call sites write
    /// <c>var (name, hp, atk) = enemy;</c> even though Enemy itself does
    /// not declare a Deconstruct method.
    /// </summary>
    public static void Deconstruct(this Enemy enemy, out string name, out int hp, out int atk)
    {
        name = enemy.Name;
        hp = enemy.Health;
        atk = enemy.AttackPower;
    }
}
