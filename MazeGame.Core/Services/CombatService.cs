// CombatService.cs — Combat resolution + object descriptions.
// ND1: [#14] 'is' operator, [#21] pattern matching, [#5] switch-when.

using MazeGame.Core.Abstract;
using MazeGame.Core.Models;

namespace MazeGame.Core.Services;

/// <summary>
/// Combat / interaction service. Stateless — every method takes the world
/// it operates on. Heavy use of pattern matching for terse, exhaustive
/// descriptions of game-object state.
/// </summary>
public class CombatService
{
    /// <summary>
    /// Resolves the player's attack on the tile they face.
    /// Returns a human-readable description, or null when the attack hits empty space.
    /// </summary>
    public string? ProcessAttack(GameWorld world)
    {
        Position attackPos = world.Player.Attack(world);

        // [#17] out parameter
        if (world.TryGetEnemyAt(attackPos, out var enemy) && enemy != null)
        {
            // [#14] 'is' — check for boss subtype.
            if (enemy is BossEnemy boss)
                return $"You strike the boss {boss.Name}! HP: {boss.Health}/{boss.MaxHealth}";

            return enemy.IsActive
                ? $"You hit {enemy.Name}! HP: {enemy.Health}/{enemy.MaxHealth}"
                : $"You defeated {enemy.Name}!";
        }

        return null;
    }

    /// <summary>
    /// [#21] Switch expression with property patterns + 'when' guards. Returns
    /// a context-aware description for any GameObject in the world.
    /// </summary>
    public static string DescribeObject(GameObject obj) => obj switch
    {
        Enemy { Health: <= 0 } => "A defeated enemy lies here.",

        Enemy { IsBoss: true } e when e.Health < e.MaxHealth / 2
            => $"A wounded {e.Name} (HP:{e.Health}/{e.MaxHealth}) — finish it off!",

        Enemy { IsBoss: true } e
            => $"A fearsome {e.Name} blocks the way! (HP:{e.Health}/{e.MaxHealth})",

        Enemy e => $"{e.Name} (HP:{e.Health}/{e.MaxHealth})",

        Potion { HealAmount: > 30 } p => $"A glowing potion (+{p.HealAmount} HP)",
        Potion p => $"A potion (+{p.HealAmount} HP)",

        Key => "A shiny golden key",

        Door { IsUnlocked: true } => "An open door leading deeper...",
        Door => "A locked door — find the key!",

        _ => "Something mysterious..."
    };

    /// <summary>
    /// [#21] Positional patterns via Position.Deconstruct. Describes whatever
    /// occupies <paramref name="pos"/> — entity, item, or raw tile.
    /// </summary>
    public static string DescribePosition(Position pos, GameWorld world)
    {
        var (x, y) = pos;

        if (x < 0 || x >= world.Width || y < 0 || y >= world.Height)
            return "Out of bounds";

        if (world.TryGetEnemyAt(pos, out var enemy))
            return DescribeObject(enemy!);

        if (world.TryGetInteractableAt(pos, out var interactable) && interactable is GameObject gameObj)
            return DescribeObject(gameObj);

        return world.TileGrid[x, y] switch
        {
            '#' => "A solid wall",
            '.' => "Empty floor",
            _ => "Unknown terrain"
        };
    }

    /// <summary>Returns the ASCII glyph used to render the player's sword swing.</summary>
    public static char GetSwordSymbol(Enums.Direction facing) => facing switch
    {
        Enums.Direction.Up    => '|',
        Enums.Direction.Down  => '|',
        Enums.Direction.Left  => '-',
        Enums.Direction.Right => '-',
        _                     => '+'
    };
}
