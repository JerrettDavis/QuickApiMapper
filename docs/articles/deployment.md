# Deployment Guide

This guide covers deploying QuickApiMapper to various environments, from development to production.

## Overview

QuickApiMapper consists of three main applications:

1. **Management API** - Configuration and integration management
2. **Runtime Web API** - Executes integrations
3. **Web Designer** - Blazor UI for managing integrations

You can deploy all three together or separately based on your needs.

## Deployment Options

### Option 1: .NET Aspire (Recommended)

Deploy all services using .NET Aspire for automatic orchestration, service discovery, and monitoring.

### Option 2: Docker Containers

Deploy each service as a Docker container with orchestration via Docker Compose or Kubernetes.

### Option 3: Azure App Service

Deploy to Azure App Service for managed hosting with built-in scaling and monitoring.

### Option 4: Self-Hosted

Deploy to on-premises servers using IIS, Kestrel, or systemd.

## Prerequisites

### All Deployments

- .NET 10 Runtime
- PostgreSQL 14+ or SQLite
- SSL certificates for HTTPS

### Optional

- Docker (for containerized deployments)
- Azure subscription (for Azure deployments)
- Kubernetes cluster (for K8s deployments)

## Deployment with .NET Aspire

### Step 1: Configure Aspire Host

**appsettings.Production.json** in `QuickApiMapper.Host.AppHost`:

```json
{
 "Logging": {
 "LogLevel": {
 "Default": "Information",
 "Microsoft.AspNetCore": "Warning"
 }
 },
 "ConnectionStrings": {
 "QuickApiMapper": "Host=postgres;Database=quickapimapper;Username=quickapimapper;Password=<strong-password>"
 }
}
```

### Step 2: Build and Publish

```bash
# Build the solution
dotnet build -c Release

# Publish Aspire host
dotnet publish src/QuickApiMapper.Host.AppHost -c Release -o ./publish
```

### Step 3: Run in Production

```bash
cd publish
export ASPNETCORE_ENVIRONMENT=Production
dotnet QuickApiMapper.Host.AppHost.dll
```

### Step 4: Configure Reverse Proxy

Use nginx or another reverse proxy for HTTPS termination:

**nginx.conf**:
```nginx
server {
 listen 443 ssl;
 server_name quickapimapper.example.com;

 ssl_certificate /etc/ssl/certs/quickapimapper.crt;
 ssl_certificate_key /etc/ssl/private/quickapimapper.key;

 location / {
 proxy_pass http://localhost:5173;
 proxy_set_header Host $host;
 proxy_set_header X-Real-IP $remote_addr;
 proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
 proxy_set_header X-Forwarded-Proto $scheme;
 }

 location /api/ {
 proxy_pass http://localhost:5000;
 proxy_set_header Host $host;
 }

 location /management/ {
 proxy_pass http://localhost:5074;
 proxy_set_header Host $host;
 }
}
```

## Deployment with Docker

### Step 1: Create Dockerfiles

**Management API Dockerfile**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/QuickApiMapper.Management.Api/QuickApiMapper.Management.Api.csproj", "src/QuickApiMapper.Management.Api/"]
RUN dotnet restore "src/QuickApiMapper.Management.Api/QuickApiMapper.Management.Api.csproj"
COPY . .
WORKDIR "/src/src/QuickApiMapper.Management.Api"
RUN dotnet build "QuickApiMapper.Management.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QuickApiMapper.Management.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuickApiMapper.Management.Api.dll"]
```

**Runtime Web API Dockerfile** (similar structure):
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/QuickApiMapper.Web/QuickApiMapper.Web.csproj", "src/QuickApiMapper.Web/"]
RUN dotnet restore "src/QuickApiMapper.Web/QuickApiMapper.Web.csproj"
COPY . .
WORKDIR "/src/src/QuickApiMapper.Web"
RUN dotnet build "QuickApiMapper.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QuickApiMapper.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuickApiMapper.Web.dll"]
```

### Step 2: Create Docker Compose

