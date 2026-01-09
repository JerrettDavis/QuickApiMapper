using Microsoft.Extensions.DependencyInjection;
using QuickApiMapper.MessageCapture.Abstractions.Interfaces;
using QuickApiMapper.MessageCapture.Abstractions.Options;
using QuickApiMapper.MessageCapture.InMemory.Providers;

namespace QuickApiMapper.MessageCapture.InMemory.Extensions;

/// <summary>
/// Extension methods for registering in-memory message capture.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds in-memory message capture provider.
    /// </summary>
    public static IServiceCollection AddInMemoryMessageCapture(
        this IServiceCollection services,
        Action<MessageCaptureOptions>? configure = null)
    {
        var options = new MessageCaptureOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IMessageCaptureProvider, InMemoryMessageCaptureProvider>();

        return services;
    }
}
