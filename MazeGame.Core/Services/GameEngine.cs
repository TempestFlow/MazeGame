// GameEngine.cs — The central game engine that orchestrates the game loop.
// Demonstrates: [#18] Delegates, lambda functions, and events.
// Manages game state, processes input, updates entities, and fires events
// that the UI layer (MazeGame.App) subscribes to.

using MazeGame.Core.Enums;
using MazeGame.Core.Interfaces;
using MazeGame.Core.Models;

namespace MazeGame.Core.Services;

/// <summary>
/// The core game engine. Manages the game loop phases (input, update, render),
/// level progression, and communicates with the UI through events and delegates.
/// The App layer creates a GameEngine and subscribes to its events.
/// </summary>
public class GameEngine
{
    // ---------------------------------------------------------------
    // [#18] Delegates and Events — decouple Core from App
    // ---------------------------------------------------------------

    /// <summary>
    /// Event fired when the game wants to display a message in the HUD.
    /// The App's Renderer subscribes to this to show messages to the player.
    /// </summary>
    public event Action<string>? OnMessage;

    /// <summary>
    /// Event fired when the player completes a level (walks through an unlocked door).
    /// </summary>
    public event Action? OnLevelComplete;

    /// <summary>
    /// Event fired when the player dies (health reaches 0).
    /// </summary>
    public event Action? OnGameOver;

    /// <summary>
    /// Event fired when the player wins the game (completes all levels).
    /// </summary>
    public event Action? OnVictory;

    /// <summary>
    /// [#18] Func delegate — a strategy for sorting enemies, injectable by the caller.
    /// Defaults to sorting by difficulty (using IComparable).
    /// </summary>
    public Func<List<Enemy>, List<Enemy>>? EnemySortStrategy { get; set; }

    // ---------------------------------------------------------------
    // Game state
    // ---------------------------------------------------------------

    /// <summary>The current game world (map, entities, items).</summary>
    public GameWorld? World { get; private set; }

    /// <summary>The current game state (Playing, GameOver, etc.).</summary>
    public GameState State { get; private set; }

    /// <summary>The current level number (1-based).</summary>
    public int CurrentLevel { get; private set; }

    /// <summary>Whether the player is currently attacking (for sword visual).</summary>
    public bool IsAttacking { get; private set; }

    /// <summary>The position where the sword visual should appear.</summary>
    public Position AttackPosition { get; private set; }

    /// <summary>Remaining frames to show the attack visual.</summary>
    private int _attackVisualTimer;

    // Services
    private readonly LevelLoader _levelLoader;
    private readonly CombatService _combatService;

    /// <summary>
    /// Creates a new GameEngine with fresh services.
    /// </summary>
    public GameEngine()
    {
        _levelLoader = new LevelLoader();
        _combatService = new CombatService();
        State = GameState.MainMenu;
        CurrentLevel = 0;

        // [#18] Lambda — default sort strategy uses IComparable<Enemy> (#2)
        EnemySortStrategy = enemies =>
        {
            var sorted = new List<Enemy>(enemies);
            sorted.Sort();  // Uses Enemy.CompareTo (IComparable<Enemy>)
            return sorted;
        };
    }

    /// <summary>
    /// Starts the game from level 1. Initializes the first level
    /// and subscribes to player events.
    /// </summary>
    public void StartGame()
    {
        CurrentLevel = 1;
        State = GameState.Playing;
        LoadCurrentLevel();
    }

    /// <summary>
    /// Loads the current level using the LevelLoader and wires up player events.
    /// Uses [#15] named arguments when calling LoadLevel for readability.
    /// </summary>
    private void LoadCurrentLevel()
    {
        // [#15] Named arguments — makes the call self-documenting
        World = CurrentLevel switch
        {
            1 => _levelLoader.LoadLevel(1, enemyHealth: 40, enemyAttack: 8, bossName: "Guardian"),
            2 => _levelLoader.LoadLevel(2, enemyHealth: 60, enemyAttack: 12, bossName: "Warden"),
            3 => _levelLoader.LoadLevel(3, enemyHealth: 75, enemyAttack: 15,
                                        bossName: "Dragon", bossHealth: 200),
            _ => _levelLoader.LoadLevel(1)
        };

        // [#18] Subscribe to player events using lambdas
        World.Player.OnDeath += () =>
        {
            State = GameState.GameOver;
            OnGameOver?.Invoke();
        };

        World.Player.OnAction += message =>
        {
            // Forward player action messages to the HUD
            World.AddMessages(message);
            OnMessage?.Invoke(message);
        };

        // [#18] Demonstrate the enemy sort strategy delegate (#2 IComparable)
        if (EnemySortStrategy != null)
        {
            // Sort enemies by difficulty — uses IComparable<Enemy>.CompareTo
            var sortedEnemies = EnemySortStrategy(World.Enemies);

            // Log the sorted enemy roster to demonstrate the sorting
            OnMessage?.Invoke($"Enemies sorted by difficulty:");
            foreach (var enemy in sortedEnemies)
            {
                OnMessage?.Invoke($"  {enemy.Name} (Difficulty: {enemy.Difficulty})");
            }
        }
    }

