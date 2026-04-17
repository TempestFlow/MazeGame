// GameWorld.cs — The central game state: map grid, entities, and tile flags.
// Demonstrates: [#13] System.Collections.Generic (List, Queue),
//               [#16] params keyword, [#17] out parameter initialization,
//               [#19] Bitwise operations with TileFlags, [#20] Null-conditional/coalescing.

using MazeGame.Core.Abstract;
using MazeGame.Core.Enums;
using MazeGame.Core.Interfaces;

namespace MazeGame.Core.Models;

/// <summary>
/// Represents the entire state of a single game level — the tile grid,
/// tile flags, all entities (player, enemies, items), and the message log.
/// Acts as the single source of truth that all game systems read and modify.
/// </summary>
public class GameWorld
{
    // ---------------------------------------------------------------
    // Map data
    // ---------------------------------------------------------------

    /// <summary>The 2D grid of ASCII characters representing the maze layout ('#' walls, '.' floors).</summary>
    public char[,] TileGrid { get; set; }

    /// <summary>
    /// [#19] Bitwise flag overlay for each tile — tracks dynamic properties
    /// like walkability, item presence, and enemy presence using bit flags.
    /// </summary>
    public TileFlags[,] Flags { get; set; }

    /// <summary>Width of the map (number of columns).</summary>
    public int Width { get; }

    /// <summary>Height of the map (number of rows).</summary>
    public int Height { get; }

    // ---------------------------------------------------------------
    // [#13] Collections — List, Queue for game entities and messages
    // ---------------------------------------------------------------

    /// <summary>All enemies currently in this level.</summary>
    public List<Enemy> Enemies { get; set; }

    /// <summary>All items (keys, potions, doors) currently in this level.</summary>
    public List<GameObject> Items { get; set; }

    /// <summary>The player character.</summary>
    public Player Player { get; set; }

    /// <summary>
    /// A queue of recent messages displayed in the HUD.
    /// Old messages are dequeued to keep the log short.
    /// </summary>
    public Queue<string> MessageLog { get; }

    /// <summary>Maximum number of messages to keep in the log.</summary>
    private const int MaxMessages = 5;

    // [#20] Null-coalescing assignment — cached description string
    private string? _cachedLevelDescription;

    /// <summary>
    /// Creates a new GameWorld with the given dimensions.
    /// Initializes all collections and grids.
    /// </summary>
    /// <param name="width">Map width in tiles.</param>
    /// <param name="height">Map height in tiles.</param>
    public GameWorld(int width, int height)
    {
        Width = width;
        Height = height;

        // Initialize the tile grid and flags grid
        TileGrid = new char[width, height];
        Flags = new TileFlags[width, height];

        // Initialize empty collections
        Enemies = new List<Enemy>();
        Items = new List<GameObject>();
        MessageLog = new Queue<string>();

        // Player will be set during level loading — create a placeholder
        Player = null!;
    }

    // ---------------------------------------------------------------
    // Tile and movement queries
    // ---------------------------------------------------------------

    /// <summary>
    /// Checks if a position is within the map bounds and walkable
    /// (not a wall, within grid limits).
    /// </summary>
    /// <param name="pos">The position to check.</param>
    /// <returns>True if the player/enemy can move to this tile.</returns>
    public bool IsWalkable(Position pos)
    {
        // [#11] Deconstruct — extract X and Y from the position
        var (x, y) = pos;

        // Bounds check
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;

        // Check the tile character — only floors, keys, potions, and doors are walkable
        char tile = TileGrid[x, y];
        return tile != '#';
    }

    /// <summary>
    /// Checks if an enemy currently occupies the given position.
    /// Used by enemy AI to avoid stacking on the same tile.
    /// </summary>
    public bool HasEnemyAt(Position pos)
    {
        // Check all active enemies for a position match
        foreach (var enemy in Enemies)
        {
            if (enemy.IsActive && enemy.Position == pos)
                return true;
        }
        return false;
    }

    // ---------------------------------------------------------------
    // [#17] out parameter initialization — TryGet pattern
    // ---------------------------------------------------------------

    /// <summary>
    /// Attempts to find an enemy at the given position.
    /// Uses the TryGet pattern with an out parameter.
    /// </summary>
    /// <param name="pos">Position to check.</param>
    /// <param name="enemy">The enemy found at that position, or null.</param>
    /// <returns>True if an active enemy was found at the position.</returns>
    public bool TryGetEnemyAt(Position pos, out Enemy? enemy)
    {
        // Search through all active enemies
        foreach (var e in Enemies)
        {
            if (e.IsActive && e.Position == pos)
            {
                enemy = e;
                return true;
            }
        }

        // No enemy found — set out parameter to null
        enemy = null;
        return false;
    }