**docker-compose.yml**:
```yaml
version: '3.8'

services:
 postgres:
 image: postgres:17
 environment:
 POSTGRES_DB: quickapimapper
 POSTGRES_USER: quickapimapper
 POSTGRES_PASSWORD: ${DB_PASSWORD}
 volumes:
 - postgres_data:/var/lib/postgresql/data
 networks:
 - quickapimapper
 healthcheck:
 test: ["CMD-SHELL", "pg_isready -U quickapimapper"]
 interval: 10s
 timeout: 5s
 retries: 5

 management-api:
 build:
 context: .
 dockerfile: src/QuickApiMapper.Management.Api/Dockerfile
 environment:
 - ASPNETCORE_ENVIRONMENT=Production
 - ConnectionStrings__QuickApiMapper=Host=postgres;Database=quickapimapper;Username=quickapimapper;Password=${DB_PASSWORD}
 - Persistence__Provider=PostgreSQL
 depends_on:
 postgres:
 condition: service_healthy
 networks:
 - quickapimapper
 ports:
 - "5074:8080"

 web-api:
 build:
 context: .
 dockerfile: src/QuickApiMapper.Web/Dockerfile
 environment:
 - ASPNETCORE_ENVIRONMENT=Production
 - ConnectionStrings__QuickApiMapper=Host=postgres;Database=quickapimapper;Username=quickapimapper;Password=${DB_PASSWORD}
 - Persistence__Provider=PostgreSQL
 depends_on:
 postgres:
 condition: service_healthy
 networks:
 - quickapimapper
 ports:
 - "5000:8080"

 designer-web:
 build:
 context: .
 dockerfile: src/QuickApiMapper.Designer.Web/Dockerfile
 environment:
 - ASPNETCORE_ENVIRONMENT=Production
 - ApiClient__BaseUrl=http://management-api:8080
 depends_on:
 - management-api
 networks:
 - quickapimapper
 ports:
 - "5173:8080"

networks:
 quickapimapper:
 driver: bridge

volumes:
 postgres_data:
```

### Step 3: Deploy

```bash
# Create .env file
echo "DB_PASSWORD=your-secure-password" > .env

# Build and start
docker-compose up -d

# View logs
docker-compose logs -f

# Scale runtime API
docker-compose up -d --scale web-api=3
```

## Deployment to Azure

### Azure App Service

#### Step 1: Create Azure Resources

```bash
# Login to Azure
az login

# Create resource group
az group create --name quickapimapper-rg --location eastus

# Create PostgreSQL server
az postgres flexible-server create \
 --resource-group quickapimapper-rg \
 --name quickapimapper-db \
 --location eastus \
 --admin-user quickapimapper \
 --admin-password <secure-password> \
 --sku-name Standard_B1ms \
 --version 16

# Create database
az postgres flexible-server db create \
 --resource-group quickapimapper-rg \
 --server-name quickapimapper-db \
 --database-name quickapimapper

# Create App Service Plan
az appservice plan create \
 --name quickapimapper-plan \
 --resource-group quickapimapper-rg \
 --sku P1V2 \
 --is-linux

# Create Web Apps
az webapp create \
 --resource-group quickapimapper-rg \
 --plan quickapimapper-plan \
 --name quickapimapper-management \
 --runtime "DOTNET|10.0"

az webapp create \
 --resource-group quickapimapper-rg \
 --plan quickapimapper-plan \
 --name quickapimapper-web \
 --runtime "DOTNET|10.0"

az webapp create \
 --resource-group quickapimapper-rg \
 --plan quickapimapper-plan \
 --name quickapimapper-designer \
 --runtime "DOTNET|10.0"
```

#### Step 2: Configure App Settings

```bash
# Get PostgreSQL connection string
PG_CONN="Host=quickapimapper-db.postgres.database.azure.com;Database=quickapimapper;Username=quickapimapper;Password=<password>;SslMode=Require"

# Configure Management API
az webapp config appsettings set \
 --resource-group quickapimapper-rg \
 --name quickapimapper-management \
 --settings \
 ConnectionStrings__QuickApiMapper="$PG_CONN" \
 Persistence__Provider="PostgreSQL"

# Configure Runtime API
az webapp config appsettings set \
 --resource-group quickapimapper-rg \
 --name quickapimapper-web \
 --settings \
 ConnectionStrings__QuickApiMapper="$PG_CONN" \
 Persistence__Provider="PostgreSQL"

# Configure Designer
az webapp config appsettings set \
 --resource-group quickapimapper-rg \
 --name quickapimapper-designer \
 --settings \
 ApiClient__BaseUrl="https://quickapimapper-management.azurewebsites.net"
```

#### Step 3: Deploy Applications

