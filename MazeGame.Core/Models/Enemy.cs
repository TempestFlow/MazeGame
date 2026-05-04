// Enemy.cs — A roaming hostile entity. ND1: [#2] IComparable<Enemy>, [#5] switch-when AI.

using MazeGame.Core.Abstract;
using MazeGame.Core.Enums;

namespace MazeGame.Core.Models;

/// <summary>
/// An enemy character. Roams the maze, chases the player when in detection
/// range, attacks on contact, and flees when low on health. Bosses (see
/// <see cref="BossEnemy"/>) override <see cref="IsBoss"/> for special AI.
/// Implements <see cref="IComparable{Enemy}"/> for difficulty-based sort.
/// </summary>
public class Enemy : GameObject, IComparable<Enemy>
{
    /// <summary>Display name (e.g. "Goblin", "Skeleton").</summary>
    public string Name { get; set; }

    /// <summary>Current HP. Defeat occurs at 0.</summary>
    public int Health { get; set; }

    /// <summary>Maximum HP.</summary>
    public int MaxHealth { get; set; }

    /// <summary>Damage per attack.</summary>
    public int AttackPower { get; set; }

    /// <summary>Manhattan distance at which the enemy detects the player.</summary>
    public int DetectionRange { get; set; }

    /// <summary>Numeric difficulty rating (sort key).</summary>
    public int Difficulty { get; set; }

    /// <summary>Boss flag — used by AI and pattern matching. Overridden by BossEnemy.</summary>
    public virtual bool IsBoss => false;

    private static readonly Random _random = new();
    private int _moveTimer;

    /// <summary>
    /// Creates a new enemy. [#15] Default and named arguments allow flexible callers.
    /// </summary>
    public Enemy(Position position, string name = "Goblin", int health = 50,
                 int attackPower = 10, int detectionRange = 5, int difficulty = 1)
        : base(position, 'E')
    {
        Name = name;
        Health = health;
        MaxHealth = health;
        AttackPower = attackPower;
        DetectionRange = detectionRange;
        Difficulty = difficulty;
        _moveTimer = 0;
    }

    // [#2] IComparable<Enemy> — sort by difficulty, nulls last.
    public int CompareTo(Enemy? other)
    {
        if (other is null) return 1;
        return Difficulty.CompareTo(other.Difficulty);
    }

    /// <summary>
    /// Per-frame AI. [#5] Switch with 'when' guards picks the behavior:
    /// boss-charge, flee-when-low-HP, attack-adjacent, chase, wander.
    /// </summary>
    public override void Update(GameWorld world)
    {
        if (Health <= 0 || !IsActive) return;

        // Throttle to 1 move every 3 frames so the player can outpace enemies.
        _moveTimer++;
        if (_moveTimer < 3) return;
        _moveTimer = 0;

        int distToPlayer = Position.ManhattanDistance(world.Player.Position);

        // [#5] Switch with 'when' clauses
        switch (this)
        {
            case { IsBoss: true } when distToPlayer <= 2:
                MoveToward(world.Player.Position, world);
                if (distToPlayer <= 1)
                {
                    world.Player.TakeDamage(AttackPower);
                    world.AddMessages($"{Name} strikes you for {AttackPower} damage!");
                }
                break;

            case { Health: var hp } when hp < MaxHealth / 4 && distToPlayer <= DetectionRange:
                MoveAwayFrom(world.Player.Position, world);
                break;

            case Enemy when distToPlayer <= 1:
                world.Player.TakeDamage(AttackPower);
                world.AddMessages($"{Name} hits you for {AttackPower} damage!");
                break;

            case Enemy when distToPlayer <= DetectionRange:
                MoveToward(world.Player.Position, world);
                break;

            default:
                MoveRandomly(world);
                break;
        }
    }

    /// <summary>Reduces HP and marks inactive when killed.</summary>
    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            Health = 0;
            IsActive = false;
        }
    }

    // ---------------------------------------------------------------
    // AI movement helpers
    // ---------------------------------------------------------------

    private void MoveToward(Position target, GameWorld world)
    {
        int dx = target.X - Position.X;
        int dy = target.Y - Position.Y;

        Position newPos;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            newPos = new Position(Position.X + Math.Sign(dx), Position.Y);
            if (!world.IsWalkable(newPos) || world.HasEnemyAt(newPos))
                newPos = new Position(Position.X, Position.Y + Math.Sign(dy));
        }
        else
        {
            newPos = new Position(Position.X, Position.Y + Math.Sign(dy));
            if (!world.IsWalkable(newPos) || world.HasEnemyAt(newPos))
                newPos = new Position(Position.X + Math.Sign(dx), Position.Y);
        }

        if (world.IsWalkable(newPos) && !world.HasEnemyAt(newPos))
            Position = newPos;
    }

    private void MoveAwayFrom(Position target, GameWorld world)
    {
        int dx = Position.X - target.X;
        int dy = Position.Y - target.Y;

        Position newPos = Math.Abs(dx) >= Math.Abs(dy)
            ? new Position(Position.X + Math.Sign(dx), Position.Y)
            : new Position(Position.X, Position.Y + Math.Sign(dy));

        if (world.IsWalkable(newPos) && !world.HasEnemyAt(newPos))
            Position = newPos;
    }

    private void MoveRandomly(GameWorld world)
    {
        Direction[] directions = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];
        Direction dir = directions[_random.Next(directions.Length)];
        Position newPos = Position + Position.FromDirection(dir);

        if (world.IsWalkable(newPos) && !world.HasEnemyAt(newPos))
            Position = newPos;
    }
}
