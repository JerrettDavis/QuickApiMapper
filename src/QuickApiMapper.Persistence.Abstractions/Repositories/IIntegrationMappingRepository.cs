using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.Abstractions.Repositories;

/// <summary>
/// Repository interface for managing integration mappings in the database.
/// </summary>
public interface IIntegrationMappingRepository
{
    /// <summary>
    /// Gets all active integration mappings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active integration mappings.</returns>
    Task<IEnumerable<IntegrationMappingEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an integration mapping by its unique identifier.
    /// </summary>
    /// <param name="id">The integration mapping ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration mapping if found; otherwise, null.</returns>
    Task<IntegrationMappingEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an integration mapping by its name.
    /// </summary>
    /// <param name="name">The integration name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration mapping if found; otherwise, null.</returns>
    Task<IntegrationMappingEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an integration mapping by its endpoint path.
    /// </summary>
    /// <param name="endpoint">The endpoint path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The integration mapping if found; otherwise, null.</returns>
    Task<IntegrationMappingEntity?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new integration mapping to the database.
    /// </summary>
    /// <param name="entity">The integration mapping to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The added integration mapping with generated ID.</returns>
    Task<IntegrationMappingEntity> AddAsync(IntegrationMappingEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing integration mapping.
    /// </summary>
    /// <param name="entity">The integration mapping to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(IntegrationMappingEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an integration mapping by ID.
    /// </summary>
    /// <param name="id">The integration mapping ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all global static values (not specific to any integration).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of global static values.</returns>
    Task<IEnumerable<StaticValueEntity>> GetGlobalStaticValuesAsync(CancellationToken cancellationToken = default);
}
