// GameEngine.cs — Orchestrates the game loop.
// ND1: [#18] delegates / lambdas / events.
// ND2: [ND2 #4] uses ConsoleKey.IsMovementKey extension.
//      [ND2 #8] uses SortedGameCollection<Enemy>.
//      [ND2 #9] uses WhereActive<T> generic extension.
//      [ND2 #11] uses Player.Clone() for level checkpoints.
//      [ND2 #12] adds OnEnemyDefeated and OnHighScoreSaved events.
//      [ND2 #13] LINQ used in stats helpers.

using MazeGame.Core.Enums;
using MazeGame.Core.Interfaces;
using MazeGame.Core.Models;
using MazeGame.Core.Utils;

namespace MazeGame.Core.Services;

/// <summary>
/// Core game engine. Manages level progression, processes input, ticks
/// entities, and exposes events the UI layer subscribes to.
/// </summary>
public class GameEngine
{
    // ---------------------------------------------------------------
    // [#18] Events / delegates — decouple Core from App
    // ---------------------------------------------------------------

    public event Action<string>? OnMessage;
    public event Action? OnLevelComplete;
    public event Action? OnGameOver;
    public event Action? OnVictory;

    /// <summary>
    /// [ND2 #12] Fires every time an enemy's IsActive flips false during
    /// an update tick. Argument is the defeated enemy's Difficulty so the
    /// App can tally points / play sounds proportional to the kill.
    /// </summary>
    public event Action<int>? OnEnemyDefeated;

    /// <summary>
    /// [ND2 #12] Raised by the App after a high score is successfully saved.
    /// The engine declares it so other Core systems could subscribe in future.
    /// </summary>
    public event Action<int>? OnHighScoreSaved;

    /// <summary>[#18] Strategy delegate for sorting enemies (defaults to IComparable).</summary>
    public Func<List<Enemy>, List<Enemy>>? EnemySortStrategy { get; set; }

    // ---------------------------------------------------------------
    // State
    // ---------------------------------------------------------------

    public GameWorld? World { get; private set; }
    public GameState State { get; private set; }
    public int CurrentLevel { get; private set; }
    public bool IsAttacking { get; private set; }
    public Position AttackPosition { get; private set; }

    /// <summary>[ND2 #11] Snapshot taken at the start of each level via Player.Clone().</summary>
    public Player? LevelStartSnapshot { get; private set; }

    /// <summary>[ND2 #8] Enemies sorted by IComparable&lt;Enemy&gt; (Difficulty).</summary>
    public SortedGameCollection<Enemy> SortedEnemies { get; } = new();

    private int _attackVisualTimer;
    private readonly LevelLoader _levelLoader;
    private readonly CombatService _combatService;

    // Tracks enemies for which we've already fired OnEnemyDefeated so the
    // event fires exactly once per kill regardless of which subsystem caused it.
    private readonly HashSet<Enemy> _defeatedAnnounced = new();

    public GameEngine()
    {
        _levelLoader = new LevelLoader();
        _combatService = new CombatService();
        State = GameState.MainMenu;
        CurrentLevel = 0;

        // [#18] Lambda — default sort uses Enemy.CompareTo (ND1 #2).
        EnemySortStrategy = enemies =>
        {
            var sorted = new List<Enemy>(enemies);
            sorted.Sort();
            return sorted;
        };
    }

    public void StartGame()
    {
        CurrentLevel = 1;
        State = GameState.Playing;
        LoadCurrentLevel();
    }

    /// <summary>
    /// Loads <see cref="CurrentLevel"/> and wires up player events.
    /// Falls back to level 1 if the loader throws <see cref="Exceptions.InvalidLevelException"/>
    /// — keeps ND1 default behavior intact for the App while still exercising
    /// [ND2 #5] / [ND2 #6] lower in the stack.
    /// </summary>
    private void LoadCurrentLevel()
    {
        try
        {
            // [#15] Named arguments
            World = CurrentLevel switch
            {
                1 => _levelLoader.LoadLevel(1, enemyHealth: 40, enemyAttack: 8, bossName: "Guardian"),
                2 => _levelLoader.LoadLevel(2, enemyHealth: 60, enemyAttack: 12, bossName: "Warden"),
                3 => _levelLoader.LoadLevel(3, enemyHealth: 75, enemyAttack: 15,
                                            bossName: "Dragon", bossHealth: 200),
                _ => _levelLoader.LoadLevel(1)
            };
        }
        catch (Exceptions.InvalidLevelException ex)
        {
            // [ND2 #6] graceful fallback — bad level number → use level 1 and keep playing.
            OnMessage?.Invoke($"Level {ex.LevelNumber} unavailable, falling back to level 1.");
            CurrentLevel = 1;
            World = _levelLoader.LoadLevel(1);
        }

        // [#18] Subscribe to player events using lambdas.
        World.Player.OnDeath += () =>
        {
            State = GameState.GameOver;
            OnGameOver?.Invoke();
        };

        World.Player.OnAction += message =>
        {
            World.AddMessages(message);
            OnMessage?.Invoke(message);
        };

        // [ND2 #11] Take a checkpoint snapshot of the player's stats at level start.
        LevelStartSnapshot = (Player)World.Player.Clone();

        // [ND2 #8] Rebuild the sorted-by-difficulty enemy collection.
        SortedEnemies.Clear();
        foreach (var e in World.Enemies)
        {
            SortedEnemies.Add(e);
        }

        // Fresh level → fresh defeated-tracking set.
        _defeatedAnnounced.Clear();

        // [#18] Demonstrate the strategy delegate by listing the roster.
        if (EnemySortStrategy != null)
        {
            var sorted = EnemySortStrategy(World.Enemies);
            OnMessage?.Invoke("Enemies sorted by difficulty:");
            foreach (var enemy in sorted)
            {
                // [ND2 #10] Extension Deconstruct on Enemy.
                var (name, hp, atk) = enemy;
                OnMessage?.Invoke($"  {name} (HP:{hp}, ATK:{atk}, Difficulty:{enemy.Difficulty})");
            }
        }
    }

