// GameWorld.cs — Central game state: map grid, entities, tile flags.
// ND1: [#13] Generic collections, [#16] params, [#17] out, [#19] bitwise, [#20] null-coalescing.
// ND2: [ND2 #1] IEnumerable<GameObject>, [ND2 #3] yield return iterator.

using System.Collections;
using MazeGame.Core.Abstract;
using MazeGame.Core.Enums;
using MazeGame.Core.Interfaces;
using MazeGame.Core.Utils;

namespace MazeGame.Core.Models;

/// <summary>
/// Single source of truth for one level: the tile grid, tile flags, all
/// entities (player, enemies, items), and a short HUD message log.
/// [ND2 #1] Implements <see cref="IEnumerable{GameObject}"/> so callers
/// can iterate every live entity in priority order via foreach or LINQ.
/// </summary>
public class GameWorld : IEnumerable<GameObject>
{
    /// <summary>2D grid of ASCII characters ('#' walls, '.' floors).</summary>
    public char[,] TileGrid { get; set; }

    /// <summary>[#19] Bitwise flag overlay for each tile.</summary>
    public TileFlags[,] Flags { get; set; }

    /// <summary>Map width (columns).</summary>
    public int Width { get; }

    /// <summary>Map height (rows).</summary>
    public int Height { get; }

    /// <summary>All enemies currently in this level.</summary>
    public List<Enemy> Enemies { get; set; }

    /// <summary>All items (keys, potions, doors) currently in this level.</summary>
    public List<GameObject> Items { get; set; }

    /// <summary>The player character.</summary>
    public Player Player { get; set; }

    /// <summary>Bounded queue of recent HUD messages.</summary>
    public Queue<string> MessageLog { get; }

    private const int MaxMessages = 5;

    // [#20] Null-coalescing assignment — cached level description.
    private string? _cachedLevelDescription;

    public GameWorld(int width, int height)
    {
        Width = width;
        Height = height;
        TileGrid = new char[width, height];
        Flags = new TileFlags[width, height];
        Enemies = new List<Enemy>();
        Items = new List<GameObject>();
        MessageLog = new Queue<string>();
        Player = null!;  // Set during level loading.
    }

    // ---------------------------------------------------------------
    // Tile and movement queries
    // ---------------------------------------------------------------

    /// <summary>True when <paramref name="pos"/> is in-bounds and not a wall.</summary>
    public bool IsWalkable(Position pos)
    {
        // [#11] Deconstruct
        var (x, y) = pos;
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return TileGrid[x, y] != '#';
    }

    /// <summary>True when an active enemy occupies <paramref name="pos"/>.</summary>
    public bool HasEnemyAt(Position pos)
    {
        foreach (var enemy in Enemies)
        {
            if (enemy.IsActive && enemy.Position == pos)
                return true;
        }
        return false;
    }

    // [#17] out parameter — TryGet pattern.
    public bool TryGetEnemyAt(Position pos, out Enemy? enemy)
    {
        foreach (var e in Enemies)
        {
            if (e.IsActive && e.Position == pos)
            {
                enemy = e;
                return true;
            }
        }
        enemy = null;
        return false;
    }

    public bool TryGetInteractableAt(Position pos, out IInteractable? interactable)
    {
        // [#14] 'is' operator
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

    // [#16] params — append any number of messages, oldest dropped past MaxMessages.
    public void AddMessages(params string[] messages)
    {
        foreach (var msg in messages)
        {
            MessageLog.Enqueue(msg);
            while (MessageLog.Count > MaxMessages)
            {
                MessageLog.Dequeue();
            }
        }
    }

    // ---------------------------------------------------------------
    // [#19] Bitwise — refresh dynamic tile flags each tick.
    // ---------------------------------------------------------------
    public void UpdateFlags()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (TileGrid[x, y] != '#')
                    Flags[x, y] = TileFlags.Walkable | TileFlags.Visible;
                else
                    Flags[x, y] = TileFlags.Visible;

                Flags[x, y] &= ~TileFlags.HasItem;
                Flags[x, y] &= ~TileFlags.HasEnemy;
            }
        }

        foreach (var enemy in Enemies)
        {
            if (enemy.IsActive)
            {
                var (ex, ey) = enemy.Position;
                if (ex >= 0 && ex < Width && ey >= 0 && ey < Height)
                    Flags[ex, ey] |= TileFlags.HasEnemy;
            }
        }

        foreach (var item in Items)
        {
            if (item.IsActive)
            {
                var (ix, iy) = item.Position;
                if (ix >= 0 && ix < Width && iy >= 0 && iy < Height)
                    Flags[ix, iy] |= TileFlags.HasItem;
            }
        }
    }

    /// <summary>Returns a cached level description, building it on first call. Uses [#20] ??=.</summary>
    public string GetLevelDescription()
    {
        _cachedLevelDescription ??= BuildLevelDescription();
        return _cachedLevelDescription;
    }

    private string BuildLevelDescription()
    {
        int activeEnemies = Enemies.Count(e => e.IsActive);
        int activeItems = Items.Count(i => i.IsActive);
        return $"Level {Player?.Level ?? 0}: {Width}x{Height} maze, {activeEnemies} enemies, {activeItems} items";
    }

    public void InvalidateDescription()
    {
        _cachedLevelDescription = null;
    }

    // ---------------------------------------------------------------
    // [ND2 #1] IEnumerable<GameObject>
    // [ND2 #2] Returns the hand-written GameObjectEnumerator (not a yield).
    // ---------------------------------------------------------------

    /// <summary>
    /// [ND2 #1] Walks the world's live entities in priority order:
    /// player → active enemies → active items. Built on top of the
    /// hand-written <see cref="GameObjectEnumerator"/> so [ND2 #1] and
    /// [ND2 #2] are satisfied independently.
    /// </summary>
    public IEnumerator<GameObject> GetEnumerator() => new GameObjectEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // ---------------------------------------------------------------
    // [ND2 #3] yield return — adjacent walkable tiles
    // ---------------------------------------------------------------

    /// <summary>
    /// [ND2 #3] Lazily yields every walkable orthogonal neighbour of
    /// <paramref name="pos"/>. Used by enemy AI / pathfinding helpers.
    /// </summary>
    public IEnumerable<Position> GetWalkableNeighbors(Position pos)
    {
        Position up    = new(pos.X,     pos.Y - 1);
        Position down  = new(pos.X,     pos.Y + 1);
        Position left  = new(pos.X - 1, pos.Y);
        Position right = new(pos.X + 1, pos.Y);

        if (IsWalkable(up))    yield return up;
        if (IsWalkable(down))  yield return down;
        if (IsWalkable(left))  yield return left;
        if (IsWalkable(right)) yield return right;
    }
}
