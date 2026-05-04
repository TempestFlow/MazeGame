// Renderer.cs — Console renderer: map, HUD, message log, menus, leaderboard.
// ND1: [#20] null-coalescing assignment (??=) for lazy buffer init.
// ND2: leaderboard rendering uses [ND2 #4] string.Truncate extension.

using MazeGame.Core.Enums;
using MazeGame.Core.Models;
using MazeGame.Core.Services;
using MazeGame.Core.Utils;
using MazeGame.Data.Entities;

namespace MazeGame.App.Rendering;

/// <summary>
/// Responsible for rendering the game to the console.
/// Maintains a buffer of the previous frame to enable diff-based rendering —
/// only tiles that changed are redrawn, eliminating screen flicker.
/// Also renders the HUD (health, level, key status) and message log below the map.
/// </summary>
public class Renderer
{
    /// <summary>
    /// Buffer storing the previous frame's characters.
    /// [#20] Uses ??= for lazy initialization on first draw.
    /// </summary>
    private char[,]? _previousFrame;

    /// <summary>
    /// Buffer storing the previous frame's colors for each cell.
    /// </summary>
    private ConsoleColor[,]? _previousColors;

    /// <summary>The width of the last rendered frame (for detecting size changes).</summary>
    private int _lastWidth;

    /// <summary>The height of the last rendered frame.</summary>
    private int _lastHeight;

    /// <summary>Previously rendered HUD lines — used to detect if HUD needs redraw.</summary>
    private string[] _previousHudLines = Array.Empty<string>();

    /// <summary>
    /// Hides the cursor, sets the title, ensures the console buffer is at
    /// least 80×35 (large enough for the biggest level + HUD + messages
    /// + menu content), then clears the screen.
    /// </summary>
    public void Initialize()
    {
        Console.CursorVisible = false;
        Console.Title = "Maze Game — Zelda-style Dungeon Crawler";
        EnsureBufferSize(80, 35);
        Console.Clear();
    }

