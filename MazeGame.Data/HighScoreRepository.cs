// HighScoreRepository.cs — Async data-access layer for HighScore entries. [ND2 #14]
// Demonstrates [ND2 #6] try/catch wrapping EF exceptions into DatabaseException,
// and [ND2 #13] LINQ over EF (OrderByDescending, Take, ToListAsync, CountAsync).

using MazeGame.Core.Exceptions;
using MazeGame.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MazeGame.Data;

/// <summary>
/// Repository over the HighScores table. All public methods are async and
/// translate any EF Core failure into a DatabaseException so callers see
/// one stable exception type from this assembly.
/// </summary>
public class HighScoreRepository
{
    private readonly GameDbContext _context;

    public HighScoreRepository(GameDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Persists a new score row. [ND2 #6] try/catch wraps EF errors as DatabaseException.
    /// </summary>
    public async Task SaveScoreAsync(HighScore score)
    {
        try
        {
            await _context.HighScores.AddAsync(score);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new DatabaseException("Failed to save high score.", ex);
        }
    }

    /// <summary>
    /// Returns the top <paramref name="count"/> scores, highest first.
    /// [ND2 #13] LINQ-to-Entities query — translated to SQL by EF Core.
    /// </summary>
    public async Task<List<HighScore>> GetTopScoresAsync(int count = 10)
    {
        try
        {
            return await _context.HighScores
                .OrderByDescending(s => s.Score)
                .ThenByDescending(s => s.AchievedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new DatabaseException("Failed to load top scores.", ex);
        }
    }

    /// <summary>
    /// Total number of completed runs ever recorded. [ND2 #13] CountAsync.
    /// </summary>
    public async Task<int> GetTotalGamesPlayedAsync()
    {
        try
        {
            return await _context.HighScores.CountAsync();
        }
        catch (Exception ex)
        {
            throw new DatabaseException("Failed to count games.", ex);
        }
    }
}
