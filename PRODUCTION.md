# Production Deployment Guide

This guide provides instructions for deploying the eShop application to production environments.

## Prerequisites

- Azure subscription (for Azure deployments)
- .NET 8.0 SDK or later
- Docker Desktop (for local testing)
- Azure CLI (for Azure deployments)
- Azure Developer CLI (azd) - optional but recommended

## Environment Configuration

### Required Environment Variables

The following environment variables must be configured for production deployment:

#### Application Insights
```
APPLICATIONINSIGHTS_CONNECTION_STRING=<your-app-insights-connection-string>
```

#### Azure Service Bus (Event Bus)
```
ConnectionStrings__EventBus=<namespace>.servicebus.windows.net
```

#### Azure AD Authentication
```
AzureAd__Instance=https://login.microsoftonline.com/
AzureAd__TenantId=<your-tenant-id>
AzureAd__ClientId=<your-client-id>
AzureAd__ClientSecret=<your-client-secret>
```

#### Webhook Client Token
```
WebhookClientOptions__Token=<secure-random-guid>
```

#### Database Connections
- PostgreSQL connection strings are managed by .NET Aspire during deployment
- For Azure deployments, these are automatically configured

#### OpenAI (Optional)
```
ConnectionStrings__OpenAi=Endpoint=<your-openai-endpoint>;Key=<your-api-key>
```

### Configuration Files

Production-specific settings are stored in `appsettings.Production.json` files. Update the following:

1. **AllowedHosts**: Change from `"*"` to your specific domain(s)
   - Example: `"AllowedHosts": "www.yourshop.com,api.yourshop.com"`

2. **Logging Levels**: Production files use more restrictive logging:
   - Default: Warning
   - Errors: Error level only

## Secrets Management

**IMPORTANT**: Never commit secrets to source control!

### Recommended Approaches

#### Option 1: Azure Key Vault (Recommended for Azure)
```bash
# Store secrets in Azure Key Vault
az keyvault secret set --vault-name <vault-name> --name "AzureAd--ClientSecret" --value "<secret>"
az keyvault secret set --vault-name <vault-name> --name "WebhookToken" --value "<token>"
```

Configure your application to use Key Vault references in Azure App Service/Container Apps.

#### Option 2: Environment Variables
Set environment variables through your deployment platform:
- Azure Portal: Configuration → Application Settings
- Kubernetes: ConfigMaps and Secrets
- Docker Compose: Environment files (not checked into source control)

#### Option 3: User Secrets (Development Only)
```bash
dotnet user-secrets set "AzureAd:ClientSecret" "<secret>" --project src/eShop.AppHost
```

## Deployment Methods

### Method 1: Azure Developer CLI (Recommended)

1. **Install Azure Developer CLI**
   ```bash
   # Windows
   powershell -ex AllSigned -c "Invoke-RestMethod 'https://aka.ms/install-azd.ps1' | Invoke-Expression"
   
   # Linux/macOS
   curl -fsSL https://aka.ms/install-azd.sh | bash
   ```

2. **Login to Azure**
   ```bash
   azd auth login
   ```

3. **Initialize the project** (first time only)
   ```bash
   azd init
   ```
   - Select `Use code in the current directory`
   - Confirm `.NET (Aspire)`
   - Select services to expose (recommend exposing `webapp`)
   - Provide an environment name

4. **Deploy to Azure**
   ```bash
   azd up
   ```
   
   This command will:
   - Provision all required Azure resources
   - Build container images
   - Deploy the application
   - Output the webapp URL

5. **Update existing deployment**
   ```bash
   azd deploy
   ```

### Method 2: Manual Azure Deployment

1. **Create Azure Resources**
   - Azure Container Apps or Azure Kubernetes Service (AKS)
   - Azure Container Registry
   - Azure Database for PostgreSQL
   - Azure Redis Cache
   - Azure Service Bus
   - Azure Application Insights

2. **Build and Push Container Images**
   ```bash
   # Build images
   dotnet publish src/WebApp/WebApp.csproj -c Release
   dotnet publish src/Catalog.API/Catalog.API.csproj -c Release
   # ... repeat for all services
   
   # Tag and push to Azure Container Registry
   docker tag eshop/webapp:latest <your-acr>.azurecr.io/webapp:latest
   docker push <your-acr>.azurecr.io/webapp:latest
   ```

3. **Configure Container Apps**
   - Set environment variables for each service
   - Configure health check endpoints: `/health` and `/alive`
   - Set up ingress rules for public-facing services

