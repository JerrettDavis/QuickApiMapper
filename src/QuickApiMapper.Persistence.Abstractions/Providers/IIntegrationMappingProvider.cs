using QuickApiMapper.Persistence.Abstractions.Models;

namespace QuickApiMapper.Persistence.Abstractions.Providers;

/// <summary>
/// Provider interface for retrieving integration mapping configurations.
/// Abstracts the source (file-based, database, etc.) from consumers.
/// </summary>
public interface IIntegrationMappingProvider
{
    /// <summary>
    /// Gets all active integration mappings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active integration mappings.</returns>
    Task<IEnumerable<IntegrationMappingEntity>> GetAllAsync(CancellationToken cancellationToken = default);

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
    /// Gets all global static values (not specific to any integration).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of global static values.</returns>
    Task<Dictionary<string, string>> GetGlobalStaticValuesAsync(CancellationToken cancellationToken = default);
}
