// BossEnemy.cs — A sealed boss enemy variant with enhanced stats.
// Demonstrates: [#8] Sealed class — BossEnemy cannot be further inherited.
// Bosses are tougher, deal more damage, and have special visual representation.

namespace MazeGame.Core.Models;

/// <summary>
/// A sealed boss enemy class. The "sealed" keyword prevents any further
/// subclassing — BossEnemy is the final form of the Enemy hierarchy.
/// Bosses have higher stats and a larger detection range than normal enemies.
/// </summary>
public sealed class BossEnemy : Enemy
{
    /// <summary>
    /// Bosses always report true for IsBoss, triggering special AI behavior
    /// in the base Enemy.Update() switch statement.
    /// </summary>
    public override bool IsBoss => true;

    /// <summary>
    /// Creates a new BossEnemy with boosted stats.
    /// Bosses have more health, higher attack, larger detection range,
    /// and a higher difficulty rating than regular enemies.
    /// </summary>
    /// <param name="position">Starting grid position.</param>
    /// <param name="name">Boss display name (e.g., "Dragon", "Guardian").</param>
    /// <param name="health">Starting health (typically much higher than normal enemies).</param>
    /// <param name="attackPower">Damage per hit.</param>
    public BossEnemy(Position position, string name = "Guardian", int health = 150,
                     int attackPower = 20)
        : base(position, name, health, attackPower, detectionRange: 8, difficulty: 10)
    {
        // Boss uses a different symbol to stand out visually on the map
        Symbol = 'B';
    }
}
