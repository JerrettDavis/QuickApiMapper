using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.HealthChecks;

public class ConfigurationProviderHealthCheck : IHealthCheck
{
    private readonly IIntegrationConfigurationProvider _provider;

    public ConfigurationProviderHealthCheck(IIntegrationConfigurationProvider provider)
    {
        _provider = provider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var integrations = await _provider.GetAllActiveIntegrationsAsync();
            var count = integrations.Count();

            return count > 0
                ? HealthCheckResult.Healthy($"Loaded {count} active integration(s)")
                : HealthCheckResult.Degraded("No active integrations configured");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Failed to load integrations", ex);
        }
    }
}
