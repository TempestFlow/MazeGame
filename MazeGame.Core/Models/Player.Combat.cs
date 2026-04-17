// Player.Combat.cs — The player character (partial class, part 2: combat and movement).
// Demonstrates: [#8] Partial class (second file for Player).
// This file handles movement, attacking, taking damage, healing,
// and key collection. Properties and formatting are in Player.cs.

using MazeGame.Core.Enums;

namespace MazeGame.Core.Models;

/// <summary>
/// Partial class for Player — this part handles combat mechanics,
/// movement through the maze, healing, and key interactions.
/// </summary>
public partial class Player
{
    /// <summary>
    /// Moves the player one tile in the specified direction, if the destination is walkable.
    /// Also updates the player's facing direction for attacks.
    /// </summary>
    /// <param name="dir">The direction to move.</param>
    /// <param name="world">The game world to check for wall collisions.</param>
    public void Move(Direction dir, GameWorld world)
    {
        // Always update facing direction, even if we can't move
        Facing = dir;

        // Calculate the target position by adding the direction offset
        Position target = Position + Position.FromDirection(dir);

        // Only move if the target tile is within bounds and walkable
        if (world.IsWalkable(target))
        {
            Position = target;
        }
    }

    /// <summary>
    /// Attacks the tile directly in front of the player (based on Facing direction).
    /// If an enemy is there, it takes damage equal to the player's AttackPower.
    /// Returns the attack position so the renderer can show the sword visual.
    /// </summary>
    /// <param name="world">The game world to check for enemies.</param>
    /// <returns>The position where the attack lands (for sword visual).</returns>
    public Position Attack(GameWorld world)
    {
        // The attack hits the tile the player is facing
        Position attackPos = Position + Position.FromDirection(Facing);

        // [#17] out parameter — check if there's a game object at the attack position
        if (world.TryGetEnemyAt(attackPos, out var enemy) && enemy != null)
        {
            // Deal damage to the enemy
            enemy.TakeDamage(AttackPower);

            // Log the attack to the message system
            if (enemy.Health <= 0)
            {
                OnAction?.Invoke($"Defeated {enemy.Name}!");
            }
            else
            {
                OnAction?.Invoke($"Hit {enemy.Name} for {AttackPower} damage!");
            }
        }

        return attackPos;
    }

    /// <summary>
    /// Reduces the player's health by the given amount.
    /// If health drops to zero or below, fires the OnDeath event.
    /// </summary>
    /// <param name="amount">Amount of damage to take.</param>
    public void TakeDamage(int amount)
    {
        Health -= amount;

        // Clamp health to zero — no negative health
        if (Health <= 0)
        {
            Health = 0;
            // Fire the death event so the game engine can handle Game Over
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Heals the player by the given amount, capped at MaxHealth.
    /// </summary>
    /// <param name="amount">Amount of health to restore.</param>
    public void Heal(int amount)
    {
        Health = Math.Min(Health + amount, MaxHealth);
    }

    /// <summary>
    /// Gives the player a key, allowing them to open doors.
    /// </summary>
    public void CollectKey()
    {
        HasKey = true;
    }

    /// <summary>
    /// Consumes the player's key when they open a door.
    /// </summary>
    public void UseKey()
    {
        HasKey = false;
    }

    /// <summary>
    /// Player update — the player is driven by input, not AI,
    /// so this is a no-op. All player logic happens in response to key presses.
    /// </summary>
    public override void Update(GameWorld world)
    {
        // Player actions are input-driven, handled by GameEngine.ProcessInput().
        // This method exists to satisfy the abstract GameObject.Update() contract.
    }
}