    /// <summary>
    /// Attempts to find an interactable item at the given position.
    /// Uses the TryGet pattern with an out parameter for [#17].
    /// </summary>
    /// <param name="pos">Position to check.</param>
    /// <param name="interactable">The interactable found, or null.</param>
    /// <returns>True if an active interactable item was found.</returns>
    public bool TryGetInteractableAt(Position pos, out IInteractable? interactable)
    {
        // [#14] 'is' operator — check if items at this position implement IInteractable
        foreach (var item in Items)
        {
            if (item.IsActive && item.Position == pos && item is IInteractable interact)
            {
                interactable = interact;
                return true;
            }
        }

        interactable = null;
        return false;
    }

    // ---------------------------------------------------------------
    // [#16] params keyword — add multiple messages at once
    // ---------------------------------------------------------------

    /// <summary>
    /// Adds one or more messages to the HUD message log.
    /// Uses the params keyword so callers can pass any number of strings.
    /// Old messages are removed to keep the log at MaxMessages length.
    /// </summary>
    /// <param name="messages">One or more messages to add to the log.</param>
    public void AddMessages(params string[] messages)
    {
        foreach (var msg in messages)
        {
            MessageLog.Enqueue(msg);

            // Keep the log from growing too large
            while (MessageLog.Count > MaxMessages)
            {
                MessageLog.Dequeue();
            }
        }
    }

    // ---------------------------------------------------------------
    // [#19] Bitwise operations — update tile flags
    // ---------------------------------------------------------------

    /// <summary>
    /// Updates the TileFlags grid to reflect the current positions of
    /// enemies and items. Called each frame by the game engine.
    /// Uses bitwise OR (|) to set flags and bitwise AND-NOT (&amp; ~) to clear them.
    /// </summary>
    public void UpdateFlags()
    {
        // First, clear all dynamic flags (HasItem, HasEnemy) from every tile
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                // Set the Walkable flag based on the tile character
                if (TileGrid[x, y] != '#')
                    Flags[x, y] = TileFlags.Walkable | TileFlags.Visible;
                else
                    Flags[x, y] = TileFlags.Visible;

                // Clear the dynamic flags using bitwise AND with NOT
                Flags[x, y] &= ~TileFlags.HasItem;
                Flags[x, y] &= ~TileFlags.HasEnemy;
            }
        }

        // Set HasEnemy flags for each active enemy's position
        foreach (var enemy in Enemies)
        {
            if (enemy.IsActive)
            {
                var (ex, ey) = enemy.Position;
                if (ex >= 0 && ex < Width && ey >= 0 && ey < Height)
                {
                    // Use bitwise OR to add the HasEnemy flag
                    Flags[ex, ey] |= TileFlags.HasEnemy;
                }
            }
        }

        // Set HasItem flags for each active item's position
        foreach (var item in Items)
        {
            if (item.IsActive)
            {
                var (ix, iy) = item.Position;
                if (ix >= 0 && ix < Width && iy >= 0 && iy < Height)
                {
                    // Use bitwise OR to add the HasItem flag
                    Flags[ix, iy] |= TileFlags.HasItem;
                }
            }
        }
    }

    // ---------------------------------------------------------------
    // [#20] Null-coalescing assignment (??=)
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns a description of the current level.
    /// Uses ??= to lazily initialize the cached description string.
    /// </summary>
    public string GetLevelDescription()
    {
        // [#20] ??= operator — only compute the description once, then cache it
        _cachedLevelDescription ??= BuildLevelDescription();
        return _cachedLevelDescription;
    }

    /// <summary>
    /// Builds a summary string describing the level layout and enemy count.
    /// </summary>
    private string BuildLevelDescription()
    {
        int activeEnemies = Enemies.Count(e => e.IsActive);
        int activeItems = Items.Count(i => i.IsActive);
        return $"Level {Player?.Level ?? 0}: {Width}x{Height} maze, {activeEnemies} enemies, {activeItems} items";
    }

    /// <summary>
    /// Resets the cached level description so it will be rebuilt next time.
    /// Call this when the level state changes significantly.
    /// </summary>
    public void InvalidateDescription()
    {
        _cachedLevelDescription = null;
    }
}
