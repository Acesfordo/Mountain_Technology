# Deployment Runbook

## Overview

This runbook provides step-by-step procedures for deploying the eShop application to production and managing deployments.

**Last Updated:** October 23, 2025  
**Version:** 1.0

---

## Table of Contents

1. [Pre-Deployment Checklist](#pre-deployment-checklist)
2. [Environment Setup](#environment-setup)
3. [Deployment Procedures](#deployment-procedures)
4. [Post-Deployment Verification](#post-deployment-verification)
5. [Rollback Procedures](#rollback-procedures)
6. [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)
7. [Emergency Contacts](#emergency-contacts)

---

## Pre-Deployment Checklist

### Code Quality

- [ ] All unit tests pass
- [ ] Code review completed and approved
- [ ] No critical security vulnerabilities
- [ ] All merge conflicts resolved
- [ ] Code follows established standards
- [ ] Documentation updated

### Dependencies

- [ ] All NuGet packages updated to stable versions
- [ ] npm packages audited and vulnerabilities fixed
- [ ] Dependency conflicts resolved
- [ ] Third-party service dependencies verified

### Configuration

- [ ] Environment-specific configurations prepared
- [ ] Secrets stored in Azure Key Vault
- [ ] Connection strings validated
- [ ] Feature flags configured
- [ ] API keys and certificates ready

### Infrastructure

- [ ] Azure resources provisioned
- [ ] Database migrations prepared
- [ ] Container registry accessible
- [ ] Network security groups configured
- [ ] Load balancers configured
- [ ] DNS records configured

### Monitoring

- [ ] Application Insights configured
- [ ] Alerting rules set up
- [ ] Log Analytics workspace ready
- [ ] Dashboard configured

### Backup

- [ ] Database backup completed
- [ ] Configuration backup saved
- [ ] Previous deployment artifacts archived

---

## Environment Setup

### Development Environment

**Purpose:** Local development and testing

**Components:**
- Docker Desktop for local container orchestration
- .NET 8 SDK
- Aspire workload
- PostgreSQL (via Docker)
- Redis (via Docker)
- RabbitMQ or Azure Service Bus emulator

**Setup Commands:**
```bash
# Install Aspire workload
dotnet workload install aspire

# Restore dependencies
dotnet restore eShop.Web.slnf

# Run the application
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

### Staging Environment

**Purpose:** Pre-production testing

**URL:** `https://eshop-staging.azurewebsites.net`

**Azure Resources:**
- Resource Group: `rg-eshop-staging`
- Container Apps Environment: `cae-eshop-staging`
- PostgreSQL Server: `psql-eshop-staging`
- Redis Cache: `redis-eshop-staging`
- Service Bus Namespace: `sb-eshop-staging`
- Application Insights: `ai-eshop-staging`

### Production Environment

**Purpose:** Live production system

**URL:** `https://eshop.azurewebsites.net`

**Azure Resources:**
- Resource Group: `rg-eshop-prod`
- Container Apps Environment: `cae-eshop-prod`
- PostgreSQL Server: `psql-eshop-prod`
- Redis Cache: `redis-eshop-prod`
- Service Bus Namespace: `sb-eshop-prod`
- Application Insights: `ai-eshop-prod`
- Azure Front Door: `afd-eshop-prod`

---

## Deployment Procedures

### Option 1: Azure Developer CLI (Recommended)

#### Initial Deployment

1. **Install Azure Developer CLI**
   ```bash
   # Windows (PowerShell)
   winget install microsoft.azd

   # macOS/Linux
   curl -fsSL https://aka.ms/install-azd.sh | bash
   ```

2. **Authenticate to Azure**
   ```bash
   azd auth login
   ```

3. **Initialize the Project**
   ```bash
   azd init
   ```
   - Select: `Use code in the current directory`
   - Confirm: `.NET (Aspire)`
   - Select services to expose
   - Name your environment (e.g., `prod`)

4. **Deploy to Azure**
   ```bash
   azd up
   ```
   
   This command will:
   - Provision Azure resources
   - Build container images
   - Deploy all services
   - Configure networking
   - Set up monitoring

   **Expected Duration:** 15-20 minutes

5. **Note the Output**
   - Save the endpoint URLs
   - Save resource names
   - Save connection strings

#### Subsequent Deployments

```bash
# Deploy updates
azd deploy

# Deploy specific service
azd deploy catalog-api

# Provision infrastructure only
azd provision

# Deploy code only (skip infrastructure)
azd deploy --no-provision
```

### Option 2: Manual Azure Container Apps Deployment

#### 1. Build Container Images

```bash
# Navigate to project root
cd /path/to/Mountain_Technology

# Build images for each service
docker build -t eshop/catalog-api:v1.0.0 -f src/Catalog.API/Dockerfile .
docker build -t eshop/basket-api:v1.0.0 -f src/Basket.API/Dockerfile .
docker build -t eshop/ordering-api:v1.0.0 -f src/Ordering.API/Dockerfile .
docker build -t eshop/webhooks-api:v1.0.0 -f src/Webhooks.API/Dockerfile .
docker build -t eshop/webapp:v1.0.0 -f src/WebApp/Dockerfile .
docker build -t eshop/mobile-bff:v1.0.0 -f src/Mobile.Bff.Shopping/Dockerfile .
```

#### 2. Push Images to Azure Container Registry

```bash
# Login to ACR
az acr login --name <your-acr-name>

# Tag images
docker tag eshop/catalog-api:v1.0.0 <your-acr-name>.azurecr.io/eshop/catalog-api:v1.0.0
# ... repeat for all images

# Push images
docker push <your-acr-name>.azurecr.io/eshop/catalog-api:v1.0.0
# ... repeat for all images
```

#### 3. Deploy to Container Apps

```bash
# Deploy Catalog API
az containerapp update \
  --name catalog-api \
  --resource-group rg-eshop-prod \
  --image <your-acr-name>.azurecr.io/eshop/catalog-api:v1.0.0

# Repeat for all services
```

#### 4. Run Database Migrations

```bash
# Connect to the database
az postgres flexible-server connect \
  --name psql-eshop-prod \
  --database-name catalogdb \
  --admin-user <admin-user>

# Run migrations (from application)
# or use EF Core CLI tools
```

### Option 3: CI/CD with GitHub Actions

#### Prerequisites

- GitHub repository with code
- Azure service principal with appropriate permissions
- GitHub secrets configured

#### Setup Secrets

In GitHub repository settings, add:
- `AZURE_CREDENTIALS` - Service principal JSON
- `AZURE_SUBSCRIPTION_ID` - Azure subscription ID
- `ACR_NAME` - Container registry name
- `ACR_USERNAME` - Registry username
- `ACR_PASSWORD` - Registry password

#### Workflow File

Create `.github/workflows/deploy-production.yml`:

```yaml
name: Deploy to Production

on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Login to Azure
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Install Azure Developer CLI
        run: curl -fsSL https://aka.ms/install-azd.sh | bash
      
      - name: Azure Dev Deploy
        run: azd deploy
        env:
          AZURE_ENV_NAME: prod
```

---

## Post-Deployment Verification

### 1. Health Checks

```bash
# Check all service health endpoints
curl https://eshop.azurewebsites.net/health
curl https://eshop.azurewebsites.net/catalog-api/health
curl https://eshop.azurewebsites.net/basket-api/health
curl https://eshop.azurewebsites.net/ordering-api/health
```

**Expected Response:** `200 OK` with health status

### 2. Smoke Tests

Run these basic tests to verify core functionality:

```bash
# Test catalog listing
curl https://eshop.azurewebsites.net/api/v1/catalog/items?pageSize=5

# Test health of all services
for service in webapp catalog-api basket-api ordering-api webhooks-api; do
  echo "Testing $service..."
  curl -f https://eshop.azurewebsites.net/$service/health || echo "$service FAILED"
done
```

### 3. Application Insights Verification

1. Open Azure Portal
2. Navigate to Application Insights resource
3. Check:
   - Live Metrics Stream (should show traffic)
   - Request rate (should be > 0)
   - Failure rate (should be < 1%)
   - Response times (should be reasonable)

### 4. Database Verification

```bash
# Connect to database and verify
az postgres flexible-server execute \
  --name psql-eshop-prod \
  --database-name catalogdb \
  --admin-user <admin-user> \
  --querytext "SELECT COUNT(*) FROM catalog_items;"
```

### 5. End-to-End Test

1. Open the web application
2. Browse catalog
3. Add item to basket
4. Complete checkout (test order)
5. Verify order appears in order history

### 6. Performance Baseline

```bash
# Run load test (if available)
# Verify response times are within acceptable range
```

---

## Rollback Procedures

### When to Rollback

Initiate rollback if:
- Critical functionality is broken
- Error rate exceeds 5%
- Performance degrades by > 50%
- Security vulnerability introduced
- Data corruption detected

### Rollback Methods

#### Method 1: Azure Developer CLI Rollback

```bash
# List previous deployments
az deployment group list \
  --resource-group rg-eshop-prod \
  --query "[].{name:name, timestamp:properties.timestamp}" \
  --output table

# Rollback to previous deployment
azd deploy --from-deployment <previous-deployment-id>
```

#### Method 2: Container Image Rollback

```bash
# Rollback to previous image version
az containerapp update \
  --name catalog-api \
  --resource-group rg-eshop-prod \
  --image <your-acr-name>.azurecr.io/eshop/catalog-api:v0.9.9

# Repeat for all affected services
```

#### Method 3: Traffic Split Rollback

If using traffic splitting:

```bash
# Route 100% traffic back to old revision
az containerapp ingress traffic set \
  --name catalog-api \
  --resource-group rg-eshop-prod \
  --revision-weight latest=0 previous=100
```

### Database Rollback

⚠️ **WARNING:** Database rollbacks are complex and may result in data loss.

1. **Stop all application instances**
   ```bash
   az containerapp scale --name <app-name> --min-replicas 0 --max-replicas 0
   ```

2. **Restore database from backup**
   ```bash
   az postgres flexible-server restore \
     --resource-group rg-eshop-prod \
     --name psql-eshop-prod \
     --restore-time <timestamp> \
     --target-server psql-eshop-prod-restored
   ```

3. **Verify restored data**

4. **Update connection strings** to point to restored database

5. **Restart application instances**

### Post-Rollback Actions

- [ ] Document rollback reason
- [ ] Notify stakeholders
- [ ] Update incident log
- [ ] Schedule post-mortem
- [ ] Create fix plan
- [ ] Test fix in staging

---

## Monitoring and Troubleshooting

### Monitoring Dashboards

**Application Insights Dashboard:**
https://portal.azure.com/#blade/Microsoft_Azure_Monitoring/AzureMonitoringBrowseBlade

**Key Metrics to Monitor:**
- Request rate (requests/sec)
- Response time (ms)
- Failed requests (count and %)
- Exceptions (count)
- Dependency failures
- CPU and memory usage
- Database connections

### Log Analysis

#### View Application Logs

```bash
# Stream logs from a specific service
az containerapp logs show \
  --name catalog-api \
  --resource-group rg-eshop-prod \
  --follow

# Query logs with KQL
az monitor log-analytics query \
  --workspace <workspace-id> \
  --analytics-query "ContainerAppConsoleLogs_CL | where ContainerAppName_s == 'catalog-api' | order by TimeGenerated desc | take 100"
```

#### Common Issues and Solutions

##### Issue: Service Not Starting

**Symptoms:** Container restarts repeatedly

**Troubleshooting:**
```bash
# Check container logs
az containerapp logs show --name <service-name> --resource-group <rg-name>

# Check environment variables
az containerapp show --name <service-name> --resource-group <rg-name>

# Check resource limits
az containerapp show --name <service-name> --resource-group <rg-name> --query "properties.template.containers[0].resources"
```

**Common Causes:**
- Missing environment variables
- Database connection issues
- Insufficient memory
- Invalid configuration

##### Issue: High Response Times

**Symptoms:** Slow API responses

**Troubleshooting:**
1. Check Application Insights performance metrics
2. Review slow database queries
3. Check cache hit rate
4. Monitor dependency call times

**Solutions:**
- Scale up resources
- Optimize database queries
- Implement caching
- Enable CDN for static content

##### Issue: Database Connection Failures

**Symptoms:** 500 errors, connection timeout exceptions

**Troubleshooting:**
```bash
# Test database connectivity
az postgres flexible-server execute \
  --name psql-eshop-prod \
  --admin-user <admin> \
  --querytext "SELECT 1;"

# Check firewall rules
az postgres flexible-server firewall-rule list \
  --resource-group rg-eshop-prod \
  --server-name psql-eshop-prod
```

**Solutions:**
- Verify connection string
- Check firewall rules
- Increase connection pool size
- Verify credentials in Key Vault

### Alerting

#### Critical Alerts (Immediate Response Required)

- Service availability < 99%
- Error rate > 5%
- Response time > 5 seconds (P95)
- Database connections exhausted

#### Warning Alerts (Response Within 1 Hour)

- CPU usage > 80%
- Memory usage > 85%
- Disk space < 20%
- Error rate > 1%

#### Information Alerts

- Deployment completed
- Scale operation completed
- Backup completed

---

## Emergency Contacts

### Escalation Path

**Level 1 - On-Call Engineer**
- Response Time: 15 minutes
- Contact: [Contact details]

**Level 2 - Lead Engineer**
- Response Time: 30 minutes
- Contact: [Contact details]

**Level 3 - Engineering Manager**
- Response Time: 1 hour
- Contact: [Contact details]

### External Contacts

**Azure Support**
- Portal: https://portal.azure.com
- Phone: [Azure support number]
- Priority: Based on support plan

**Third-Party Services**
- Service Bus Support
- Database Support

---

## Appendix

### A. Environment Variables

| Variable | Description | Dev | Staging | Prod |
|----------|-------------|-----|---------|------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Development | Staging | Production |
| `ConnectionStrings__CatalogDB` | Catalog database | Local | Azure PostgreSQL | Azure PostgreSQL |
| `ConnectionStrings__OrderingDB` | Ordering database | Local | Azure PostgreSQL | Azure PostgreSQL |
| `ConnectionStrings__Redis` | Redis cache | Local | Azure Redis | Azure Redis |
| `ConnectionStrings__ServiceBus` | Message bus | RabbitMQ | Azure Service Bus | Azure Service Bus |

### B. Resource Naming Convention

Pattern: `<resource-type>-<app-name>-<environment>`

Examples:
- `ca-catalog-api-prod` - Container App
- `psql-eshop-prod` - PostgreSQL Server
- `redis-eshop-prod` - Redis Cache
- `rg-eshop-prod` - Resource Group

### C. Version History

| Version | Date | Changes | Deployed By |
|---------|------|---------|-------------|
| 1.0.0 | 2025-10-23 | Initial deployment | - |

---

**Document Owner:** DevOps Team  
**Review Cycle:** Quarterly  
**Next Review:** January 2026