    /// <summary>
    /// Ensures the console buffer is at least the given size.
    /// On Windows, the buffer can be resized; on other platforms this may
    /// not be supported, so errors are caught and ignored.
    /// </summary>
    private static void EnsureBufferSize(int minWidth, int minHeight)
    {
        // Buffer/window resizing only works on Windows; other terminals just scroll.
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            if (Console.BufferWidth < minWidth) Console.BufferWidth = minWidth;
            if (Console.BufferHeight < minHeight) Console.BufferHeight = minHeight;

            int windowWidth = Math.Min(minWidth, Console.LargestWindowWidth);
            int windowHeight = Math.Min(minHeight, Console.LargestWindowHeight);
            if (Console.WindowWidth < windowWidth) Console.WindowWidth = windowWidth;
            if (Console.WindowHeight < windowHeight) Console.WindowHeight = windowHeight;
        }
        catch
        {
            // Resize unsupported — game still runs, console may scroll.
        }
    }

    /// <summary>
    /// Safely sets the cursor position, ignoring errors if the position
    /// is outside the console buffer (e.g., terminal was resized).
    /// </summary>
    private static void SafeSetCursorPosition(int left, int top)
    {
        try { Console.SetCursorPosition(left, top); }
        catch (ArgumentOutOfRangeException) { /* off-screen — drop the write */ }
    }

    /// <summary>
    /// Draws the complete game frame: map, entities, HUD, and messages.
    /// Uses diff-based rendering to minimize console writes and prevent flicker.
    /// </summary>
    /// <param name="world">The current game world to render.</param>
    /// <param name="engine">The game engine (for attack visuals and level info).</param>
    public void Draw(GameWorld? world, GameEngine engine)
    {
        // [#20] null-conditional
        if (world?.Player == null) return;

        int width = world.Width;
        int height = world.Height;

        EnsureBufferSize(width + 1, height + 12);

        // [#20] ??= — lazy buffer init on first call.
        _previousFrame ??= new char[width, height];
        _previousColors ??= new ConsoleColor[width, height];

        // Map size changed (new level) → reset buffers + clear screen.
        if (width != _lastWidth || height != _lastHeight)
        {
            _previousFrame = new char[width, height];
            _previousColors = new ConsoleColor[width, height];
            _lastWidth = width;
            _lastHeight = height;
            Console.Clear();
        }

        DrawMap(world, engine, width, height);
        DrawHud(world.Player, engine.CurrentLevel, height, world);
        DrawMessages(world, height);
    }

    /// <summary>
    /// Renders the map tiles and all game entities.
    /// Only redraws cells that differ from the previous frame.
    /// </summary>
    private void DrawMap(GameWorld world, GameEngine engine, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char displayChar = world.TileGrid[x, y];
                ConsoleColor color = ConsoleColor.DarkGray;

                Position pos = new Position(x, y);

                // Render priority: player > sword > enemy > items > tile.
                if (world.Player.Position == pos)
                {
                    displayChar = world.Player.Render();
                    color = ConsoleColor.Green;
                }
                else if (engine.IsAttacking && engine.AttackPosition == pos)
                {
                    displayChar = CombatService.GetSwordSymbol(world.Player.Facing);
                    color = ConsoleColor.White;
                }
                else if (world.TryGetEnemyAt(pos, out var enemy))
                {
                    displayChar = enemy!.Render();
                    color = enemy?.IsBoss == true ? ConsoleColor.Magenta : ConsoleColor.Red;
                }
                else
                {
                    foreach (var item in world.Items)
                    {
                        if (item.IsActive && item.Position == pos)
                        {
                            displayChar = item.Render();
                            color = GetItemColor(displayChar);
                            break;
                        }
                    }

                    if (displayChar == '#') color = ConsoleColor.DarkYellow;
                    else if (displayChar == '.') color = ConsoleColor.DarkGray;
                }

                // Diff-based rendering — only write cells that changed.
                if (_previousFrame![x, y] != displayChar || _previousColors![x, y] != color)
                {
                    SafeSetCursorPosition(x, y);
                    Console.ForegroundColor = color;
                    Console.Write(displayChar);
                    _previousFrame![x, y] = displayChar;
                    _previousColors![x, y] = color;
                }
            }
        }
    }

    /// <summary>
    /// Returns the appropriate console color for an item character.
    /// </summary>
    private static ConsoleColor GetItemColor(char symbol) => symbol switch
    {
        'K' => ConsoleColor.Yellow,
        'D' => ConsoleColor.Cyan,
        '+' => ConsoleColor.Green,
        _   => ConsoleColor.White
    };

    /// <summary>
    /// Draws the HUD (heads-up display) below the map.
    /// Shows level, health bar, attack power, and key status.
    /// Uses [#4] IFormattable — calls player.ToString("H", null) for the health bar
    /// and player.ToString("F", null) for the full status.
    /// </summary>
    private void DrawHud(Player player, int level, int mapHeight, GameWorld world)
    {
        // [#4] IFormattable — "H" health-bar, "F" full status.
        string[] hudLines = new string[]
        {
            "",
            $"  Health: {player.ToString("H", null)}",
            $"  {player.ToString("F", null)}",
            $"  Enemies remaining: {world.Enemies.Count(e => e.IsActive)}",
            ""
        };

        int padWidth = Console.WindowWidth > 0 ? Math.Min(Console.WindowWidth - 1, 80) : 80;

        for (int i = 0; i < hudLines.Length; i++)
        {
            string paddedLine = hudLines[i].PadRight(padWidth);

            if (i >= _previousHudLines.Length || _previousHudLines[i] != paddedLine)
            {
                SafeSetCursorPosition(0, mapHeight + i);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(paddedLine);
            }
        }

        string[] padded = new string[hudLines.Length];
        for (int i = 0; i < hudLines.Length; i++)
            padded[i] = hudLines[i].PadRight(padWidth);
        _previousHudLines = padded;
    }

    /// <summary>
    /// Draws the message log below the HUD area.
    /// Shows the most recent game messages (pickups, attacks, etc.).
    /// </summary>
    private void DrawMessages(GameWorld world, int mapHeight)
    {
        int messageStartRow = mapHeight + 5;
        string[] messages = world.MessageLog.ToArray();
        int padWidth = Console.WindowWidth > 0 ? Math.Min(Console.WindowWidth - 1, 80) : 80;

        Console.ForegroundColor = ConsoleColor.DarkCyan;

        for (int i = 0; i < 5; i++)
        {
            SafeSetCursorPosition(0, messageStartRow + i);
            string line = i < messages.Length
                ? $"  {messages[i]}".PadRight(padWidth)
                : new string(' ', padWidth);
            Console.Write(line);
        }

        Console.ForegroundColor = ConsoleColor.Gray;
    }

    /// <summary>
    /// Draws the main menu / title screen before the game starts.
    /// </summary>
    public void DrawMainMenu()
    {
        // Ensure the console is large enough for the menu
        EnsureBufferSize(80, 30);
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;

        string[] title = new string[]
        {
            @"  __  __                   ____                      ",
            @" |  \/  | __ _ _______   / ___| __ _ _ __ ___   ___ ",
            @" | |\/| |/ _` |_  / _ \ | |  _ / _` | '_ ` _ \ / _ \",
            @" | |  | | (_| |/ /  __/ | |_| | (_| | | | | | |  __/",
            @" |_|  |_|\__,_/___\___|  \____|\__,_|_| |_| |_|\___|",
        };

        // Center the title vertically
        int startRow = 2;
        foreach (string line in title)
        {
            SafeSetCursorPosition(2, startRow++);
            Console.Write(line);
        }

        startRow += 2;
        Console.ForegroundColor = ConsoleColor.White;
        SafeSetCursorPosition(2, startRow++);
        Console.Write("A Zelda-style ASCII Dungeon Crawler");

        startRow += 2;
        Console.ForegroundColor = ConsoleColor.Gray;
        SafeSetCursorPosition(2, startRow++);
        Console.Write("Controls:");
        SafeSetCursorPosition(2, startRow++);
        Console.Write("  WASD / Arrow Keys  — Move");
        SafeSetCursorPosition(2, startRow++);
        Console.Write("  Space              — Attack");
        SafeSetCursorPosition(2, startRow++);
        Console.Write("  Q                  — Quit");

        startRow += 2;
        SafeSetCursorPosition(2, startRow++);
        Console.Write("Symbols:");
        SafeSetCursorPosition(2, startRow++);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  @");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" Player    ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("E");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" Enemy    ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write("B");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" Boss");

        SafeSetCursorPosition(2, startRow++);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  K");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" Key       ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("D");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" Door     ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("+");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(" Potion");

        startRow += 2;
        Console.ForegroundColor = ConsoleColor.Yellow;
        SafeSetCursorPosition(2, startRow++);
        Console.Write("[Enter] Start game     [H] View high scores     [Q] Quit");

        Console.ForegroundColor = ConsoleColor.Gray;
    }

    /// <summary>
    /// [ND2 #14] Renders the top-N leaderboard. Uses the
    /// [ND2 #4] <c>string.Truncate</c> extension method on each name.
    /// </summary>
    public void DrawHighScores(List<HighScore> scores)
    {
        EnsureBufferSize(80, 30);
        Console.Clear();

        Console.ForegroundColor = ConsoleColor.Yellow;
        SafeSetCursorPosition(2, 2);
        Console.Write("=== HIGH SCORES ===");

        Console.ForegroundColor = ConsoleColor.Gray;
        SafeSetCursorPosition(2, 4);
        Console.Write($"{"#",-4}{"NAME",-18}{"LVL",-6}{"SCORE",-10}{"DATE",-12}{"RESULT",-10}");

        if (scores.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            SafeSetCursorPosition(2, 6);
            Console.Write("(no scores yet — finish a run to be the first!)");
        }
        else
        {
            for (int i = 0; i < scores.Count; i++)
            {
                var hs = scores[i];
                string nameTrimmed = hs.PlayerName.Truncate(16);
                string dateStr = hs.AchievedAt.ToLocalTime().ToString("yyyy-MM-dd");
                string result = hs.Victory ? "Victory" : "Died";

                Console.ForegroundColor = i == 0 ? ConsoleColor.Yellow
                                        : i == 1 ? ConsoleColor.White
                                        : i == 2 ? ConsoleColor.DarkYellow
                                        : ConsoleColor.Gray;

                SafeSetCursorPosition(2, 5 + i);
                Console.Write($"{i + 1,-4}{nameTrimmed,-18}{hs.LevelReached,-6}{hs.Score,-10}{dateStr,-12}{result,-10}");
            }
        }

        Console.ForegroundColor = ConsoleColor.Gray;
        SafeSetCursorPosition(2, 5 + Math.Max(scores.Count, 1) + 2);
        Console.Write("Press any key to return to the menu...");
    }

    /// <summary>
    /// Prompts the player for a name on the game-over / victory screen.
    /// Falls back to "Anonymous" on null/empty/whitespace input. The caller
    /// handles the actual save via the repository.
    /// </summary>
    public string PromptForName(string headline)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        SafeSetCursorPosition(2, 5);
        Console.Write(headline);

        Console.ForegroundColor = ConsoleColor.Gray;
        SafeSetCursorPosition(2, 7);
        Console.Write("Enter your name (max 16 chars), then press Enter:");
        SafeSetCursorPosition(2, 8);
        Console.Write("> ");

        Console.CursorVisible = true;
        string? line;
        try
        {
            // [ND2 #6] try/catch — guards against null/disposed stdin.
            line = Console.ReadLine();
        }
        catch
        {
            line = null;
        }
        Console.CursorVisible = false;

        if (string.IsNullOrWhiteSpace(line)) return "Anonymous";
        return line.Trim().Truncate(16);
    }

    /// <summary>
    /// Draws the Game Over screen when the player dies.
    /// </summary>
    public void DrawGameOver(int level)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red;
        SafeSetCursorPosition(2, 5);
        Console.Write("=== GAME OVER ===");
        Console.ForegroundColor = ConsoleColor.Gray;
        SafeSetCursorPosition(2, 7);
        Console.Write($"You were defeated on Level {level}.");
        SafeSetCursorPosition(2, 9);
        Console.Write("Press R to restart or Q to quit.");
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    /// <summary>
    /// Draws the Victory screen when the player completes all levels.
    /// </summary>
    public void DrawVictory()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        SafeSetCursorPosition(2, 5);
        Console.Write("=== VICTORY! ===");
        Console.ForegroundColor = ConsoleColor.Green;
        SafeSetCursorPosition(2, 7);
        Console.Write("You have conquered all three dungeon levels!");
        SafeSetCursorPosition(2, 8);
        Console.Write("The treasure is yours. Congratulations!");
        Console.ForegroundColor = ConsoleColor.Gray;
        SafeSetCursorPosition(2, 10);
        Console.Write("Press R to play again or Q to quit.");
    }

    /// <summary>
    /// Resets the renderer's state for a new game or level change.
    /// Clears the frame buffers so the next draw is a full redraw.
    /// </summary>
    public void Reset()
    {
        _previousFrame = null;
        _previousColors = null;
        _previousHudLines = Array.Empty<string>();
        Console.Clear();
    }
}
