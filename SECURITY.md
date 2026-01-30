# Security Best Practices

This document outlines security best practices for deploying and maintaining the eShop application in production.

## Secrets Management

### ✅ DO
- **Use Azure Key Vault** for storing production secrets
- **Use Managed Identities** to access Key Vault without credentials
- **Rotate secrets regularly** (quarterly minimum)
- **Use different secrets** for each environment (dev, staging, prod)
- **Audit secret access** through Azure Monitor
- **Set expiration dates** on secrets in Key Vault

### ❌ DON'T
- **Never commit secrets** to source control
- **Don't use placeholder values** in production (`<tenant id>`, etc.)
- **Don't share secrets** via email, chat, or insecure channels
- **Don't reuse secrets** across environments
- **Don't store secrets** in configuration files that get deployed

### Implementation

#### Azure Key Vault Integration
```bash
# Create Key Vault
az keyvault create \
  --name eshop-keyvault \
  --resource-group eshop-rg \
  --location eastus

# Store secrets
az keyvault secret set --vault-name eshop-keyvault --name "AzureAd-ClientSecret" --value "<secret>"
az keyvault secret set --vault-name eshop-keyvault --name "WebhookToken" --value "<token>"

# Grant access to managed identity
az keyvault set-policy \
  --name eshop-keyvault \
  --object-id <managed-identity-principal-id> \
  --secret-permissions get list
```

#### Application Configuration
Add to your application startup:
```csharp
// In Program.cs or Startup
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

## Authentication and Authorization

### Azure AD Configuration

1. **Register separate applications** for each environment
2. **Use least privilege principle** for API permissions
3. **Configure appropriate token lifetimes** (default: 60 minutes)
4. **Enable MFA** for administrative access
5. **Review app registrations regularly** for unused permissions

### JWT Token Validation

Ensure proper validation in APIs:
```csharp
// Already configured in Basket.API
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Identity:Authority"];
        options.Audience = configuration["Identity:Audience"];
        options.RequireHttpsMetadata = true;
    });
```

### API Scopes

- Define **granular scopes** for each API (basket, orders, webhooks)
- **Validate scopes** in API endpoints
- **Document required scopes** for each endpoint

## Network Security

### Allowed Hosts Configuration

**Update `appsettings.Production.json`** with specific domains:
```json
{
  "AllowedHosts": "www.eshop.com;api.eshop.com"
}
```

**Why**: Prevents host header injection attacks

### HTTPS Configuration

✅ HTTPS is enforced by default via `app.UseHttpsRedirection()`

**Configure HSTS** for production:
```csharp
// Already in WebApp/Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts(); // Default: 30 days
}
```

**Consider increasing HSTS max-age** for mature deployments:
```csharp
app.UseHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});
```

### CORS Configuration

If your APIs are accessed from web browsers, configure CORS explicitly:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins("https://www.eshop.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("ProductionPolicy");
```

**Current Status**: CORS not explicitly configured; uses default restrictions

## Data Protection

### Database Security

1. **Use SSL/TLS** for database connections
2. **Enable encryption at rest** (Azure PostgreSQL supports this)
3. **Implement column-level encryption** for sensitive data
4. **Use separate credentials** per service
5. **Enable Azure AD authentication** for PostgreSQL

### Redis Cache Security

1. **Enable SSL/TLS** for Redis connections
2. **Use access keys rotation** in Azure Redis Cache
3. **Limit network access** via virtual networks or firewall rules

### Personal Data (GDPR Compliance)

- **Implement data retention policies**
- **Provide data export capabilities**
- **Implement right to deletion**
- **Log consent for data processing**
- **Encrypt PII** (Personally Identifiable Information)

## Input Validation and Sanitization

### Anti-forgery Protection

✅ Already enabled: `app.UseAntiforgery()`

### Model Validation

Ensure all DTOs have validation attributes:
```csharp
public class BasketItem
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, 100)]
    public int Quantity { get; set; }
}
```

### SQL Injection Prevention

✅ Using Entity Framework Core prevents SQL injection
- All queries use parameterization
- LINQ queries are translated safely

### XSS Prevention

✅ Razor Components automatically encode output
- Manual encoding: `@System.Net.WebUtility.HtmlEncode(userInput)`

## Logging and Monitoring

### Security Event Logging

Log security-relevant events:
- Authentication failures
- Authorization denials
- Suspicious activity patterns
- Configuration changes
- Secret access

### Sensitive Data in Logs

❌ **Never log**:
- Passwords or secrets
- Full credit card numbers
- Social security numbers
- JWT tokens

✅ **Log safely**:
- User IDs (not usernames if PII)
- Request IDs for correlation
- Sanitized error messages