### Method 3: Docker Compose (Testing/Staging)

1. **Create production docker-compose.yml** (example structure)
   ```yaml
   version: '3.8'
   services:
     webapp:
       image: eshop/webapp:latest
       environment:
         - ASPNETCORE_ENVIRONMENT=Production
         - ConnectionStrings__EventBus=${SERVICEBUS_CONNECTION}
       ports:
         - "8080:8080"
   ```

2. **Deploy using Docker Compose**
   ```bash
   docker-compose -f docker-compose.production.yml up -d
   ```

## Health Checks

Health check endpoints are enabled for all environments:

- **Liveness Probe**: `GET /alive` - Returns 200 if application is running
- **Readiness Probe**: `GET /health` - Returns 200 if application is ready to serve traffic

Configure your orchestrator to use these endpoints:

### Kubernetes
```yaml
livenessProbe:
  httpGet:
    path: /alive
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5
```

### Azure Container Apps
```bash
az containerapp create \
  --name webapp \
  --health-probe-path /health \
  --health-probe-type http
```

## Monitoring and Observability

### Application Insights
The application is configured to send telemetry to Azure Application Insights:
- Distributed tracing
- Custom metrics
- Log aggregation
- Performance monitoring

Access metrics through Azure Portal → Application Insights.

### OpenTelemetry
OpenTelemetry is configured for:
- Traces (HTTP requests, gRPC calls)
- Metrics (ASP.NET Core, HTTP client, runtime)
- Logs (structured logging with scopes)

## Security Checklist

Before deploying to production:

- [ ] Update `AllowedHosts` in production config files to specific domains
- [ ] Remove all placeholder secrets from configuration files
- [ ] Store secrets in Azure Key Vault or secure secret management system
- [ ] Configure Azure AD authentication with production tenant
- [ ] Enable HTTPS redirection (configured by default)
- [ ] Review HSTS settings (default: 30 days)
- [ ] Enable Application Insights for monitoring
- [ ] Configure proper CORS policies if needed
- [ ] Review and update session cookie lifetime (default: 60 minutes)
- [ ] Ensure health check endpoints are accessible to orchestrator
- [ ] Set appropriate logging levels (Warning/Error for production)

## Performance Considerations

- **Database**: Use appropriate PostgreSQL tier with pgvector support
- **Redis**: Use Azure Cache for Redis in production for better performance
- **Service Bus**: Configure appropriate message retention and partitioning
- **Container Resources**: Set appropriate CPU and memory limits
- **Scaling**: Configure auto-scaling based on CPU/memory/request metrics

## Rollback Procedures

### Azure Developer CLI
```bash
# View deployment history
az containerapp revision list --name <app-name> --resource-group <rg-name>

# Rollback to previous revision
az containerapp revision activate --revision <previous-revision>
```

### Container Orchestrators
```bash
# Kubernetes
kubectl rollout undo deployment/<deployment-name>

# Docker Compose
docker-compose -f docker-compose.production.yml down
docker-compose -f docker-compose.production.yml up -d --build
```

## Troubleshooting

### Common Issues

1. **Health checks failing**
   - Verify `/health` and `/alive` endpoints are accessible
   - Check application logs for startup errors
   - Ensure database migrations have completed

2. **Authentication errors**
   - Verify Azure AD configuration
   - Check client ID and tenant ID are correct
   - Ensure redirect URIs are configured in Azure AD

3. **Service Bus connection issues**
   - Verify connection string format
   - Check Service Bus namespace is accessible
   - Ensure managed identity or credentials are configured

4. **Database connection failures**
   - Verify PostgreSQL connection strings
   - Check firewall rules allow application access
   - Ensure pgvector extension is installed

### Logs and Diagnostics

```bash
# Azure CLI - View logs
az containerapp logs show --name <app-name> --resource-group <rg-name>

# Stream logs
az containerapp logs tail --name <app-name> --resource-group <rg-name>

# Kubernetes
kubectl logs <pod-name>
kubectl describe pod <pod-name>
```

## Support and Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)

## Maintenance

### Regular Tasks
- Monitor Application Insights for errors and performance issues
- Review and rotate secrets quarterly
- Update dependencies regularly for security patches
- Review and optimize database query performance
- Monitor resource utilization and adjust scaling rules

### Updates and Patches
```bash
# Update using azd
azd deploy

# Or rebuild and redeploy containers
docker build -t eshop/webapp:v2 .
docker push <registry>/webapp:v2
# Update deployment with new image
```
