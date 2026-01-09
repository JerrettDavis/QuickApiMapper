using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.Abstractions.Repositories;

/// <summary>
/// Repository interface for managing global toggle switches.
/// </summary>
public interface IGlobalToggleRepository
{
    /// <summary>
    /// Gets all global toggles.
    /// </summary>
    Task<IEnumerable<GlobalToggleEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a toggle by its unique identifier.
    /// </summary>
    Task<GlobalToggleEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a toggle by its key (e.g., "EmergencyDisableAll").
    /// </summary>
    Task<GlobalToggleEntity?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a toggle is enabled by its key.
    /// Returns false if the toggle doesn't exist.
    /// </summary>
    Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new global toggle.
    /// </summary>
    Task AddAsync(GlobalToggleEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing global toggle.
    /// </summary>
    Task UpdateAsync(GlobalToggleEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a global toggle.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the enabled state of a toggle by key.
    /// Creates the toggle if it doesn't exist.
    /// </summary>
    Task SetToggleAsync(string key, bool isEnabled, string? updatedBy = null, CancellationToken cancellationToken = default);
}
