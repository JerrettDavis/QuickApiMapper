using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickApiMapper.Contracts;

namespace QuickApiMapper.Extensions.ServiceBus.Workers;

/// <summary>
/// Background worker that processes messages from Azure Service Bus.
/// Receives messages from a queue or topic subscription and processes them through QuickApiMapper.
/// </summary>
public class ServiceBusWorker : BackgroundService
{
    private readonly ILogger<ServiceBusWorker> _logger;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;
    private readonly string _queueOrTopicName;

    public ServiceBusWorker(
        ILogger<ServiceBusWorker> logger,
        ServiceBusClient client,
        string queueOrTopicName,
        string? subscriptionName = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _queueOrTopicName = queueOrTopicName ?? throw new ArgumentNullException(nameof(queueOrTopicName));

        // Create processor for either queue or topic subscription
        _processor = string.IsNullOrEmpty(subscriptionName)
            ? _client.CreateProcessor(_queueOrTopicName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 10,
                AutoCompleteMessages = false
            })
            : _client.CreateProcessor(_queueOrTopicName, subscriptionName, new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 10,
                AutoCompleteMessages = false
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Service Bus worker for {QueueOrTopic}", _queueOrTopicName);

        await _processor.StartProcessingAsync(stoppingToken);

        // Keep the worker running until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            _logger.LogInformation("Received message: {MessageId}", args.Message.MessageId);

            // TODO: Process message through QuickApiMapper
            // This would involve:
            // 1. Getting the integration configuration for this queue
            // 2. Applying field mappings
            // 3. Sending to destination

            // For now, just log the message
            _logger.LogDebug("Message body: {Body}", messageBody);

            // Complete the message
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}", args.Message.MessageId);

            // Move to dead-letter queue
            await args.DeadLetterMessageAsync(args.Message, "ProcessingError", ex.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error in {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Service Bus worker");

        await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _processor?.DisposeAsync().AsTask().Wait();
        base.Dispose();
    }
}
