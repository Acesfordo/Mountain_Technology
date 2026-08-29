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

Azure Key Vault provides secure, centralized secret storage with access control and audit logging.

**Step 1:** Store your sensitive configuration in Azure Key Vault
```bash
# Store the Azure AD client secret
# Replace <vault-name> with your Key Vault name (e.g., "eshop-keyvault-prod")
# Replace <secret> with your actual secret value
az keyvault secret set --vault-name <vault-name> --name "AzureAd--ClientSecret" --value "<secret>"

# Store the webhook authentication token
# Replace <token> with a secure random GUID or token
az keyvault secret set --vault-name <vault-name> --name "WebhookToken" --value "<token>"
```

**Step 2:** Configure your application to use Key Vault references

For Azure App Service or Container Apps, reference Key Vault secrets in your application settings:
- Go to Azure Portal → Your App Service/Container App → Configuration
- Add application setting: `AzureAd__ClientSecret = @Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/AzureAd--ClientSecret/)`
- The app will automatically retrieve secrets from Key Vault using its managed identity

#### Option 2: Environment Variables

Set environment variables through your deployment platform:
- **Azure Portal**: Navigate to Configuration → Application Settings
  1. Click "New application setting"
  2. Add name/value pairs for your secrets
  3. Mark as "Deployment slot setting" if needed
  4. Click "Save" to apply changes
  
- **Kubernetes**: Use ConfigMaps (non-sensitive) and Secrets (sensitive data)
  ```bash
  # Create a Kubernetes secret
  kubectl create secret generic eshop-secrets \
    --from-literal=AzureAd__ClientSecret='<your-secret>' \
    --from-literal=WebhookToken='<your-token>'
  ```
  
- **Docker Compose**: Use environment files (never commit these to source control!)
  ```bash
  # Create a .env file (add to .gitignore!)
  echo "AzureAd__ClientSecret=<your-secret>" > .env
  echo "WebhookToken=<your-token>" >> .env
  ```

#### Option 3: User Secrets (Development Only)

**⚠️ WARNING:** Only use this for local development. Never use for production!

```bash
# Store secrets securely on your local machine (not in source control)
# The secret is stored in your user profile directory
dotnet user-secrets set "AzureAd:ClientSecret" "<secret>" --project src/eShop.AppHost

# You can also use PowerShell to generate and store a random webhook token
# Example:
# $token = [System.Guid]::NewGuid().ToString()
# dotnet user-secrets set "WebhookToken" $token --project src/eShop.AppHost
```

These secrets are stored in:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

## Deployment Methods

### Method 1: Azure Developer CLI (Recommended)

The Azure Developer CLI (azd) simplifies deploying .NET Aspire applications to Azure by automating infrastructure provisioning and deployment.

#### 1. Install Azure Developer CLI

**For Windows (PowerShell):**
```powershell
# Downloads and executes the azd installer script
# The -ex AllSigned parameter allows running the digitally signed script
# The script is downloaded from the official Microsoft URL and executed immediately
powershell -ex AllSigned -c "Invoke-RestMethod 'https://aka.ms/install-azd.ps1' | Invoke-Expression"
```

**For Linux/macOS (Bash):**
```bash
# Downloads and executes the azd installer script using curl
# The -fsSL flags make curl silent and follow redirects
curl -fsSL https://aka.ms/install-azd.sh | bash
```

**Verify installation:**
```bash
# Check that azd is installed and view its version
azd version
```

#### 2. Login to Azure

```bash
# Opens a browser window for Azure authentication
# You'll sign in with your Microsoft account that has access to your Azure subscription
azd auth login
```

**What happens:**
- Browser opens with Azure login page
- You authenticate with your Azure credentials
- azd stores authentication tokens locally for future use

#### 3. Initialize the project (first time only)

```bash
# Detects your .NET Aspire project and configures Azure deployment settings
# This creates azd configuration files in your project
azd init
```

