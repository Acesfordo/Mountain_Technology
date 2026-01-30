# Environment Variables Reference

This document provides a complete reference of all environment variables used in the eShop application for production deployment.

## Core Infrastructure

### Application Insights (Monitoring)
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Yes | Azure Application Insights connection string for telemetry | `InstrumentationKey=xxx;IngestionEndpoint=https://...` |

### OpenTelemetry
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | OpenTelemetry Protocol (OTLP) endpoint for custom telemetry | `http://otel-collector:4317` |

## Authentication (Azure AD)

### All Services with Authentication
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `AzureAd__Instance` | Yes | Azure AD instance URL | `https://login.microsoftonline.com/` |
| `AzureAd__TenantId` | Yes | Azure AD tenant ID | `12345678-1234-1234-1234-123456789abc` |
| `AzureAd__ClientId` | Yes | Application (client) ID from Azure AD | `87654321-4321-4321-4321-abcdef123456` |
| `AzureAd__ClientSecret` | Yes* | Client secret from Azure AD (*for WebApp, WebhookClient) | `your-secret-value` |
| `AzureAd__Scopes` | Varies | OAuth scopes for the API | `basket`, `orders`, `webhooks` |

## Event Bus (Azure Service Bus)

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ConnectionStrings__EventBus` | Yes | Azure Service Bus namespace connection | `<namespace>.servicebus.windows.net` or connection string |
| `EventBus__SubscriptionClientName` | Yes | Unique name for service subscription | `Ordering`, `Basket`, `Catalog`, etc. |

## Databases (PostgreSQL)

Managed by .NET Aspire, but can be configured manually:

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ConnectionStrings__catalogdb` | Yes | Catalog database connection | Managed by Aspire |
| `ConnectionStrings__identitydb` | Yes | Identity database connection | Managed by Aspire |
| `ConnectionStrings__orderingdb` | Yes | Ordering database connection | Managed by Aspire |
| `ConnectionStrings__webhooksdb` | Yes | Webhooks database connection | Managed by Aspire |

## Redis Cache

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ConnectionStrings__Redis` | Yes | Redis connection string | Managed by Aspire or `redis-host:6379` |

## AI Features (Optional)

### OpenAI Integration
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ConnectionStrings__OpenAi` | No | OpenAI or Azure OpenAI connection | `Endpoint=https://...;Key=xxx` or `Key=sk-xxx` |
| `AI__OpenAI__ChatModel` | No | Chat model name for WebApp | `gpt-35-turbo-16k` |
| `AI__OpenAI__EmbeddingName` | No | Embedding model for Catalog | `text-embedding-3-small` |

## Service-Specific Variables

### WebApp
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `AllowedHosts` | Yes | Allowed host headers | `www.yourshop.com;api.yourshop.com` |
| `SessionCookieLifetimeMinutes` | No | Session timeout in minutes | `60` (default) |
| `CallBackUrl` | Yes | Self-referencing callback URL | Set automatically by Aspire |

### WebhookClient
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `WebhookClientOptions__Token` | Yes | Authentication token for webhooks | `<secure-random-guid>` |
| `CallBackUrl` | Yes | Self-referencing callback URL | Set automatically by Aspire |

### Basket.API
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `Identity__Audience` | Yes | Expected audience for JWT tokens | `https://your-api-domain/` |
| `Identity__Authority` | Yes | Azure AD authority URL with tenant | `https://login.microsoftonline.com/<tenant>/v2.0/` |

### OrderProcessor
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `BackgroundTaskOptions__GracePeriodTime` | No | Grace period in seconds | `1` |
| `BackgroundTaskOptions__CheckUpdateTime` | No | Check interval in seconds | `30` |

### PaymentProcessor
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `PaymentOptions__PaymentSucceeded` | No | Simulate payment success | `true` (default) |

### Catalog.API
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `CatalogOptions__UseCustomizationData` | No | Use custom seed data | `false` (default) |

### Webhooks.API
| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `UseCustomizationData` | No | Use custom seed data | `false` (default) |

## ASP.NET Core Standard Variables

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Yes | Environment name | `Production`, `Staging`, `Development` |
| `ASPNETCORE_URLS` | No | URLs to listen on | `http://+:8080` |
| `ASPNETCORE_HTTPS_PORT` | No | HTTPS port for redirects | `443` |

