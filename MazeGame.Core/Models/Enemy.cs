// Enemy.cs — Represents an enemy that roams the maze and attacks the player.
// Demonstrates: [#2] IComparable<T>, [#5] Switch with 'when' keyword.
// Enemies have simple AI: wander randomly, chase the player within detection range,
// and flee when low on health. Bosses have special aggressive behavior.

using MazeGame.Core.Abstract;
using MazeGame.Core.Enums;

namespace MazeGame.Core.Models;

/// <summary>
/// An enemy character in the game world. Enemies move around the maze
/// and damage the player on contact. Implements IComparable to allow
/// sorting enemies by difficulty level.
/// </summary>
public class Enemy : GameObject, IComparable<Enemy>
{
    // ---------------------------------------------------------------
    // Enemy stats
    // ---------------------------------------------------------------

    /// <summary>The enemy's display name (e.g., "Goblin", "Skeleton").</summary>
    public string Name { get; set; }

    /// <summary>Current health points. The enemy is defeated when this hits 0.</summary>
    public int Health { get; set; }

    /// <summary>Maximum health points for this enemy.</summary>
    public int MaxHealth { get; set; }

    /// <summary>Damage dealt to the player per attack.</summary>
    public int AttackPower { get; set; }

    /// <summary>How far away (Manhattan distance) the enemy can detect the player.</summary>
    public int DetectionRange { get; set; }

    /// <summary>
    /// A numeric difficulty rating used for sorting and comparison.
    /// Higher values mean a tougher enemy.
    /// </summary>
    public int Difficulty { get; set; }

    /// <summary>Whether this enemy is a boss (used in AI and pattern matching).</summary>
    public virtual bool IsBoss => false;

    // Random number generator for AI movement decisions
    private static readonly Random _random = new();

    // Tracks how many frames since the last move (enemies move slower than the player)
    private int _moveTimer;

    /// <summary>
    /// Creates a new enemy at the given position with specified stats.
    /// Uses [#15] default and named arguments for flexible construction.
    /// </summary>
    /// <param name="position">Starting grid position.</param>
    /// <param name="name">Display name for this enemy.</param>
    /// <param name="health">Starting and max health.</param>
    /// <param name="attackPower">Damage per hit.</param>
    /// <param name="detectionRange">Chase range in tiles.</param>
    /// <param name="difficulty">Difficulty rating for sorting.</param>
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

    // ---------------------------------------------------------------
    // [#2] IComparable<Enemy> — sort enemies by difficulty
    // ---------------------------------------------------------------

    /// <summary>
    /// Compares this enemy to another by difficulty rating.
    /// Used to sort enemy lists from weakest to strongest.
    /// </summary>
    public int CompareTo(Enemy? other)
    {
        // Null enemies sort to the end
        if (other is null) return 1;
        return Difficulty.CompareTo(other.Difficulty);
    }

    // ---------------------------------------------------------------
    // [#5] Switch with 'when' keyword — AI behavior
    // ---------------------------------------------------------------

    /// <summary>
    /// Updates the enemy's position each frame based on AI behavior.
    /// Uses a switch statement with 'when' guard clauses to decide
    /// between different behaviors based on the enemy's state and
    /// distance to the player.
    /// </summary>
    public override void Update(GameWorld world)
    {
        // Dead enemies don't move
        if (Health <= 0 || !IsActive) return;

        // Enemies move every few frames to be slower than the player
        _moveTimer++;
        if (_moveTimer < 3) return;
        _moveTimer = 0;

        // Calculate distance to the player for AI decisions
        int distToPlayer = Position.ManhattanDistance(world.Player.Position);

        // [#5] Switch with 'when' — different behaviors based on state + conditions
        switch (this)
        {
            // Boss enemies charge aggressively when player is very close
            case { IsBoss: true } when distToPlayer <= 2:
                MoveToward(world.Player.Position, world);
                // Boss attacks if adjacent to the player
                if (distToPlayer <= 1)
                {
                    world.Player.TakeDamage(AttackPower);
                    world.AddMessages($"{Name} strikes you for {AttackPower} damage!");
                }
                break;

            // Any enemy flees when health is critically low (below 25%)
            case { Health: var hp } when hp < MaxHealth / 4 && distToPlayer <= DetectionRange:
                MoveAwayFrom(world.Player.Position, world);
                break;

            // Normal chase behavior when player is within detection range
            case Enemy when distToPlayer <= 1:
                // Adjacent to player — attack!
                world.Player.TakeDamage(AttackPower);
                world.AddMessages($"{Name} hits you for {AttackPower} damage!");
                break;

            case Enemy when distToPlayer <= DetectionRange:
                // Within detection range — chase the player
                MoveToward(world.Player.Position, world);
                break;

            default:
                // Out of range — wander randomly
                MoveRandomly(world);
                break;
        }
    }

    /// <summary>
    /// Reduces the enemy's health by the given amount.
    /// Marks the enemy as inactive if health drops to zero.
    /// </summary>
    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            Health = 0;
            IsActive = false;  // Remove from the game world
        }
    }

    // ---------------------------------------------------------------
    // Movement helpers for AI
    // ---------------------------------------------------------------

    /// <summary>
    /// Moves one step toward the target position (simple greedy pathfinding).
    /// Prefers the axis with the greatest distance.
    /// </summary>
    private void MoveToward(Position target, GameWorld world)
    {
        // Calculate the offset to the target
        int dx = target.X - Position.X;
        int dy = target.Y - Position.Y;

        // Try to move along the axis with the greatest distance first
        Position newPos;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            // Try horizontal movement first
            newPos = new Position(Position.X + Math.Sign(dx), Position.Y);
            if (!world.IsWalkable(newPos) || world.HasEnemyAt(newPos))
            {
                // Blocked horizontally — try vertical instead
                newPos = new Position(Position.X, Position.Y + Math.Sign(dy));
            }
        }
        else
        {
            // Try vertical movement first
            newPos = new Position(Position.X, Position.Y + Math.Sign(dy));
            if (!world.IsWalkable(newPos) || world.HasEnemyAt(newPos))
            {
                // Blocked vertically — try horizontal instead
                newPos = new Position(Position.X + Math.Sign(dx), Position.Y);
            }
        }

        // Only move if the new position is valid and not occupied by another enemy
        if (world.IsWalkable(newPos) && !world.HasEnemyAt(newPos))
        {
            Position = newPos;
        }
    }

    /// <summary>
    /// Moves one step away from the target position (flee behavior).
    /// </summary>
    private void MoveAwayFrom(Position target, GameWorld world)
    {
        // Invert the direction — move opposite to where the target is
        int dx = Position.X - target.X;
        int dy = Position.Y - target.Y;

        Position newPos;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            newPos = new Position(Position.X + Math.Sign(dx), Position.Y);
        }
        else
        {
            newPos = new Position(Position.X, Position.Y + Math.Sign(dy));
        }

        if (world.IsWalkable(newPos) && !world.HasEnemyAt(newPos))
        {
            Position = newPos;
        }
    }

    /// <summary>
    /// Moves one step in a random direction (wander behavior).
    /// </summary>
    private void MoveRandomly(GameWorld world)
    {
        // Pick a random direction from the four cardinal directions
        Direction[] directions = [Direction.Up, Direction.Down, Direction.Left, Direction.Right];
        Direction dir = directions[_random.Next(directions.Length)];
        Position newPos = Position + Position.FromDirection(dir);

        // Only move if the chosen tile is walkable and unoccupied
        if (world.IsWalkable(newPos) && !world.HasEnemyAt(newPos))
        {
            Position = newPos;
        }
    }
}
