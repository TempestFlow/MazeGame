// Program.cs — Entry point for the Maze Game application.
// Demonstrates: [#18] Events/lambdas (subscribing to engine events),
//               [#20] Null-conditional operators.
// Contains the main game loop: Input → Update → Render.

using MazeGame.App.Rendering;
using MazeGame.Core.Enums;
using MazeGame.Core.Services;

// ---------------------------------------------------------------
// Initialize the game engine and renderer
// ---------------------------------------------------------------

// Create the renderer that handles all console output
var renderer = new Renderer();

// Create the game engine that manages all game logic
var engine = new GameEngine();

// Track whether the game should keep running
bool running = true;

// Track whether we need to redraw the screen after a state change
bool needsFullRedraw = false;

// ---------------------------------------------------------------
// [#18] Event subscriptions — using lambdas to wire up callbacks
// ---------------------------------------------------------------

// When a level is completed, reset the renderer for the new level layout
engine.OnLevelComplete += () =>
{
    renderer.Reset();
    needsFullRedraw = true;
};

// When the player dies, switch to the Game Over screen
engine.OnGameOver += () =>
{
    renderer.DrawGameOver(engine.CurrentLevel);
};

// When the player wins, switch to the Victory screen
engine.OnVictory += () =>
{
    renderer.DrawVictory();
};

// ---------------------------------------------------------------
// Show the main menu and wait for the player to start
// ---------------------------------------------------------------

renderer.Initialize();
renderer.DrawMainMenu();

// Wait for any key press to start the game
Console.ReadKey(true);

// Start the game from level 1
engine.StartGame();
renderer.Reset();

// ---------------------------------------------------------------
// Main game loop: Input → Update → Render
// ---------------------------------------------------------------

while (running)
{
    // --- INPUT PHASE ---
    // Check if the player pressed a key (non-blocking)
    if (Console.KeyAvailable)
    {
        var keyInfo = Console.ReadKey(true);
        ConsoleKey key = keyInfo.Key;

        // Handle global keys that work in any state
        if (key == ConsoleKey.Q)
        {
            // Quit the game
            running = false;
            continue;
        }

        // Handle state-specific input
        switch (engine.State)
        {
            case GameState.Playing:
                // Forward gameplay keys to the engine
                engine.ProcessInput(key);
                break;

            case GameState.GameOver:
            case GameState.Victory:
                // R restarts the game from level 1
                if (key == ConsoleKey.R)
                {
                    engine.StartGame();
                    renderer.Reset();
                    needsFullRedraw = true;
                }
                break;
        }
    }

    // --- UPDATE PHASE ---
    if (engine.State == GameState.Playing)
    {
        // Update all game entities (enemy AI, cleanup, etc.)
        engine.Update();
    }

    // --- RENDER PHASE ---
    if (engine.State == GameState.Playing)
    {
        // [#20] Null-conditional — safely access World and Player
        if (engine.World?.Player != null)
        {
            renderer.Draw(engine.World, engine);
        }
    }
    else if (needsFullRedraw)
    {
        // After a state change, let the next frame handle drawing
        needsFullRedraw = false;
    }

    // Cap the frame rate at roughly 15 FPS to avoid burning CPU
    // and to keep the game at a playable speed
    Thread.Sleep(66);
}

// ---------------------------------------------------------------
// Cleanup — restore the console to a normal state
// ---------------------------------------------------------------

Console.Clear();
Console.CursorVisible = true;
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("Thanks for playing Maze Game!");