```bash
# Build and publish
dotnet publish src/QuickApiMapper.Management.Api -c Release -o ./publish/management
dotnet publish src/QuickApiMapper.Web -c Release -o ./publish/web
dotnet publish src/QuickApiMapper.Designer.Web -c Release -o ./publish/designer

# Create deployment packages
cd publish/management && zip -r ../management.zip . && cd ../..
cd publish/web && zip -r ../web.zip . && cd ../..
cd publish/designer && zip -r ../designer.zip . && cd ../..

# Deploy
az webapp deployment source config-zip \
 --resource-group quickapimapper-rg \
 --name quickapimapper-management \
 --src publish/management.zip

az webapp deployment source config-zip \
 --resource-group quickapimapper-rg \
 --name quickapimapper-web \
 --src publish/web.zip

az webapp deployment source config-zip \
 --resource-group quickapimapper-rg \
 --name quickapimapper-designer \
 --src publish/designer.zip
```

### Azure Container Instances

Deploy using Docker containers:

```bash
# Create container registry
az acr create \
 --resource-group quickapimapper-rg \
 --name quickapimapper \
 --sku Basic

# Build and push images
az acr build \
 --registry quickapimapper \
 --image quickapimapper/management:latest \
 --file src/QuickApiMapper.Management.Api/Dockerfile .

az acr build \
 --registry quickapimapper \
 --image quickapimapper/web:latest \
 --file src/QuickApiMapper.Web/Dockerfile .

# Deploy container group
az container create \
 --resource-group quickapimapper-rg \
 --name quickapimapper \
 --image quickapimapper.azurecr.io/quickapimapper/web:latest \
 --registry-login-server quickapimapper.azurecr.io \
 --registry-username <username> \
 --registry-password <password> \
 --dns-name-label quickapimapper \
 --ports 80
```

## Deployment to Kubernetes

### Step 1: Create Kubernetes Manifests

**postgres-deployment.yaml**:
```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
 name: postgres-pvc
spec:
 accessModes:
 - ReadWriteOnce
 resources:
 requests:
 storage: 10Gi
---
apiVersion: apps/v1
kind: Deployment
metadata:
 name: postgres
spec:
 replicas: 1
 selector:
 matchLabels:
 app: postgres
 template:
 metadata:
 labels:
 app: postgres
 spec:
 containers:
 - name: postgres
 image: postgres:17
 env:
 - name: POSTGRES_DB
 value: quickapimapper
 - name: POSTGRES_USER
 value: quickapimapper
 - name: POSTGRES_PASSWORD
 valueFrom:
 secretKeyRef:
 name: postgres-secret
 key: password
 ports:
 - containerPort: 5432
 volumeMounts:
 - name: postgres-storage
 mountPath: /var/lib/postgresql/data
 volumes:
 - name: postgres-storage
 persistentVolumeClaim:
 claimName: postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
 name: postgres
spec:
 selector:
 app: postgres
 ports:
 - port: 5432
```

**web-api-deployment.yaml**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
 name: quickapimapper-web
spec:
 replicas: 3
 selector:
 matchLabels:
 app: quickapimapper-web
 template:
 metadata:
 labels:
 app: quickapimapper-web
 spec:
 containers:
 - name: web
 image: quickapimapper/web:latest
 env:
 - name: ASPNETCORE_ENVIRONMENT
 value: Production
 - name: ConnectionStrings__QuickApiMapper
 valueFrom:
 secretKeyRef:
 name: db-connection
 key: connection-string
 ports:
 - containerPort: 8080
 livenessProbe:
 httpGet:
 path: /health
 port: 8080
 initialDelaySeconds: 30
 periodSeconds: 10
 readinessProbe:
 httpGet:
 path: /health
 port: 8080
 initialDelaySeconds: 10
 periodSeconds: 5
 resources:
 requests:
 memory: "256Mi"
 cpu: "500m"
 limits:
 memory: "512Mi"
 cpu: "1000m"
---
apiVersion: v1
kind: Service
metadata:
 name: quickapimapper-web
spec:
 type: LoadBalancer
 selector:
 app: quickapimapper-web
 ports:
 - port: 80
 targetPort: 8080
```

### Step 2: Create Secrets

```bash
# Create database password secret
kubectl create secret generic postgres-secret \
 --from-literal=password=your-secure-password

# Create connection string secret
kubectl create secret generic db-connection \
 --from-literal=connection-string="Host=postgres;Database=quickapimapper;Username=quickapimapper;Password=your-secure-password"
```

### Step 3: Deploy

```bash
# Apply manifests
kubectl apply -f postgres-deployment.yaml
kubectl apply -f web-api-deployment.yaml
kubectl apply -f management-api-deployment.yaml
kubectl apply -f designer-deployment.yaml

# Check status
kubectl get pods
kubectl get services