**During initialization, you'll be prompted for:**
- **"Use code in the current directory"** - Select this option
- **Confirm `.NET (Aspire)`** - azd auto-detects this, just confirm
- **Services to expose** - Select which services should have public endpoints (recommend: `webapp`)
- **Environment name** - Provide a name like "dev", "staging", or "prod"

**What gets created:**
- `.azure/` directory with environment-specific configuration
- `azure.yaml` file defining your deployment configuration

#### 4. Deploy to Azure

```bash
# Provisions Azure resources and deploys your application in one command
# This is the "magic command" that does everything
azd up
```

**This single command will:**
1. **Provision Azure resources:**
   - Azure Container Registry (ACR) for your Docker images
   - Azure Container Apps for running your microservices
   - Azure PostgreSQL for databases
   - Azure Redis for caching
   - Azure Service Bus for messaging
   - Azure Application Insights for monitoring

2. **Build and containerize:**
   - Builds your .NET projects
   - Creates Docker images for each service
   - Pushes images to Azure Container Registry

3. **Deploy:**
   - Deploys container images to Azure Container Apps
   - Configures networking and service discovery
   - Sets up environment variables and secrets

4. **Output:**
   - Displays the URL of your deployed webapp
   - Shows resource group and subscription information

**Expected output example:**
```
SUCCESS: Your application was provisioned and deployed to Azure in X minutes.
You can view the resources created under the resource group rg-eshop-dev in the Azure Portal.

Endpoint: https://webapp-abc123.azurecontainerapps.io
```

#### 5. Update existing deployment

When you've made code changes and want to redeploy:

```bash
# Rebuilds images and redeploys to existing Azure resources
# Much faster than 'azd up' because infrastructure already exists
azd deploy
```

**What this does:**
- Rebuilds only changed services
- Pushes updated images to ACR
- Updates Container Apps with new images
- Maintains existing infrastructure and configuration

**Useful azd commands:**
```bash
# View deployment status
azd show

# View application logs
azd monitor --logs

# Remove all Azure resources (cleanup)
azd down

# View environment configuration
azd env list
```

### Method 2: Manual Azure Deployment

For more control over the deployment process, you can manually provision Azure resources and deploy your application.

#### 1. Create Azure Resources

You'll need to create the following Azure resources (either via Azure Portal or Azure CLI):

**Required Resources:**
- **Azure Container Apps or Azure Kubernetes Service (AKS)** - For hosting microservices
- **Azure Container Registry (ACR)** - For storing Docker images
- **Azure Database for PostgreSQL** - For persistent data storage
- **Azure Redis Cache** - For session state and caching
- **Azure Service Bus** - For asynchronous messaging between services
- **Azure Application Insights** - For monitoring and telemetry

**Example using Azure CLI:**
```bash
# Set variables for your deployment
RESOURCE_GROUP="rg-eshop-prod"
LOCATION="eastus"
ACR_NAME="eshopacr"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Azure Container Registry
az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Standard

# Create PostgreSQL server
az postgres flexible-server create \
  --name eshop-db-prod \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user adminuser \
  --admin-password <secure-password> \
  --sku-name Standard_B2s \
  --tier Burstable
```

#### 2. Build and Push Container Images

**Step 1:** Build your .NET projects for production
```bash
# Build the WebApp service
# The -c Release flag builds an optimized production version
dotnet publish src/WebApp/WebApp.csproj -c Release -o ./publish/webapp

# Build the Catalog API service
dotnet publish src/Catalog.API/Catalog.API.csproj -c Release -o ./publish/catalog-api

# Build the Basket API service
dotnet publish src/Basket.API/Basket.API.csproj -c Release -o ./publish/basket-api

# Repeat for all other services:
# - Ordering.API
# - Mobile.Bff.Shopping
# - Webhooks.API
# - OrderProcessor
# - PaymentProcessor
```

