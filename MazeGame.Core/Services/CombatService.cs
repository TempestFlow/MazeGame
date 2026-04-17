// CombatService.cs — Handles combat logic, object descriptions, and type checking.
// Demonstrates: [#14] 'is' operator, [#21] Pattern matching (property/positional patterns),
//               [#5] Switch with 'when' keyword (additional usage).

using MazeGame.Core.Abstract;
using MazeGame.Core.Models;

namespace MazeGame.Core.Services;

/// <summary>
/// Service responsible for processing combat interactions and describing
/// game objects. Uses advanced pattern matching and type checking to
/// determine what happens when the player interacts with different objects.
/// </summary>
public class CombatService
{
    /// <summary>
    /// Processes the player's attack action. Checks the tile in front of the player
    /// and damages any enemy found there.
    /// Uses [#14] the 'is' operator for type checking when identifying enemies.
    /// </summary>
    /// <param name="world">The current game world.</param>
    /// <returns>A description of what happened, or null if the attack hit nothing.</returns>
    public string? ProcessAttack(GameWorld world)
    {
        // Get the attack position (tile in front of the player)
        Position attackPos = world.Player.Attack(world);

        // [#17] out parameter — try to find an enemy at the attack position
        if (world.TryGetEnemyAt(attackPos, out var enemy) && enemy != null)
        {
            // [#14] 'is' operator — check if the enemy is specifically a BossEnemy
            if (enemy is BossEnemy boss)
            {
                return $"You strike the boss {boss.Name}! HP: {boss.Health}/{boss.MaxHealth}";
            }

            // Regular enemy hit
            if (enemy.IsActive)
            {
                return $"You hit {enemy.Name}! HP: {enemy.Health}/{enemy.MaxHealth}";
            }
            else
            {
                return $"You defeated {enemy.Name}!";
            }
        }

        // Attack hit empty space
        return null;
    }

    // ---------------------------------------------------------------
    // [#21] Pattern matching — property patterns, positional patterns
    // ---------------------------------------------------------------

    /// <summary>
    /// Returns a rich description of a game object using advanced pattern matching.
    /// Uses property patterns ({ Property: value }), type patterns, and 'when' guards
    /// in a switch expression to produce context-aware descriptions.
    /// </summary>
    /// <param name="obj">The game object to describe.</param>
    /// <returns>A human-readable description string.</returns>
    public static string DescribeObject(GameObject obj)
    {
        // [#21] Switch expression with property patterns and 'when' guards
        return obj switch
        {
            // Dead enemy — property pattern checking Health <= 0
            Enemy { Health: <= 0 } => "A defeated enemy lies here.",

            // Wounded boss — property pattern + when guard for compound condition
            Enemy { IsBoss: true } e when e.Health < e.MaxHealth / 2
                => $"A wounded {e.Name} (HP:{e.Health}/{e.MaxHealth}) — finish it off!",

            // Healthy boss
            Enemy { IsBoss: true } e
                => $"A fearsome {e.Name} blocks the way! (HP:{e.Health}/{e.MaxHealth})",

            // Regular enemy
            Enemy e => $"{e.Name} (HP:{e.Health}/{e.MaxHealth})",

            // Strong potion — property pattern with value comparison
            Potion { HealAmount: > 30 } p
                => $"A glowing potion (+{p.HealAmount} HP)",

            // Regular potion
            Potion p => $"A potion (+{p.HealAmount} HP)",

            // Key item
            Key => "A shiny golden key",

            // Unlocked door
            Door { IsUnlocked: true } => "An open door leading deeper...",

            // Locked door
            Door => "A locked door — find the key!",

            // Catch-all for any unknown object type
            _ => "Something mysterious..."
        };
    }

    /// <summary>
    /// Describes a position using positional pattern matching with the Deconstruct method.
    /// Demonstrates [#11] Deconstructor usage in pattern matching and [#21] positional patterns.
    /// </summary>
    /// <param name="pos">The position to describe.</param>
    /// <param name="world">The game world for context.</param>
    /// <returns>A description of what's at this position.</returns>
    public static string DescribePosition(Position pos, GameWorld world)
    {
        // [#21] Positional pattern — uses Position.Deconstruct(out int x, out int y)
        // to match against specific coordinate patterns
        var (x, y) = pos;

        // Check bounds first
        if (x < 0 || x >= world.Width || y < 0 || y >= world.Height)
            return "Out of bounds";

        // Check what's at this position
        if (world.TryGetEnemyAt(pos, out var enemy))
            return DescribeObject(enemy!);

        if (world.TryGetInteractableAt(pos, out var interactable) && interactable is GameObject gameObj)
            return DescribeObject(gameObj);

        // Describe the tile itself
        return world.TileGrid[x, y] switch
        {
            '#' => "A solid wall",
            '.' => "Empty floor",
            _ => "Unknown terrain"
        };
    }

    /// <summary>
    /// Gets the sword visual character based on the player's facing direction.
    /// The sword appears as different characters depending on attack direction,
    /// mimicking classic Zelda-style combat.
    /// </summary>
    /// <param name="facing">The direction the player is facing.</param>
    /// <returns>The ASCII character to display for the sword.</returns>
    public static char GetSwordSymbol(Enums.Direction facing)
    {
        return facing switch
        {
            Enums.Direction.Up    => '|',   // Vertical sword pointing up
            Enums.Direction.Down  => '|',   // Vertical sword pointing down
            Enums.Direction.Left  => '-',   // Horizontal sword pointing left
            Enums.Direction.Right => '-',   // Horizontal sword pointing right
            _                     => '+'    // Default cross shape
        };
    }
}
