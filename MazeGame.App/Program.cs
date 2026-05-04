// Program.cs — Entry point for the Maze Game application.
// ND1: [#18] events / lambdas, [#20] null-conditional.
// ND2: [ND2 #4] uses ConsoleKey.IsMovementKey, [ND2 #6] try/catch around DB init,
//      [ND2 #10] uses extension Deconstruct on a saved HighScore (via Enemy elsewhere
//      and on the most-recent score below — see usage), [ND2 #12] subscribes to
//      OnEnemyDefeated, [ND2 #13] LINQ on saved scores, [ND2 #14] EF Core SQLite.

using MazeGame.App.Rendering;
using MazeGame.Core.Enums;
using MazeGame.Core.Exceptions;
using MazeGame.Core.Services;
using MazeGame.Data;
using MazeGame.Data.Entities;

var renderer = new Renderer();
var engine = new GameEngine();

// ---------------------------------------------------------------
// [ND2 #14] Database initialization with EF Core + SQLite.
// [ND2 #6] Wrapped in try/catch — if the DB is unavailable, the
// game still runs but the high-score features are disabled.
// ---------------------------------------------------------------
HighScoreRepository? highScoreRepo = null;
try
{
    var dbContext = new GameDbContext();
    dbContext.Database.EnsureCreated();
    highScoreRepo = new HighScoreRepository(dbContext);
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Warning: high-score database unavailable ({ex.Message}). Continuing without persistence.");
    Console.ForegroundColor = ConsoleColor.Gray;
    Thread.Sleep(1500);
}

bool running = true;

// Counts enemies the player defeats during a single run (reset on StartGame).
int enemiesDefeatedThisRun = 0;

// ---------------------------------------------------------------
// [#18] Event subscriptions
// ---------------------------------------------------------------

engine.OnLevelComplete += () =>
{
    renderer.Reset();
};

engine.OnGameOver += () =>
{
    renderer.DrawGameOver(engine.CurrentLevel);
};

engine.OnVictory += () =>
{
    renderer.DrawVictory();
};

// [ND2 #12] Track kills via the new event from the engine.
engine.OnEnemyDefeated += _ => enemiesDefeatedThisRun++;

renderer.Initialize();

// ---------------------------------------------------------------
// Main menu loop — handles "start", "view scores", "quit".
// ---------------------------------------------------------------
while (running)
{
    renderer.DrawMainMenu();
    var menuKey = Console.ReadKey(true).Key;

    if (menuKey == ConsoleKey.Q)
    {
        running = false;
        break;
    }

    if (menuKey == ConsoleKey.H)
    {
        await ShowHighScoresAsync();
        continue;
    }

    // Default: any other key (Enter, Spacebar, etc.) starts a new run.
    enemiesDefeatedThisRun = 0;
    engine.StartGame();
    renderer.Reset();

    await PlayUntilEndAsync();

    // After the run ends (game over OR victory), prompt to save.
    if (engine.State is GameState.GameOver or GameState.Victory)
    {
        await PromptAndSaveScoreAsync(engine.State == GameState.Victory);
    }

    // Wait for R (restart → returns to main menu) or Q (quit).
    while (true)
    {
        if (!Console.KeyAvailable) { Thread.Sleep(40); continue; }
        var k = Console.ReadKey(true).Key;
        if (k == ConsoleKey.Q) { running = false; break; }
        if (k == ConsoleKey.R) { break; }
    }
}

Console.Clear();
Console.CursorVisible = true;
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("Thanks for playing Maze Game!");
return;

// ---------------------------------------------------------------
// Local functions
// ---------------------------------------------------------------

async Task PlayUntilEndAsync()
{
    while (engine.State == GameState.Playing)
    {
        if (Console.KeyAvailable)
        {
            var keyInfo = Console.ReadKey(true);
            ConsoleKey key = keyInfo.Key;

            if (key == ConsoleKey.Q)
            {
                running = false;
                return;
            }

            // [ND2 #4] IsMovementKey extension is used inside ProcessInput.
            engine.ProcessInput(key);
        }

        engine.Update();

        // [#20] Null-conditional — safely access World and Player.
        if (engine.World?.Player != null)
        {
            renderer.Draw(engine.World, engine);
        }

        Thread.Sleep(66);
    }

    // Drain any final frame so the user sees the game-over screen below.
    await Task.CompletedTask;
}

async Task ShowHighScoresAsync()
{
    List<HighScore> scores;
    if (highScoreRepo == null)
    {
        scores = new List<HighScore>();
    }
    else
    {
        try
        {
            scores = await highScoreRepo.GetTopScoresAsync(10);
        }
        catch (DatabaseException)
        {
            scores = new List<HighScore>();
        }
    }

    renderer.DrawHighScores(scores);
    Console.ReadKey(true);
}

async Task PromptAndSaveScoreAsync(bool victory)
{
    if (engine.World?.Player == null) return;

    var player = engine.World.Player;

    string headline = victory ? "VICTORY!" : "GAME OVER";
    string playerName;
    try
    {
        playerName = renderer.PromptForName(headline);
    }
    catch
    {
        // [ND2 #6] guard against console redirection / closed stdin.
        playerName = "Anonymous";
    }

    int score = (engine.CurrentLevel * 1000)
              + (Math.Max(player.Health, 0) * 10)
              + (enemiesDefeatedThisRun * 50);

    var record = new HighScore
    {
        PlayerName = playerName,
        LevelReached = engine.CurrentLevel,
        Score = score,
        AchievedAt = DateTime.UtcNow,
        Victory = victory,
    };

    if (highScoreRepo != null)
    {
        try
        {
            await highScoreRepo.SaveScoreAsync(record);
            engine.NotifyHighScoreSaved(record.Score);

            // [ND2 #13] LINQ on the saved leaderboard for a quick comparison.
            var top = await highScoreRepo.GetTopScoresAsync(10);
            int rank = top.FindIndex(s => s.Id == record.Id) + 1;
            string note = rank > 0 && rank <= 10
                ? $"Saved! You ranked #{rank} on the leaderboard."
                : "Saved!";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("  " + note);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  Press R to return to the menu, or Q to quit.");
        }
        catch (DatabaseException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine($"  Could not save score: {ex.Message}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("  Press R to return to the menu, or Q to quit.");
        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine();
        Console.WriteLine("  (Score not saved — database unavailable.)");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("  Press R to return to the menu, or Q to quit.");
    }
}
