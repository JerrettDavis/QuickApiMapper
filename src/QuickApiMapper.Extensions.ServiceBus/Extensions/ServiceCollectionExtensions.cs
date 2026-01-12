using System.ComponentModel.DataAnnotations;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickApiMapper.Application.Destinations;
using QuickApiMapper.Extensions.ServiceBus.Destinations;
using QuickApiMapper.Extensions.ServiceBus.Options;
using QuickApiMapper.Extensions.ServiceBus.Workers;

namespace QuickApiMapper.Extensions.ServiceBus.Extensions;

/// <summary>
/// Extension methods for registering Azure Service Bus support in QuickApiMapper.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure Service Bus protocol support to QuickApiMapper.
    /// Registers destination handlers and background workers for message processing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">Azure Service Bus connection string.</param>
    /// <param name="configureOptions">Optional action to configure Service Bus options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceBusSupport(
        this IServiceCollection services,
        string connectionString,
        Action<ServiceBusOptions>? configureOptions = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Service Bus connection string is required", nameof(connectionString));
        }

        // Configure options
        var options = new ServiceBusOptions
        {
            ConnectionString = connectionString
        };
        configureOptions?.Invoke(options);

        // Validate options
        ValidateOptions(options);

        // Register Service Bus client as singleton
        services.AddSingleton(sp => new ServiceBusClient(connectionString, new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                Mode = ServiceBusRetryMode.Exponential,
                MaxRetries = options.MaxRetries,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(30)
            }
        }));

        // Register destination handler with keyed service for modern .NET
        services.AddKeyedSingleton<IDestinationHandler, ServiceBusDestinationHandler>("ServiceBus");
        services.AddKeyedSingleton<IDestinationHandler, ServiceBusDestinationHandler>("SERVICEBUS"); // Case variation

        // Also register non-keyed for backward compatibility
        services.AddSingleton<IDestinationHandler, ServiceBusDestinationHandler>();

        // Register background workers if queues are specified
        if (options.InputQueues != null)
        {
            foreach (var queue in options.InputQueues)
            {
                var queueConfig = queue; // Capture for closure
                services.AddHostedService(sp =>
                {
                    var client = sp.GetRequiredService<ServiceBusClient>();
                    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ServiceBusWorker>>();
                    return new ServiceBusWorker(logger, client, queueConfig.QueueOrTopicName, queueConfig.SubscriptionName);
                });
            }
        }

        return services;
    }

    private static void ValidateOptions(ServiceBusOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ArgumentException($"Invalid ServiceBusOptions configuration: {errors}");
        }

        // Validate nested queue configurations
        if (options.InputQueues != null)
        {
            foreach (var queueConfig in options.InputQueues)
            {
                var queueContext = new ValidationContext(queueConfig);
                var queueResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(queueConfig, queueContext, queueResults, validateAllProperties: true))
                {
                    var queueErrors = string.Join("; ", queueResults.Select(r => r.ErrorMessage));
                    throw new ArgumentException($"Invalid ServiceBusQueueConfig: {queueErrors}");
                }
            }
        }
    }
}