# Scale
kubectl scale deployment quickapimapper-web --replicas=5
```

## Configuration

### Environment Variables

**Management API**:
- `ASPNETCORE_ENVIRONMENT` - Environment name (Development, Production)
- `ConnectionStrings__QuickApiMapper` - Database connection string
- `Persistence__Provider` - PostgreSQL or SQLite

**Runtime Web API**:
- `ASPNETCORE_ENVIRONMENT`
- `ConnectionStrings__QuickApiMapper`
- `Persistence__Provider`
- `MessageCapture__MaxPayloadSizeKB` - Max captured message size

**Web Designer**:
- `ApiClient__BaseUrl` - Management API URL

### appsettings.Production.json

```json
{
 "Logging": {
 "LogLevel": {
 "Default": "Information",
 "Microsoft.AspNetCore": "Warning",
 "Microsoft.EntityFrameworkCore": "Warning"
 }
 },
 "AllowedHosts": "*",
 "ConnectionStrings": {
 "QuickApiMapper": "Host=postgres;Database=quickapimapper;Username=quickapimapper;Password=<from-env>"
 },
 "Persistence": {
 "Provider": "PostgreSQL"
 },
 "MessageCapture": {
 "MaxPayloadSizeKB": 1024,
 "RetentionPeriod": "7.00:00:00"
 }
}
```

## Monitoring and Observability

### Health Checks

All applications expose `/health` endpoints:

```bash
curl http://localhost:5000/health
curl http://localhost:5074/health
```

### Logging

Configure structured logging with Serilog:

**Program.cs**:
```csharp
builder.Host.UseSerilog((context, services, configuration) => configuration
 .ReadFrom.Configuration(context.Configuration)
 .ReadFrom.Services(services)
 .Enrich.FromLogContext()
 .WriteTo.Console()
 .WriteTo.Seq("http://seq:5341"));
```

### Application Insights

For Azure deployments:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Metrics

Export metrics to Prometheus:

```csharp
builder.Services.AddOpenTelemetry()
 .WithMetrics(metrics => metrics
 .AddAspNetCoreInstrumentation()
 .AddHttpClientInstrumentation()
 .AddPrometheusExporter());
```

## Security

### HTTPS

Always use HTTPS in production:

```csharp
builder.Services.AddHttpsRedirection(options =>
{
 options.HttpsPort = 443;
});

app.UseHttpsRedirection();
```

### Authentication

Implement authentication for Management API:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
 .AddJwtBearer(options =>
 {
 options.Authority = "https://your-identity-provider.com";
 options.Audience = "quickapimapper-api";
 });
```

### Secrets Management

Use Azure Key Vault or similar:

```csharp
builder.Configuration.AddAzureKeyVault(
 new Uri("https://your-keyvault.vault.azure.net/"),
 new DefaultAzureCredential());
```

## Backup and Recovery

### Database Backups

**PostgreSQL**:
```bash
# Automated backups with pg_dump
pg_dump -h postgres -U quickapimapper quickapimapper > backup.sql

# Restore
psql -h postgres -U quickapimapper quickapimapper < backup.sql
```

**Azure PostgreSQL**:
Automated backups are enabled by default with 7-day retention.

### Configuration Backups

Export integration configurations:

```bash
curl http://localhost:5074/api/integrations > integrations-backup.json
```

## Scaling

### Horizontal Scaling

Scale Runtime API for high load:

**Docker Compose**:
```bash
docker-compose up -d --scale web-api=5
```

**Kubernetes**:
```bash
kubectl scale deployment quickapimapper-web --replicas=10
```

### Database Connection Pooling

Configure connection pooling:

```csharp
options.UseNpgsql(connectionString, npgsqlOptions =>
{
 npgsqlOptions.MaxBatchSize(100);
 npgsqlOptions.CommandTimeout(30);
});
```

### Caching

Enable distributed caching:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
 options.Configuration = "redis:6379";
});

builder.Services.AddCachedConfigurationProvider();
```

## Troubleshooting

### Database Migration Issues

```bash
# Manually run migrations
dotnet ef database update --project src/QuickApiMapper.Persistence.PostgreSQL --startup-project src/QuickApiMapper.Management.Api
```

### Container Connection Issues

Check network connectivity:

```bash
docker-compose exec web-api ping postgres
docker-compose exec web-api nc -zv postgres 5432
```

### Performance Issues

Enable detailed logging:

```json
{
 "Logging": {
 "LogLevel": {
 "Microsoft.EntityFrameworkCore.Database.Command": "Information"
 }
 }
}
```

## Next Steps

- [Configuration](configuration.md) - Detailed configuration options
- [Architecture](architecture.md) - System architecture overview
- [Persistence](persistence.md) - Database configuration