**Step 2:** Build Docker images for each service
```bash
# Build Docker image for WebApp
# Uses the Dockerfile in the WebApp directory
docker build -f src/WebApp/Dockerfile -t eshop/webapp:latest .

# Build Docker image for Catalog API
docker build -f src/Catalog.API/Dockerfile -t eshop/catalog-api:latest .

# Build Docker image for Basket API
docker build -f src/Basket.API/Dockerfile -t eshop/basket-api:latest .

# Repeat for all other services
```

**Step 3:** Tag images for your Azure Container Registry
```bash
# Replace <your-acr> with your actual ACR name (e.g., eshopacr)
# Login to ACR first
az acr login --name <your-acr>

# Tag WebApp image
# This associates the local image with your ACR repository
docker tag eshop/webapp:latest <your-acr>.azurecr.io/webapp:latest
docker tag eshop/webapp:latest <your-acr>.azurecr.io/webapp:v1.0.0

# Tag Catalog API image
docker tag eshop/catalog-api:latest <your-acr>.azurecr.io/catalog-api:latest
docker tag eshop/catalog-api:latest <your-acr>.azurecr.io/catalog-api:v1.0.0

# Repeat for all services
```

**Step 4:** Push images to Azure Container Registry
```bash
# Push WebApp images to ACR
# Both the 'latest' tag and version tag
docker push <your-acr>.azurecr.io/webapp:latest
docker push <your-acr>.azurecr.io/webapp:v1.0.0

# Push Catalog API images to ACR
docker push <your-acr>.azurecr.io/catalog-api:latest
docker push <your-acr>.azurecr.io/catalog-api:v1.0.0

# Repeat for all services
```

**PowerShell script to automate image build and push:**
```powershell
# Define all services to build
$services = @("WebApp", "Catalog.API", "Basket.API", "Ordering.API", "Mobile.Bff.Shopping", "Webhooks.API")
$acrName = "<your-acr>"
$version = "v1.0.0"

# Login to ACR
az acr login --name $acrName

# Build, tag, and push each service
foreach ($service in $services) {
    $serviceName = $service.ToLower() -replace '\.', '-'
    
    Write-Host "Processing $service..." -ForegroundColor Cyan
    
    # Build Docker image
    docker build -f "src/$service/Dockerfile" -t "eshop/$serviceName:latest" .
    
    # Tag for ACR
    docker tag "eshop/$serviceName:latest" "$acrName.azurecr.io/$serviceName:latest"
    docker tag "eshop/$serviceName:latest" "$acrName.azurecr.io/$serviceName:$version"
    
    # Push to ACR
    docker push "$acrName.azurecr.io/$serviceName:latest"
    docker push "$acrName.azurecr.io/$serviceName:$version"
    
    Write-Host "✓ Completed $service" -ForegroundColor Green
}
```

#### 3. Configure Container Apps

**Set environment variables for each service:**
- Connection strings for PostgreSQL, Redis, and Service Bus
- Application Insights instrumentation key
- ASPNETCORE_ENVIRONMENT=Production

**Configure health check endpoints:**
- Primary health check: `/health` - Returns overall service health
- Liveness probe: `/alive` - Confirms the service is running

**Set up ingress rules:**
- Configure which services should be publicly accessible
- WebApp typically needs external ingress
- Internal services (APIs, processors) use internal ingress only

**Example Azure CLI command to create a Container App:**
```bash
az containerapp create \
  --name webapp \
  --resource-group $RESOURCE_GROUP \
  --environment <container-apps-environment-name> \
  --image $ACR_NAME.azurecr.io/webapp:latest \
  --target-port 8080 \
  --ingress external \
  --registry-server $ACR_NAME.azurecr.io \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ConnectionStrings__CatalogDB=<postgres-connection-string>" \
    "ApplicationInsights__ConnectionString=<app-insights-connection-string>"
```

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
