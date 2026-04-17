// Renderer.cs — Handles all console output: maze rendering, HUD, and messages.
// Demonstrates: [#20] Null-coalescing assignment (??=).
// Uses Console.SetCursorPosition for flicker-free partial redraws — only
// characters that changed since the last frame are written to the console.

using MazeGame.Core.Enums;
using MazeGame.Core.Models;
using MazeGame.Core.Services;

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
    /// Initializes the console for game rendering.
    /// Hides the cursor, sets the window title, and ensures the console
    /// buffer is large enough for the game content.
    /// </summary>
    public void Initialize()
    {
        Console.CursorVisible = false;
        Console.Title = "Maze Game — Zelda-style Dungeon Crawler";

        // Ensure the console buffer is tall enough for the largest level (15 rows)
        // plus HUD (5 lines) plus messages (5 lines) plus menu content (~25 rows)
        EnsureBufferSize(80, 35);

        // Clear the console to start fresh
        Console.Clear();
    }

    /// <summary>
    /// Ensures the console buffer is at least the given size.
    /// On Windows, the buffer can be resized; on other platforms this may
    /// not be supported, so errors are caught and ignored.
    /// </summary>
    private static void EnsureBufferSize(int minWidth, int minHeight)
    {
        // Buffer and window resizing is only supported on Windows
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            // Only resize if the current buffer is too small
            if (Console.BufferWidth < minWidth)
                Console.BufferWidth = minWidth;
            if (Console.BufferHeight < minHeight)
                Console.BufferHeight = minHeight;

            // Also try to resize the window so the user can see everything
            int windowWidth = Math.Min(minWidth, Console.LargestWindowWidth);
            int windowHeight = Math.Min(minHeight, Console.LargestWindowHeight);
            if (Console.WindowWidth < windowWidth)
                Console.WindowWidth = windowWidth;
            if (Console.WindowHeight < windowHeight)
                Console.WindowHeight = windowHeight;
        }
        catch
        {
            // Some terminals don't support buffer resizing — that's OK,
            // the game will still work, just may scroll
        }
    }

    /// <summary>
    /// Safely sets the cursor position, ignoring errors if the position
    /// is outside the console buffer (e.g., terminal was resized).
    /// </summary>
    private static void SafeSetCursorPosition(int left, int top)
    {
        try
        {
            Console.SetCursorPosition(left, top);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Position is outside the console buffer — skip this write
        }
    }

    /// <summary>
    /// Draws the complete game frame: map, entities, HUD, and messages.
    /// Uses diff-based rendering to minimize console writes and prevent flicker.
    /// </summary>
    /// <param name="world">The current game world to render.</param>
    /// <param name="engine">The game engine (for attack visuals and level info).</param>
    public void Draw(GameWorld? world, GameEngine engine)
    {
        // [#20] Null-conditional — safely handle null world
        if (world?.Player == null) return;

        int width = world.Width;
        int height = world.Height;

        // Ensure console buffer is tall enough for map + HUD + messages
        EnsureBufferSize(width + 1, height + 12);

        // [#20] ??= — lazily initialize the frame buffers on first call
        _previousFrame ??= new char[width, height];
        _previousColors ??= new ConsoleColor[width, height];

        // If the map size changed (new level), reset buffers and clear screen
        if (width != _lastWidth || height != _lastHeight)
        {
            _previousFrame = new char[width, height];
            _previousColors = new ConsoleColor[width, height];
            _lastWidth = width;
            _lastHeight = height;
            Console.Clear();
        }

        // Build the current frame from the tile grid and overlay entities
        DrawMap(world, engine, width, height);

        // Draw the HUD below the map
        DrawHud(world.Player, engine.CurrentLevel, height, world);

        // Draw the message log below the HUD
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
                // Start with the base tile character
                char displayChar = world.TileGrid[x, y];
                ConsoleColor color = ConsoleColor.DarkGray;

                Position pos = new Position(x, y);

                // Layer entities on top of the tile (priority order: player > sword > enemy > items)
                if (world.Player.Position == pos)
                {
                    // Player character
                    displayChar = world.Player.Render();
                    color = ConsoleColor.Green;
                }
                else if (engine.IsAttacking && engine.AttackPosition == pos)
                {
                    // Sword attack visual
                    displayChar = CombatService.GetSwordSymbol(world.Player.Facing);
                    color = ConsoleColor.White;
                }
                else if (world.TryGetEnemyAt(pos, out var enemy))
                {
                    // Enemy character
                    displayChar = enemy!.Render();
                    // [#20] Null-conditional — safely check boss status
                    color = enemy?.IsBoss == true ? ConsoleColor.Magenta : ConsoleColor.Red;
                }
                else
                {
                    // Check for items at this position
                    foreach (var item in world.Items)
                    {
                        if (item.IsActive && item.Position == pos)
                        {
                            displayChar = item.Render();
                            color = GetItemColor(displayChar);
                            break;
                        }
                    }

                    // Color walls differently from floors
                    if (displayChar == '#')
                    {
                        color = ConsoleColor.DarkYellow;
                    }
                    else if (displayChar == '.')
                    {
                        color = ConsoleColor.DarkGray;
                    }
                }

                // Only write to console if this cell changed (diff-based rendering)
                if (_previousFrame![x, y] != displayChar || _previousColors![x, y] != color)
                {
                    SafeSetCursorPosition(x, y);
                    Console.ForegroundColor = color;
                    Console.Write(displayChar);

                    // Update the buffers
                    _previousFrame![x, y] = displayChar;
                    _previousColors![x, y] = color;
                }
            }
        }
    }

    /// <summary>
    /// Returns the appropriate console color for an item character.
    /// </summary>
    private static ConsoleColor GetItemColor(char symbol)
    {
        return symbol switch
        {
            'K' => ConsoleColor.Yellow,     // Keys are yellow/gold
            'D' => ConsoleColor.Cyan,       // Doors are cyan
            '+' => ConsoleColor.Green,      // Potions are green
            _   => ConsoleColor.White       // Default to white
        };
    }

    /// <summary>
    /// Draws the HUD (heads-up display) below the map.
    /// Shows level, health bar, attack power, and key status.
    /// Uses [#4] IFormattable — calls player.ToString("H", null) for the health bar
    /// and player.ToString("F", null) for the full status.
    /// </summary>
    private void DrawHud(Player player, int level, int mapHeight, GameWorld world)
    {
        // Build HUD lines
        string[] hudLines = new string[]
        {
            "",  // Blank separator line
            // [#4] IFormattable — use "H" format for health bar display
            $"  Health: {player.ToString("H", null)}",
            // [#4] IFormattable — use "F" format for full player details
            $"  {player.ToString("F", null)}",
            $"  Enemies remaining: {world.Enemies.Count(e => e.IsActive)}",
            ""
        };

        // Only redraw HUD lines that changed
        for (int i = 0; i < hudLines.Length; i++)
        {
            int row = mapHeight + i;
            string line = hudLines[i];

            // Pad the line to clear any leftover characters from previous frames
            string paddedLine = line.PadRight(Console.WindowWidth > 0 ? Math.Min(Console.WindowWidth - 1, 80) : 80);

            // Check if this HUD line changed
            if (i >= _previousHudLines.Length || _previousHudLines[i] != paddedLine)
            {
                SafeSetCursorPosition(0, row);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(paddedLine);
            }
        }

        // Save for next frame comparison
        string[] padded = new string[hudLines.Length];
        for (int i = 0; i < hudLines.Length; i++)
        {
            padded[i] = hudLines[i].PadRight(Console.WindowWidth > 0 ? Math.Min(Console.WindowWidth - 1, 80) : 80);
        }
        _previousHudLines = padded;
    }

    /// <summary>
    /// Draws the message log below the HUD area.
    /// Shows the most recent game messages (pickups, attacks, etc.).
    /// </summary>
    private void DrawMessages(GameWorld world, int mapHeight)
    {
        // Messages start below the HUD (5 HUD lines)
        int messageStartRow = mapHeight + 5;

        // Get the current messages from the world
        string[] messages = world.MessageLog.ToArray();

        Console.ForegroundColor = ConsoleColor.DarkCyan;

        // Draw up to 5 message lines
        for (int i = 0; i < 5; i++)
        {
            SafeSetCursorPosition(0, messageStartRow + i);

            if (i < messages.Length)
            {
                // Pad to clear old text
                string line = $"  {messages[i]}".PadRight(
                    Console.WindowWidth > 0 ? Math.Min(Console.WindowWidth - 1, 80) : 80);
                Console.Write(line);
            }
            else
            {
                // Clear empty message lines
                Console.Write(new string(' ',
                    Console.WindowWidth > 0 ? Math.Min(Console.WindowWidth - 1, 80) : 80));
            }
        }

        // Reset color
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
        SafeSetCursorPosition(2, startRow);
        Console.Write("Press any key to start...");

        Console.ForegroundColor = ConsoleColor.Gray;
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