    /// <summary>
    /// Processes a single key press from the player.
    /// Maps console keys to game actions (movement, attack, quit).
    /// </summary>
    /// <param name="key">The console key that was pressed.</param>
    public void ProcessInput(ConsoleKey key)
    {
        // Only process input while the game is active
        if (State != GameState.Playing || World == null) return;

        // Map the key to a game action
        switch (key)
        {
            // Movement keys — WASD and Arrow keys
            case ConsoleKey.W or ConsoleKey.UpArrow:
                World.Player.Move(Direction.Up, World);
                CheckInteractions();
                break;

            case ConsoleKey.S or ConsoleKey.DownArrow:
                World.Player.Move(Direction.Down, World);
                CheckInteractions();
                break;

            case ConsoleKey.A or ConsoleKey.LeftArrow:
                World.Player.Move(Direction.Left, World);
                CheckInteractions();
                break;

            case ConsoleKey.D or ConsoleKey.RightArrow:
                World.Player.Move(Direction.Right, World);
                CheckInteractions();
                break;

            // Attack key — Spacebar
            case ConsoleKey.Spacebar:
                HandleAttack();
                break;
        }
    }

    /// <summary>
    /// Handles the player's attack action.
    /// Uses CombatService to process the attack and shows a sword visual.
    /// </summary>
    private void HandleAttack()
    {
        if (World == null) return;

        // Process the attack through CombatService
        string? result = _combatService.ProcessAttack(World);

        // Show the sword visual for a few frames
        AttackPosition = World.Player.Position + Position.FromDirection(World.Player.Facing);
        IsAttacking = true;
        _attackVisualTimer = 3;  // Show sword for 3 frames

        // Log the result if the attack hit something
        if (result != null)
        {
            World.AddMessages(result);
        }
        else
        {
            World.AddMessages("*whoosh* — your attack hits nothing but air.");
        }
    }

    /// <summary>
    /// Checks if the player is standing on an interactable object (key, potion, door).
    /// Uses [#14] 'is' operator and [#20] null-conditional operators.
    /// </summary>
    private void CheckInteractions()
    {
        if (World == null) return;

        // [#20] Null-conditional and [#17] out parameter
        if (World.TryGetInteractableAt(World.Player.Position, out var interactable))
        {
            // [#20] Null-conditional — safely invoke the interaction
            interactable?.OnInteract(World.Player);

            // [#20] Null-coalescing — provide a default message
            string message = interactable?.InteractionMessage ?? "You found something.";
            World.AddMessages(message);

            // Check if a door was opened (level complete)
            // [#14] 'is' operator for type checking
            if (interactable is Door { IsUnlocked: true })
            {
                AdvanceLevel();
            }
        }
    }

    /// <summary>
    /// Advances to the next level, or triggers victory if all levels are complete.
    /// </summary>
    private void AdvanceLevel()
    {
        CurrentLevel++;
        OnLevelComplete?.Invoke();

        if (CurrentLevel > LevelLoader.TotalLevels)
        {
            // All levels completed — the player wins!
            State = GameState.Victory;
            OnVictory?.Invoke();
        }
        else
        {
            // Load the next level, preserving the player's health
            int previousHealth = World!.Player.Health;
            int previousMaxHealth = World.Player.MaxHealth;
            int previousAttack = World.Player.AttackPower;

            LoadCurrentLevel();

            // Carry over the player's stats from the previous level
            World!.Player.Health = previousHealth;
            World.Player.MaxHealth = previousMaxHealth;
            World.Player.AttackPower = previousAttack;
        }
    }

    /// <summary>
    /// Updates all game entities for the current frame.
    /// Called once per game loop iteration after input processing.
    /// </summary>
    public void Update()
    {
        // Only update while the game is active
        if (State != GameState.Playing || World == null) return;

        // Tick down the attack visual timer
        if (_attackVisualTimer > 0)
        {
            _attackVisualTimer--;
            if (_attackVisualTimer == 0)
            {
                IsAttacking = false;
            }
        }

        // Update all active enemies (AI movement and attacks)
        foreach (var enemy in World.Enemies)
        {
            if (enemy.IsActive)
            {
                enemy.Update(World);
            }
        }

        // Update tile flags to reflect current entity positions
        World.UpdateFlags();

        // Clean up defeated enemies
        World.Enemies.RemoveAll(e => !e.IsActive);

        // Clean up collected items
        World.Items.RemoveAll(i => !i.IsActive);
    }
}
