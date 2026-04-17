// TileFlags.cs — A [Flags] enum for bitwise tile property tracking.
// Demonstrates: [#19] Bitwise operations with [Flags] enum.
// Each tile in the game world has a TileFlags value that encodes multiple
// boolean properties simultaneously using individual bits.

namespace MazeGame.Core.Enums;

/// <summary>
/// Bitwise flags that describe the state of a single tile in the game world.
/// Multiple flags can be combined using the | operator, checked with &amp;,
/// toggled with ^, and cleared with &amp; ~.
/// </summary>
[Flags]
public enum TileFlags : byte
{
    /// <summary>No flags set — the tile has no special properties.</summary>
    None = 0,

    /// <summary>The tile can be walked on by the player and enemies.</summary>
    Walkable = 1,       // bit 0

    /// <summary>The tile currently contains an item (key, potion, etc.).</summary>
    HasItem = 2,        // bit 1

    /// <summary>The tile currently contains an enemy.</summary>
    HasEnemy = 4,       // bit 2

    /// <summary>The tile is visible to the player (within line of sight).</summary>
    Visible = 8,        // bit 3

    /// <summary>The tile has been explored at least once by the player.</summary>
    Explored = 16       // bit 4
}
