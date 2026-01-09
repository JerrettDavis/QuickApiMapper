# QuickApiMapper Documentation

Welcome to the QuickApiMapper documentation! QuickApiMapper is a powerful, flexible API integration and mapping framework for .NET that enables seamless data transformation between different formats and protocols.

## Overview

QuickApiMapper provides a pluggable architecture for integrating disparate systems by transforming data between JSON, XML, SOAP, gRPC, and other formats. It supports multiple messaging protocols including RabbitMQ, Azure Service Bus, and direct HTTP endpoints.

## Key Features

- **Flexible Mapping Engine**: Transform data between JSON and XML with configurable field mappings
- **Multiple Protocols**: Built-in support for REST, SOAP, gRPC, RabbitMQ, and Azure Service Bus
- **Pluggable Architecture**: Extend with custom transformers, behaviors, and destination handlers
- **Behavior Pipeline**: Add cross-cutting concerns like authentication, validation, timing, and message capture
- **Database Persistence**: Store integration configurations in PostgreSQL or SQLite
- **Message Capture**: Capture and inspect messages for debugging and auditing
- **Test Mode**: Preview transformations without hitting destination systems
- **Web Designer**: Blazor-based UI for managing integrations
- **Aspire Integration**: Built-in support for .NET Aspire orchestration

## Quick Links

- [Getting Started](articles/getting-started.md) - Installation and basic setup
- [Architecture Overview](articles/architecture.md) - Understanding the core architecture
- [Creating Integrations](articles/creating-integrations.md) - Step-by-step integration guide
- [Transformers](articles/transformers.md) - Built-in and custom transformers
- [Behaviors](articles/behaviors.md) - Implementing cross-cutting concerns
- [Message Capture](articles/message-capture.md) - Debugging and auditing messages
- [API Reference](api/index.md) - Complete API documentation
- [Deployment Guide](articles/deployment.md) - Production deployment options

## Architecture

QuickApiMapper follows a modular architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│ Management API / Web Designer │
│ (Configure integrations, view messages) │
└─────────────────────────────────────────────────────────────┘
 │
 ▼
┌─────────────────────────────────────────────────────────────┐
│ Runtime API (Web) │
│ (Receives requests, executes mappings) │
└─────────────────────────────────────────────────────────────┘
 │
 ┌─────────────────────┼─────────────────────┐
 ▼ ▼ ▼
┌──────────────┐ ┌──────────────────┐ ┌──────────────┐
│ Source │ │ Mapping Engine │ │ Destination │
│ Resolvers │───▶│ + Behaviors │───▶│ Handlers │
│ (JSON, XML) │ │ + Transformers │ │ (SOAP, gRPC) │
└──────────────┘ └──────────────────┘ └──────────────┘
 │
 ▼
 ┌──────────────────┐
 │ Persistence │
 │ (PostgreSQL/ │
 │ SQLite) │
 └──────────────────┘
```

## Project Structure

The solution is organized into several key areas:

### Core Projects (`src/`)

- **QuickApiMapper.Contracts** - Core interfaces and contracts
- **QuickApiMapper.Application** - Mapping engine and core logic
- **QuickApiMapper.Behaviors** - Built-in behaviors (auth, validation, timing)
- **QuickApiMapper.StandardTransformers** - Standard data transformers
- **QuickApiMapper.CustomTransformers** - Example custom transformers

### Persistence (`src/`)

- **QuickApiMapper.Persistence.Abstractions** - Repository interfaces
- **QuickApiMapper.Persistence.PostgreSQL** - PostgreSQL implementation
- **QuickApiMapper.Persistence.SQLite** - SQLite implementation

### Extensions (`src/`)

- **QuickApiMapper.Extensions.gRPC** - gRPC protocol support
- **QuickApiMapper.Extensions.RabbitMQ** - RabbitMQ integration
- **QuickApiMapper.Extensions.ServiceBus** - Azure Service Bus integration

### Message Capture (`src/`)

- **QuickApiMapper.MessageCapture.Abstractions** - Message capture interfaces
- **QuickApiMapper.MessageCapture.InMemory** - In-memory message storage

### Applications (`src/`)

- **QuickApiMapper.Web** - Runtime API for executing integrations
- **QuickApiMapper.Management.Api** - Management API for configuration
- **QuickApiMapper.Designer.Web** - Blazor UI for managing integrations
- **QuickApiMapper.Host.AppHost** - .NET Aspire orchestration host

### Tests (`tests/`)

- **QuickApiMapper.UnitTests** - Unit tests for core functionality
- **QuickApiMapper.IntegrationTests** - Integration tests with PostgreSQL and messaging

## Getting Started

### Prerequisites

- .NET 10 SDK or later
- PostgreSQL 14+ or SQLite (for persistence)
- Optional: Docker (for running dependencies)

### Installation

1. Clone the repository:
 ```bash
 git clone https://github.com/jerrettdavis/QuickApiMapper.git
 cd QuickApiMapper
 ```

2. Build the solution:
 ```bash
 dotnet build
 ```

3. Run with Aspire (recommended):
 ```bash
 dotnet run --project src/QuickApiMapper.Host.AppHost
 ```

4. Or run individual applications:
 ```bash
 # Start the Management API
 dotnet run --project src/QuickApiMapper.Management.Api

 # Start the Runtime Web API
 dotnet run --project src/QuickApiMapper.Web

 # Start the Web Designer
 dotnet run --project src/QuickApiMapper.Designer.Web
 ```

### Your First Integration

Create a simple JSON to XML integration:

1. Navigate to the Web Designer at `http://localhost:5173`
2. Click "Create Integration"
3. Configure source (JSON) and destination (XML/SOAP)
4. Map fields from source to destination
5. Add transformers (e.g., ToUpper, FormatPhone)
6. Test the integration with sample data
7. Deploy and start receiving requests

For detailed steps, see the [Creating Integrations](articles/creating-integrations.md) guide.

## Contributing

We welcome contributions! Please see our [Contributing Guide](articles/contributing.md) for details.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

- [GitHub Issues](https://github.com/jerrettdavis/QuickApiMapper/issues) - Report bugs or request features
- [Discussions](https://github.com/jerrettdavis/QuickApiMapper/discussions) - Ask questions and share ideas
