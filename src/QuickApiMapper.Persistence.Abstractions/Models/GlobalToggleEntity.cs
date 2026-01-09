namespace QuickApiMapper.Persistence.Abstractions.Models;

/// <summary>
/// Entity representing global toggle switches for system-wide controls.
/// Examples: EmergencyDisableAll, MaintenanceMode, etc.
/// </summary>
public class GlobalToggleEntity
{
    /// <summary>
    /// Unique identifier for the toggle.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique key for the toggle (e.g., "EmergencyDisableAll", "MaintenanceMode").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this toggle controls.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this toggle is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Timestamp when this toggle was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this toggle was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who last updated this toggle.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
