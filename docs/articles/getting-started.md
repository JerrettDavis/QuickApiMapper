# Getting Started with QuickApiMapper

This guide will help you set up QuickApiMapper and create your first integration.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 10 SDK** or later ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **PostgreSQL 14+** or **SQLite** for persistence
- **Docker Desktop** (optional, for running dependencies)
- **Visual Studio 2022**, **VS Code**, or **Rider** IDE

## Installation

### Option 1: Run with .NET Aspire (Recommended)

The easiest way to run QuickApiMapper is using .NET Aspire, which orchestrates all services:

```bash
# Clone the repository
git clone https://github.com/jerrettdavis/QuickApiMapper.git
cd QuickApiMapper

# Build the solution
dotnet build

# Run with Aspire
dotnet run --project src/QuickApiMapper.Host.AppHost
```

Aspire will automatically:
- Start the Management API (port 5074)
- Start the Runtime Web API (port 5000)
- Start the Web Designer (port 5173)
- Configure PostgreSQL container (if using Docker)
- Set up service discovery and monitoring

Access the Aspire dashboard at `http://localhost:15000` to monitor all services.

### Option 2: Run Individual Services

You can also run services independently:

#### 1. Start the Management API

```bash
cd src/QuickApiMapper.Management.Api
dotnet run
```

The Management API will be available at `https://localhost:7074` (or `http://localhost:5074`)

#### 2. Start the Runtime Web API

```bash
cd src/QuickApiMapper.Web
dotnet run
```

The Runtime API will be available at `https://localhost:7000` (or `http://localhost:5000`)

#### 3. Start the Web Designer

```bash
cd src/QuickApiMapper.Designer.Web
dotnet run
```

The Web Designer will be available at `https://localhost:7173` (or `http://localhost:5173`)

## Database Setup

### SQLite (Default for Development)

QuickApiMapper uses SQLite by default for development. No additional setup is required - the database file will be created automatically on first run at:

```
src/QuickApiMapper.Management.Api/quickapimapper.db
```

Migrations are applied automatically on startup.

### PostgreSQL (Recommended for Production)

To use PostgreSQL:

1. **Start PostgreSQL** (via Docker):

```bash
docker run -d \
 --name quickapimapper-postgres \
 -e POSTGRES_USER=quickapimapper \
 -e POSTGRES_PASSWORD=quickapimapper \
 -e POSTGRES_DB=quickapimapper \
 -p 5432:5432 \
 postgres:17
```

2. **Update connection string** in `src/QuickApiMapper.Management.Api/appsettings.json`:

```json
{
 "ConnectionStrings": {
 "QuickApiMapper": "Host=localhost;Database=quickapimapper;Username=quickapimapper;Password=quickapimapper"
 },
 "Persistence": {
 "Provider": "PostgreSQL"
 }
}
```

3. **Run migrations**:

```bash
cd src/QuickApiMapper.Persistence.PostgreSQL
dotnet ef database update --startup-project ../QuickApiMapper.Management.Api
```

## Verifying Installation

### Check Service Health

Visit the following endpoints to verify services are running:

- **Management API**: `http://localhost:5074/health`
- **Runtime API**: `http://localhost:5000/health`
- **Web Designer**: `http://localhost:5173`

### View Sample Integrations

The database is seeded with 6 sample integrations on first run:

1. Navigate to the Web Designer: `http://localhost:5173`
2. Click "Integrations" in the navigation menu
3. You should see sample integrations like "Customer to ERP Integration"

## Your First Integration

Let's create a simple integration that transforms customer data from JSON to XML.

### Step 1: Open the Web Designer

Navigate to `http://localhost:5173` and click "Create Integration".

### Step 2: Configure Basic Information

- **Name**: `My First Integration`
- **Integration ID**: `first-integration` (auto-generated from name)
- **Source Type**: `JSON`
- **Destination Type**: `SOAP`

### Step 3: Configure Source

For JSON source, you don't need additional configuration. The source resolver will automatically parse incoming JSON payloads.

### Step 4: Configure Destination

For SOAP destination:

- **Endpoint URL**: `https://example.com/api/soap`
- **SOAP Action**: `http://example.com/CreateCustomer`
- **Method Name**: `CreateCustomer`

### Step 5: Map Fields

Add field mappings to transform data from source to destination:

| Source Path | Destination Path | Transformer |
|-------------|------------------|-------------|
| `$.customer.firstName` | `Customer.FirstName` | (none) |
| `$.customer.lastName` | `Customer.LastName` | ToUpper |
| `$.customer.email` | `Customer.Email` | (none) |
| `$.customer.phone` | `Customer.Phone` | FormatPhone |

Click "Add Mapping" for each field.

### Step 6: Add Static Values

You can add static values that are always included:

- **Destination Path**: `Customer.Source`
- **Static Value**: `API`

### Step 7: Test the Integration

Click "Test Integration" to preview the transformation:

**Sample Input (JSON)**:
```json
{
 "customer": {
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "phone": "5551234567"
 }
}
```

**Expected Output (XML)**:
```xml
<CreateCustomerRequest>
 <Customer>
 <FirstName>John</FirstName>
 <LastName>DOE</LastName>
 <Email>john.doe@example.com</Email>
 <Phone>(555) 123-4567</Phone>
 <Source>API</Source>
 </Customer>
</CreateCustomerRequest>
```

### Step 8: Save and Deploy

Click "Save Integration" to persist the configuration to the database.

### Step 9: Send Requests

Send requests to the Runtime API endpoint:

```bash
curl -X POST http://localhost:5000/api/map/first-integration \
 -H "Content-Type: application/json" \
 -d '{
 "customer": {
 "firstName": "John",
 "lastName": "Doe",
 "email": "john.doe@example.com",
 "phone": "5551234567"
 }
 }'
```

The API will:
1. Parse the JSON input
2. Apply field mappings and transformers
3. Generate SOAP XML
4. Send to the configured endpoint
5. Return the response

## Next Steps

Now that you have QuickApiMapper running and created your first integration, explore these topics:

- [Architecture Overview](architecture.md) - Understand the core components
- [Creating Integrations](creating-integrations.md) - Detailed integration guide
- [Transformers](transformers.md) - Built-in and custom transformers
- [Behaviors](behaviors.md) - Add authentication, validation, and more
- [Message Capture](message-capture.md) - Debug and audit messages
- [Deployment](deployment.md) - Production deployment strategies

## Troubleshooting

### Port Conflicts

If you encounter port conflicts, you can change the default ports in `launchSettings.json` or by setting environment variables:

```bash
export ASPNETCORE_URLS="http://localhost:8080"
dotnet run --project src/QuickApiMapper.Management.Api
```

### Database Connection Errors

If you see database connection errors:

1. **SQLite**: Ensure the application has write permissions to the directory
2. **PostgreSQL**: Verify PostgreSQL is running and the connection string is correct

```bash
# Test PostgreSQL connection
psql -h localhost -U quickapimapper -d quickapimapper
```

### Missing Integrations

If the Web Designer shows no integrations:

1. Check the Management API logs for migration errors
2. Verify the database was created and seeded
3. Manually run migrations if needed

```bash
cd src/QuickApiMapper.Persistence.SQLite
dotnet ef database update --startup-project ../QuickApiMapper.Management.Api
```

## Getting Help

- **Documentation**: Browse the [full documentation](../index.md)
- **GitHub Issues**: Report bugs at [GitHub](https://github.com/jerrettdavis/QuickApiMapper/issues)
- **Discussions**: Ask questions in [GitHub Discussions](https://github.com/jerrettdavis/QuickApiMapper/discussions)
