<#
.SYNOPSIS
    Publishes test messages to RabbitMQ for QuickApiMapper integration testing.

.DESCRIPTION
    This script publishes JSON or SOAP test messages to a RabbitMQ exchange
    for processing by QuickApiMapper RabbitMQ consumers.

.PARAMETER IntegrationName
    The name of the integration to process the message (e.g., "CustomerIntegration")

.PARAMETER MessageType
    Type of message to send: "customer" or "order"

.PARAMETER Exchange
    RabbitMQ exchange name (default: "quickapi.exchange")

.PARAMETER RoutingKey
    RabbitMQ routing key (default: derived from MessageType)

.PARAMETER HostName
    RabbitMQ hostname (default: "localhost")

.PARAMETER Port
    RabbitMQ port (default: 5672)

.EXAMPLE
    .\publish-test-message.ps1 -MessageType customer
    Publishes a customer message to the default exchange

.EXAMPLE
    .\publish-test-message.ps1 -MessageType order -IntegrationName "OrderIntegration"
    Publishes an order message with explicit integration name
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$IntegrationName,

    [Parameter(Mandatory=$true)]
    [ValidateSet("customer", "order")]
    [string]$MessageType,

    [Parameter(Mandatory=$false)]
    [string]$Exchange = "quickapi.exchange",

    [Parameter(Mandatory=$false)]
    [string]$RoutingKey,

    [Parameter(Mandatory=$false)]
    [string]$HostName = "localhost",

    [Parameter(Mandatory=$false)]
    [int]$Port = 5672
)

# Set defaults based on message type
if (-not $IntegrationName) {
    $IntegrationName = switch ($MessageType) {
        "customer" { "CustomerIntegration" }
        "order" { "OrderIntegration" }
    }
}

if (-not $RoutingKey) {
    $RoutingKey = switch ($MessageType) {
        "customer" { "customer.created" }
        "order" { "order.created" }
    }
}

# Define test messages
$customerMessage = @{
    customerId = "CUST-$(Get-Random -Minimum 1000 -Maximum 9999)"
    firstName = "John"
    lastName = "Doe"
    email = "john.doe@example.com"
    phoneNumber = "+1-555-0123"
    address = @{
        street = "123 Main St"
        city = "Springfield"
        state = "IL"
        zipCode = "62701"
    }
} | ConvertTo-Json -Compress

$orderMessage = @{
    orderId = "ORD-$(Get-Random -Minimum 1000 -Maximum 9999)"
    customerId = "CUST-1234"
    orderDate = (Get-Date -Format "yyyy-MM-ddTHH:mm:ss")
    totalAmount = (Get-Random -Minimum 100 -Maximum 10000) / 100
    items = @(
        @{
            productId = "PROD-001"
            quantity = 2
            unitPrice = 29.99
        },
        @{
            productId = "PROD-002"
            quantity = 1
            unitPrice = 49.99
        }
    )
} | ConvertTo-Json -Compress

$message = switch ($MessageType) {
    "customer" { $customerMessage }
    "order" { $orderMessage }
}

Write-Host "Publishing $MessageType message to RabbitMQ..." -ForegroundColor Cyan
Write-Host "Exchange: $Exchange" -ForegroundColor Gray
Write-Host "Routing Key: $RoutingKey" -ForegroundColor Gray
Write-Host "Integration: $IntegrationName" -ForegroundColor Gray
Write-Host ""
Write-Host "Message payload:" -ForegroundColor Yellow
Write-Host $message -ForegroundColor White
Write-Host ""

# Create C# script to publish message
$csharpScript = @"
#r "nuget: RabbitMQ.Client, 6.5.0"

using System;
using System.Text;
using System.Collections.Generic;
using RabbitMQ.Client;

try
{
    var factory = new ConnectionFactory
    {
        HostName = "$HostName",
        Port = $Port
    };

    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    // Declare exchange (idempotent)
    channel.ExchangeDeclare(
        exchange: "$Exchange",
        type: "direct",
        durable: true);

    // Create message properties
    var properties = channel.CreateBasicProperties();
    properties.Headers = new Dictionary<string, object>
    {
        ["IntegrationName"] = "$IntegrationName"
    };
    properties.CorrelationId = Guid.NewGuid().ToString();
    properties.Persistent = true;
    properties.ContentType = "application/json";

    // Publish message
    var body = Encoding.UTF8.GetBytes(@"$($message.Replace('"', '\"'))");
    channel.BasicPublish(
        exchange: "$Exchange",
        routingKey: "$RoutingKey",
        basicProperties: properties,
        body: body);

    Console.WriteLine("SUCCESS: Message published with correlation ID: " + properties.CorrelationId);
    Environment.Exit(0);
}
catch (Exception ex)
{
    Console.Error.WriteLine("ERROR: " + ex.Message);
    Environment.Exit(1);
}
"@

# Create temp file
$tempDir = [System.IO.Path]::GetTempPath()
$scriptFile = Join-Path $tempDir "publish-rabbitmq-$(Get-Random).csx"

try {
    # Write script
    Set-Content -Path $scriptFile -Value $csharpScript -Encoding UTF8

    # Check if dotnet-script is installed
    $dotnetScript = Get-Command "dotnet-script" -ErrorAction SilentlyContinue
    if (-not $dotnetScript) {
        Write-Host "Installing dotnet-script tool..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-script
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install dotnet-script"
        }
    }

    # Execute script
    Write-Host "Publishing message..." -ForegroundColor Cyan
    dotnet script $scriptFile

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "Message published successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "1. Check the QuickApiMapper application logs for processing details" -ForegroundColor Gray
        Write-Host "2. View captured messages via the management API" -ForegroundColor Gray
        Write-Host "3. Check the dead-letter queue if message failed: $($RoutingKey).dead-letter" -ForegroundColor Gray
    } else {
        Write-Host ""
        Write-Host "Failed to publish message!" -ForegroundColor Red
        exit 1
    }
}
finally {
    # Clean up
    if (Test-Path $scriptFile) {
        Remove-Item $scriptFile -Force
    }
}
