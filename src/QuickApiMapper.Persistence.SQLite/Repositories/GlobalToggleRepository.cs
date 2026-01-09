using Microsoft.EntityFrameworkCore;
using QuickApiMapper.Persistence.Abstractions.Models;
using QuickApiMapper.Persistence.Abstractions.Repositories;

namespace QuickApiMapper.Persistence.SQLite.Repositories;

/// <summary>
/// SQLite implementation of the global toggle repository.
/// </summary>
public class GlobalToggleRepository : IGlobalToggleRepository
{
    private readonly QuickApiMapperSqliteDbContext _context;

    public GlobalToggleRepository(QuickApiMapperSqliteDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<GlobalToggleEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GlobalToggles
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<GlobalToggleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GlobalToggles
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<GlobalToggleEntity?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.GlobalToggles
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key, cancellationToken);
    }

    public async Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default)
    {
        var toggle = await _context.GlobalToggles
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key, cancellationToken);

        return toggle?.IsEnabled ?? false;
    }

    public async Task AddAsync(GlobalToggleEntity entity, CancellationToken cancellationToken = default)
    {
        await _context.GlobalToggles.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(GlobalToggleEntity entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.GlobalToggles.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.GlobalToggles.FindAsync([id], cancellationToken);
        if (entity != null)
        {
            _context.GlobalToggles.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetToggleAsync(string key, bool isEnabled, string? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var toggle = await _context.GlobalToggles
            .FirstOrDefaultAsync(t => t.Key == key, cancellationToken);

        if (toggle == null)
        {
            // Create new toggle if it doesn't exist
            toggle = new GlobalToggleEntity
            {
                Id = Guid.NewGuid(),
                Key = key,
                Description = $"Auto-created toggle for {key}",
                IsEnabled = isEnabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = updatedBy
            };
            await _context.GlobalToggles.AddAsync(toggle, cancellationToken);
        }
        else
        {
            // Update existing toggle
            toggle.IsEnabled = isEnabled;
            toggle.UpdatedAt = DateTime.UtcNow;
            toggle.UpdatedBy = updatedBy;
            _context.GlobalToggles.Update(toggle);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