    /// <summary>
    /// Maps a console key to a game action. Uses the
    /// [ND2 #4] <c>ConsoleKey.IsMovementKey</c> extension to skip the
    /// non-movement branch quickly.
    /// </summary>
    public void ProcessInput(ConsoleKey key)
    {
        if (State != GameState.Playing || World == null) return;

        // [ND2 #4] Extension method on ConsoleKey.
        if (key.IsMovementKey())
        {
            switch (key)
            {
                case ConsoleKey.W or ConsoleKey.UpArrow:
                    World.Player.Move(Direction.Up, World); break;
                case ConsoleKey.S or ConsoleKey.DownArrow:
                    World.Player.Move(Direction.Down, World); break;
                case ConsoleKey.A or ConsoleKey.LeftArrow:
                    World.Player.Move(Direction.Left, World); break;
                case ConsoleKey.D or ConsoleKey.RightArrow:
                    World.Player.Move(Direction.Right, World); break;
            }
            CheckInteractions();
            return;
        }

        if (key == ConsoleKey.Spacebar)
        {
            HandleAttack();
        }
    }

    private void HandleAttack()
    {
        if (World == null) return;

        string? result = _combatService.ProcessAttack(World);

        AttackPosition = World.Player.Position + Position.FromDirection(World.Player.Facing);
        IsAttacking = true;
        _attackVisualTimer = 3;

        if (result != null)
            World.AddMessages(result);
        else
            World.AddMessages("*whoosh* — your attack hits nothing but air.");
    }

    /// <summary>Checks if the player is standing on an interactable. [#14] 'is', [#20] null-cond.</summary>
    private void CheckInteractions()
    {
        if (World == null) return;

        if (World.TryGetInteractableAt(World.Player.Position, out var interactable))
        {
            interactable?.OnInteract(World.Player);
            string message = interactable?.InteractionMessage ?? "You found something.";
            World.AddMessages(message);

            if (interactable is Door { IsUnlocked: true })
            {
                AdvanceLevel();
            }
        }
    }

    private void AdvanceLevel()
    {
        CurrentLevel++;
        OnLevelComplete?.Invoke();

        if (CurrentLevel > LevelLoader.TotalLevels)
        {
            State = GameState.Victory;
            OnVictory?.Invoke();
        }
        else
        {
            int previousHealth = World!.Player.Health;
            int previousMaxHealth = World.Player.MaxHealth;
            int previousAttack = World.Player.AttackPower;

            LoadCurrentLevel();

            World!.Player.Health = previousHealth;
            World.Player.MaxHealth = previousMaxHealth;
            World.Player.AttackPower = previousAttack;
        }
    }

    /// <summary>
    /// Per-frame tick: enemy AI, flag refresh, cleanup. Detects newly
    /// defeated enemies and fires [ND2 #12] <see cref="OnEnemyDefeated"/>.
    /// </summary>
    public void Update()
    {
        if (State != GameState.Playing || World == null) return;

        if (_attackVisualTimer > 0)
        {
            _attackVisualTimer--;
            if (_attackVisualTimer == 0) IsAttacking = false;
        }

        // [ND2 #9] WhereActive<T> — generic extension with GameObject constraint.
        foreach (var enemy in World.Enemies.WhereActive())
        {
            enemy.Update(World);
        }

        // [ND2 #12] Fire OnEnemyDefeated once per kill, from any source
        // (player attack from the previous tick or AI side-effect).
        foreach (var enemy in World.Enemies)
        {
            if (!enemy.IsActive && _defeatedAnnounced.Add(enemy))
            {
                OnEnemyDefeated?.Invoke(enemy.Difficulty);
                SortedEnemies.Remove(enemy);
            }
        }

        World.UpdateFlags();

        World.Enemies.RemoveAll(e => !e.IsActive);
        World.Items.RemoveAll(i => !i.IsActive);
    }

    // ---------------------------------------------------------------
    // [ND2 #13] LINQ helpers exposed to the UI / debugging layer
    // ---------------------------------------------------------------

    /// <summary>[ND2 #13] Average HP across active enemies, or 0 when none.</summary>
    public double AverageEnemyHealth()
    {
        if (World == null) return 0;
        var active = World.Enemies.WhereActive().ToList();
        return active.Count == 0 ? 0 : active.Average(e => e.Health);
    }

    /// <summary>
    /// [ND2 #13] Groups active enemies by Name and returns a small
    /// (name, count) projection — used by the App for HUD stats / debug.
    /// </summary>
    public IEnumerable<(string Name, int Count)> EnemyCountsByName()
    {
        if (World == null) yield break;

        var groups = World.Enemies
            .WhereActive()
            .GroupBy(e => e.Name)
            .Select(g => (Name: g.Key, Count: g.Count()))
            .OrderByDescending(t => t.Count);

        foreach (var t in groups) yield return t;
    }

    /// <summary>
    /// [ND2 #12] App-side hook: the App calls this after persisting a high
    /// score so engine subscribers see the event. The argument is the saved
    /// score's numeric value.
    /// </summary>
    public void NotifyHighScoreSaved(int savedScoreValue)
    {
        OnHighScoreSaved?.Invoke(savedScoreValue);
    }
}