### Example
```csharp
// Bad
logger.LogInformation("User {username} logged in with password {password}", username, password);

// Good
logger.LogInformation("User {userId} authenticated successfully", userId);
```

## Dependency Management

### Keep Dependencies Updated

```bash
# Check for outdated packages
dotnet list package --outdated

# Update packages
dotnet add package <PackageName> --version <LatestVersion>
```

### Vulnerability Scanning

- **Enable Dependabot** in GitHub (already configured)
- **Review security advisories** regularly
- **Update vulnerable dependencies** promptly
- **Subscribe to security mailing lists** for .NET, Aspire, and other frameworks

### NuGet Package Security

- **Use trusted package sources** only
- **Verify package signatures**
- **Review package dependencies** before adding

## Rate Limiting and DDoS Protection

### Implement Rate Limiting

Consider adding rate limiting middleware:

```bash
# Add package
dotnet add package Microsoft.AspNetCore.RateLimiting
```

```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

app.UseRateLimiter();
```

**Status**: Not currently implemented (recommended for production)

### Azure Front Door / Application Gateway

For production deployments, consider:
- **Azure Front Door** for global load balancing and WAF
- **Azure Application Gateway** for regional deployments
- **Azure DDoS Protection Standard** for critical workloads

## Container Security

### Base Image Security

✅ Using official Microsoft images
- Regularly updated with security patches
- Minimal attack surface

### Image Scanning

```bash
# Scan with Trivy (example)
trivy image eshop/webapp:latest

# Scan with Azure Container Registry
az acr task create \
  --registry <registry-name> \
  --name security-scan \
  --image eshop/webapp:latest \
  --cmd "trivy image --severity HIGH,CRITICAL $IMAGE"
```

### Runtime Security

- **Run as non-root user** in containers
- **Use read-only file systems** where possible
- **Limit container capabilities**
- **Implement network policies** in Kubernetes

## Health Checks Security

✅ Health check endpoints now enabled in production: `/health` and `/alive`

**Consideration**: Health checks may expose internal information
- Currently return basic status only
- No sensitive information exposed
- Consider authentication for detailed health checks

## Incident Response

### Security Incident Plan

1. **Detection**: Monitor Application Insights for anomalies
2. **Containment**: Disable compromised credentials immediately
3. **Investigation**: Review logs and audit trails
4. **Recovery**: Rotate secrets, patch vulnerabilities
5. **Post-mortem**: Document and improve processes

### Emergency Contacts

Document and maintain:
- Security team contacts
- Azure support channels
- Escalation procedures
- Communication templates

## Compliance Checklist

Before production deployment:

- [ ] All secrets stored in Azure Key Vault
- [ ] Managed identities configured for Azure resources
- [ ] AllowedHosts restricted to production domains
- [ ] HTTPS enforced on all endpoints
- [ ] Health checks configured correctly
- [ ] Logging excludes sensitive information
- [ ] Dependencies scanned for vulnerabilities
- [ ] Azure AD configured with least privilege
- [ ] Rate limiting implemented (if public-facing)
- [ ] Security monitoring enabled in Application Insights
- [ ] Incident response plan documented
- [ ] Regular security reviews scheduled

## Security Contacts and Resources

### Microsoft Security Resources
- [Azure Security Center](https://azure.microsoft.com/services/security-center/)
- [Microsoft Security Response Center](https://www.microsoft.com/msrc)
- [.NET Security Announcements](https://github.com/dotnet/announcements/labels/security)

### Reporting Security Issues

**For eShop application issues**:
- Create a private security advisory in GitHub
- Email security team (configure contact)

**For .NET/Azure issues**:
- Report to Microsoft Security Response Center: secure@microsoft.com

## Regular Security Tasks

### Daily
- Monitor Application Insights for anomalies
- Review authentication failures

### Weekly
- Review security alerts from Dependabot
- Check Azure Security Center recommendations

### Monthly
- Review user access and permissions
- Audit Key Vault access logs
- Review and update security documentation

### Quarterly
- Rotate secrets and certificates
- Security training for team
- Penetration testing or security audit
- Review and update incident response plan

## Additional Security Enhancements (Future)

Consider implementing:
1. **API Gateway** (Azure API Management) for centralized security
2. **Web Application Firewall** (WAF) via Azure Front Door
3. **Content Security Policy** (CSP) headers
4. **Subresource Integrity** (SRI) for CDN resources
5. **Certificate pinning** for sensitive communications
6. **Anomaly detection** via Azure Sentinel
7. **Penetration testing** schedule
8. **Bug bounty program** for mature deployments

## Conclusion

Security is an ongoing process, not a one-time task. Regularly review and update security practices as threats evolve and new features are added to the application.

For questions or security concerns, refer to the incident response plan and escalate appropriately.
