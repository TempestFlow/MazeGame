// Player.Combat.cs — Player partial (part 2): movement, attack, damage, healing, keys.
// ND1: [#8] partial class.

using MazeGame.Core.Enums;

namespace MazeGame.Core.Models;

public partial class Player
{
    /// <summary>
    /// Walks one tile in <paramref name="dir"/>. Always updates <see cref="Facing"/>
    /// even when blocked, so the next attack uses the latest direction.
    /// </summary>
    public void Move(Direction dir, GameWorld world)
    {
        Facing = dir;
        Position target = Position + Position.FromDirection(dir);
        if (world.IsWalkable(target))
            Position = target;
    }

    /// <summary>
    /// Strikes the tile in front of the player. Damages an enemy if present.
    /// Returns the attack position so the renderer can draw the sword visual.
    /// </summary>
    public Position Attack(GameWorld world)
    {
        Position attackPos = Position + Position.FromDirection(Facing);

        // [#17] out parameter
        if (world.TryGetEnemyAt(attackPos, out var enemy) && enemy != null)
        {
            enemy.TakeDamage(AttackPower);

            if (enemy.Health <= 0)
                OnAction?.Invoke($"Defeated {enemy.Name}!");
            else
                OnAction?.Invoke($"Hit {enemy.Name} for {AttackPower} damage!");
        }

        return attackPos;
    }

    /// <summary>Reduces HP, fires <see cref="OnDeath"/> when it reaches 0.</summary>
    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            Health = 0;
            OnDeath?.Invoke();
        }
    }

    /// <summary>Restores HP, capped at <see cref="MaxHealth"/>.</summary>
    public void Heal(int amount)
    {
        Health = Math.Min(Health + amount, MaxHealth);
    }

    public void CollectKey() => HasKey = true;

    public void UseKey() => HasKey = false;

    /// <summary>
    /// Required by <see cref="Abstract.GameObject"/>. The player is input-driven,
    /// so the per-frame update is a no-op.
    /// </summary>
    public override void Update(GameWorld world)
    {
        // Player actions are input-driven; logic lives in GameEngine.ProcessInput.
    }
}