## Testing-Specific Variables

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `ESHOP_USE_HTTP_ENDPOINTS` | No | Force HTTP for testing | `1` (enable), `0` (disable) |

## Setting Environment Variables

### Azure Container Apps
```bash
az containerapp update \
  --name <app-name> \
  --resource-group <rg-name> \
  --set-env-vars \
  "APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-connection" \
  "AzureAd__ClientSecret=secretref:azuread-secret"
```

### Kubernetes
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: eshop-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  EventBus__SubscriptionClientName: "Ordering"
---
apiVersion: v1
kind: Secret
metadata:
  name: eshop-secrets
type: Opaque
stringData:
  AzureAd__ClientSecret: "your-secret"
  WebhookClientOptions__Token: "your-token"
```

### Docker Compose
```yaml
# .env file (DO NOT commit to source control)
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=xxx
AZUREAD_CLIENT_SECRET=your-secret
WEBHOOK_TOKEN=your-token
```

```yaml
# docker-compose.yml
services:
  webapp:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - APPLICATIONINSIGHTS_CONNECTION_STRING=${APPLICATIONINSIGHTS_CONNECTION_STRING}
      - AzureAd__ClientSecret=${AZUREAD_CLIENT_SECRET}
```

### Azure App Configuration (Advanced)
For centralized configuration management:
```bash
# Store configuration in Azure App Configuration
az appconfig kv set \
  --name <config-store-name> \
  --key "eShop:AzureAd:TenantId" \
  --value "<tenant-id>"

# Reference in application
# Uses Microsoft.Azure.AppConfiguration.AspNetCore NuGet package
```

## Security Best Practices

1. **Never commit secrets to source control**
   - Use `.env` files locally (add to `.gitignore`)
   - Use Azure Key Vault in production
   - Use Kubernetes Secrets for K8s deployments

2. **Use Managed Identities when possible**
   - Azure Container Apps and AKS support managed identities
   - Eliminates need for connection strings/secrets

3. **Rotate secrets regularly**
   - Schedule quarterly secret rotation
   - Use Azure Key Vault auto-rotation features

4. **Principle of Least Privilege**
   - Grant only necessary permissions
   - Use separate credentials per service

5. **Audit and Monitor**
   - Enable Azure AD audit logs
   - Monitor secret access in Key Vault
   - Review Application Insights for auth failures

## Validation Checklist

Before deploying to production, verify:

- [ ] All required variables are set
- [ ] No placeholder values (e.g., `<tenant id>`) remain
- [ ] Secrets are stored securely (Key Vault, not config files)
- [ ] AllowedHosts is restricted to actual domains
- [ ] ASPNETCORE_ENVIRONMENT is set to "Production"
- [ ] Connection strings use production resources
- [ ] Health check endpoints are accessible
- [ ] Logging levels are appropriate for production
- [ ] Azure AD apps are configured with correct redirect URIs

## Troubleshooting

### Missing Required Variable
**Symptom**: Application fails to start or throws configuration errors

**Solution**: Check logs for specific missing configuration. Add the variable using your deployment platform's method.

### Invalid Connection String
**Symptom**: Services can't connect to databases, Redis, or Service Bus

**Solution**: Verify connection string format and ensure resources are accessible from your deployment environment.

### Authentication Failures
**Symptom**: 401/403 errors, "invalid_client" messages

**Solution**: 
- Verify AzureAd__TenantId, ClientId, and ClientSecret match Azure AD app registration
- Check redirect URIs in Azure AD include your application URLs
- Ensure API scopes are properly configured

### Health Check Failures
**Symptom**: Container orchestrator reports unhealthy services

**Solution**: 
- Verify `/health` and `/alive` endpoints return 200
- Check dependencies (database, Redis) are accessible
- Review application logs for startup errors

## Additional Resources

- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/aspnet/core/security/key-vault-configuration)
- [Azure App Configuration](https://learn.microsoft.com/azure/azure-app-configuration/)
- [.NET Configuration](https://learn.microsoft.com/dotnet/core/extensions/configuration)
- [Azure Managed Identities](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
