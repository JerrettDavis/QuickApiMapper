using QuickApiMapper.Contracts;

namespace QuickApiMapper.Application.Models;

/// <summary>
/// Cached integration mapping with automatic expiration tracking.
/// Uses C# 14 'field' keyword for compiler-synthesized backing fields.
/// </summary>
public class CachedIntegration
{
    /// <summary>
    /// Gets or sets the integration mapping.
    /// C# 14 feature: Uses implicit 'field' keyword for backing storage.
    /// </summary>
    public required IntegrationMapping Integration
    {
        get => field;
        set
        {
            field = value;
            LastAccessed = DateTime.UtcNow; // Automatically update access time
        }
    }

    /// <summary>
    /// Gets or sets the cache expiration time.
    /// C# 14 feature: Custom logic with implicit backing field.
    /// </summary>
    public DateTime ExpiresAt
    {
        get => field;
        set
        {
            if (value <= DateTime.UtcNow)
                throw new ArgumentException("Expiration time must be in the future", nameof(value));
            field = value;
        }
    } = DateTime.UtcNow.AddMinutes(5);

    /// <summary>
    /// Gets the last accessed time.
    /// Auto-updated whenever Integration is set.
    /// </summary>
    public DateTime LastAccessed { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets whether this cache entry has expired.
    /// C# 14 extension-style computed property.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Gets the number of seconds until expiration.
    /// </summary>
    public double SecondsUntilExpiration => (ExpiresAt - DateTime.UtcNow).TotalSeconds;
}
