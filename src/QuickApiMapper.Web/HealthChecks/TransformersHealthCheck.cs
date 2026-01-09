using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.HealthChecks;

public class TransformersHealthCheck : IHealthCheck
{
    private readonly IEnumerable<ITransformer> _transformers;

    public TransformersHealthCheck(IEnumerable<ITransformer> transformers)
    {
        _transformers = transformers;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = _transformers.Count();

            return Task.FromResult(count > 0
                ? HealthCheckResult.Healthy($"Loaded {count} transformer(s)")
                : HealthCheckResult.Degraded("No transformers loaded"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to enumerate transformers", ex));
        }
    }
}
