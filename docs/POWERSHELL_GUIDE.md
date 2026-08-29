# PowerShell Best Practices Guide for eShop Contributors

This guide provides best practices and conventions for writing PowerShell scripts and commands in the eShop project. Following these guidelines ensures consistency, clarity, and maintainability across our codebase.

## Table of Contents

- [Script Structure](#script-structure)
- [Documentation](#documentation)
- [Naming Conventions](#naming-conventions)
- [Code Style](#code-style)
- [Error Handling](#error-handling)
- [Security Practices](#security-practices)
- [Common Patterns](#common-patterns)

---

## Script Structure

### Header Comments

Every PowerShell script should begin with comprehensive header comments using the PowerShell help format:

```powershell
<#
.SYNOPSIS
    Brief one-line description of what the script does.

.DESCRIPTION
    Detailed explanation of the script's purpose and functionality.
    Include what problem it solves and when to use it.

.PARAMETER parameterName
    Description of what this parameter does and valid values.

.EXAMPLE
    .\script.ps1 -ParameterName "value"
    
    Description of what this example demonstrates.

.NOTES
    Prerequisites:
    - List any required tools or permissions
    - Note any dependencies
    
    Author: Your Name/Team Name
    Last Modified: YYYY-MM-DD
#>
```

### Script Organization

Organize your script in this order:

1. **Header comments** (synopsis, description, parameters, examples)
2. **Parameter declarations**
3. **Script-level variables and constants**
4. **Helper functions** (if any)
5. **Main script logic**
6. **Cleanup** (if needed)

**Example:**

```powershell
<#
.SYNOPSIS
    Deploys eShop microservices to Azure Container Apps.
#>

Param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Development"
)

# Constants
$ACR_NAME = "eshopacr"
$LOCATION = "eastus"

# Helper function
function Write-StepHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

# Main script logic
Write-StepHeader "Starting Deployment"

# ... rest of script
```

---

## Documentation

### Inline Comments

Use inline comments to explain **why**, not **what**. The code itself should be clear about what it does.

**❌ Bad:**
```powershell
# Set the name
$name = "eShop"
```

**✅ Good:**
```powershell
# Use lowercase for ACR compatibility (ACR names must be lowercase)
$acrName = "eshop".ToLower()
```

### Comment Blocks for Sections

Use comment blocks to separate major sections of your script:

```powershell
#============================================
# STEP 1: VALIDATE PREREQUISITES
#============================================

# Validation logic here...

#============================================
# STEP 2: PROVISION RESOURCES
#============================================

# Provisioning logic here...
```

### Explain Complex Logic

If a section is complex or non-obvious, add explanatory comments:

```powershell
# Build the connection string dynamically to support both local and Azure environments
# Local: Uses localhost with default credentials
# Azure: Uses FQDN with managed identity or connection string from Key Vault
if ($Environment -eq "Development") {
    $connectionString = "Host=localhost;Database=$dbName;Username=postgres;******"
} else {
    $connectionString = "Host=$($serverName).postgres.database.azure.com;Database=$dbName;Username=$adminUser"
}
```

---

## Naming Conventions

### Variables

- Use **PascalCase** for script-level variables and constants: `$ResourceGroupName`, `$AcrName`
- Use **camelCase** for local variables within functions: `$serviceName`, `$imageTag`
- Use **UPPER_CASE** for true constants: `$MAX_RETRIES`, `$DEFAULT_LOCATION`

```powershell
# Script-level variables
$ResourceGroupName = "rg-eshop-prod"
$EnvironmentName = "Production"

# Constants
$MAX_RETRY_COUNT = 3
$DEFAULT_REGION = "eastus"

# Local variables in loops
foreach ($service in $services) {
    $imageName = $service.Name.ToLower()
    $containerPort = $service.Port
}
```

### Functions

- Use **Verb-Noun** format following PowerShell conventions
- Use approved verbs: `Get-`, `Set-`, `New-`, `Remove-`, `Test-`, `Start-`, `Stop-`, etc.

```powershell
function Get-ServiceConfiguration { }
function Test-AzureConnection { }
function New-ContainerApp { }
function Deploy-Microservice { }  # ❌ "Deploy" is not an approved verb
function Publish-Microservice { }  # ✅ Use "Publish" instead
```

View approved verbs with: `Get-Verb`

---

## Code Style

### Indentation and Spacing

- Use **4 spaces** for indentation (not tabs)
- Add blank lines between logical sections
- Use spacing around operators for readability

```powershell
# Good spacing
if ($condition -eq $true) {
    $result = $value1 + $value2
    Write-Host "Result: $result"
}

# Not recommended
if($condition -eq $true){
$result=$value1+$value2
Write-Host "Result: $result"
}
```

### Line Continuation

For long commands, use backticks or splatting:

**Using backticks:**
```powershell
az containerapp create `
    --name "webapp" `
    --resource-group $ResourceGroup `
    --environment $Environment `
    --image "$AcrName.azurecr.io/webapp:latest"
```

**Using splatting (preferred for complex cmdlets):**
```powershell
$params = @{
    Name              = "webapp"
    ResourceGroupName = $ResourceGroup
    Environment       = $Environment
    Image             = "$AcrName.azurecr.io/webapp:latest"
    TargetPort        = 8080
    IngressExternal   = $true
}

New-ContainerApp @params
```

### String Formatting

Use string interpolation with double quotes when including variables:

```powershell
# String interpolation
$message = "Deploying $serviceName to $environment environment"

# For single quotes (literal strings)
$regex = '^[a-z0-9]+$'

# For complex expressions, use subexpressions
$status = "Service $($service.Name) is $($service.Status.ToUpper())"
```

---

## Error Handling

### Set Error Preferences

Always set error preferences at the start of production scripts:

```powershell
# Stop execution on any error
$ErrorActionPreference = "Stop"

# For scripts that should continue on errors (with logging)
$ErrorActionPreference = "Continue"
```

### Try-Catch Blocks

Use try-catch for operations that might fail:

```powershell
try {
    Write-Host "Deploying service: $serviceName" -ForegroundColor Cyan
    
    az containerapp create `
        --name $serviceName `
        --resource-group $ResourceGroup `
        --image "$AcrName.azurecr.io/$serviceName:latest"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Deployment failed for $serviceName"
    }
    
    Write-Host "✓ Successfully deployed $serviceName" -ForegroundColor Green
    
} catch {
    Write-Host "✗ Error deploying $serviceName" -ForegroundColor Red
    Write-Host "Error details: $_" -ForegroundColor Red
    
    # Decide whether to continue or exit
    if ($StopOnError) {
        exit 1
    }
}
```

### Check Command Results

Always check the result of external commands:

```powershell
# For Azure CLI commands
az group create --name $resourceGroup --location $location

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create resource group"
    exit 1
}

# For PowerShell cmdlets that return objects
$result = Test-Connection -ComputerName $server -Count 1 -Quiet

if (-not $result) {
    Write-Error "Server $server is not reachable"
    exit 1
}
```

---

## Security Practices

### Never Hardcode Secrets

**❌ Never do this:**
```powershell
$password = "MySecretPassword123!"
$connectionString = "Server=myserver;******;"
```

**✅ Instead, use:**

```powershell
# Read from environment variables
$password = $env:DB_PASSWORD

# Read from Azure Key Vault
$secret = az keyvault secret show --vault-name $vaultName --name "db-password" --query value -o tsv

# Prompt user securely
$credential = Get-Credential -Message "Enter database credentials"
$password = $credential.GetNetworkCredential().Password

# Use user secrets for development
dotnet user-secrets set "ConnectionStrings:Default" $connectionString --project $projectPath
```

### Secure String Handling

Use `SecureString` for sensitive data:

```powershell
# Convert to secure string
$securePassword = ConvertTo-SecureString $password -AsPlainText -Force

# Create credential object
$credential = New-Object System.Management.Automation.PSCredential($username, $securePassword)

# Clear sensitive variables when done
$password = $null
$securePassword = $null
```

### Validate Input

Always validate parameters and user input:

```powershell
Param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-z0-9]+$')]
    [string]$AcrName,
    
    [Parameter(Mandatory=$false)]
    [ValidateRange(1, 10)]
    [int]$Replicas = 3
)

# Additional validation
if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
    throw "Resource group name cannot be empty"
}

if (-not (Test-Path $configFile)) {
    throw "Configuration file not found: $configFile"
}
```

---

## Common Patterns

### Progress Indication

Provide clear feedback to users:

```powershell
Write-Host "Starting deployment process..." -ForegroundColor Yellow

$services = @("catalog-api", "basket-api", "ordering-api")
$current = 0
$total = $services.Count

foreach ($service in $services) {
    $current++
    $percent = [math]::Round(($current / $total) * 100)
    
    Write-Progress -Activity "Deploying Services" `
                   -Status "Deploying $service ($current of $total)" `
                   -PercentComplete $percent
    
    # Deployment logic here...
    Start-Sleep -Seconds 2
}

Write-Progress -Activity "Deploying Services" -Completed
Write-Host "✓ All services deployed successfully!" -ForegroundColor Green
```

### Retry Logic

Implement retries for unreliable operations:

```powershell
function Invoke-WithRetry {
    param(
        [scriptblock]$ScriptBlock,
        [int]$MaxRetries = 3,
        [int]$DelaySeconds = 5
    )
    
    $attempt = 1
    
    while ($attempt -le $MaxRetries) {
        try {
            Write-Host "Attempt $attempt of $MaxRetries..." -ForegroundColor Gray
            & $ScriptBlock
            Write-Host "✓ Operation succeeded" -ForegroundColor Green
            return
            
        } catch {
            Write-Host "✗ Attempt $attempt failed: $_" -ForegroundColor Yellow
            
            if ($attempt -eq $MaxRetries) {
                Write-Error "Operation failed after $MaxRetries attempts"
                throw
            }
            
            Write-Host "Waiting $DelaySeconds seconds before retry..." -ForegroundColor Gray
            Start-Sleep -Seconds $DelaySeconds
            $attempt++
        }
    }
}

# Usage
Invoke-WithRetry -ScriptBlock {
    az acr login --name $acrName
} -MaxRetries 3 -DelaySeconds 10
```

---

## Documentation References

- [PowerShell Best Practices](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/strongly-encouraged-development-guidelines)
- [PowerShell Approved Verbs](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/approved-verbs-for-windows-powershell-commands)
- [PowerShell Style Guide](https://poshcode.gitbook.io/powershell-practice-and-style/)
- [Azure CLI in PowerShell](https://learn.microsoft.com/en-us/cli/azure/use-cli-effectively)

---

## Checklist for PowerShell Scripts

Before submitting a PowerShell script, ensure:

- [ ] Header comments include synopsis, description, parameters, and examples
- [ ] All parameters have proper validation and help text
- [ ] Error handling with try-catch blocks is implemented
- [ ] No hardcoded secrets or sensitive information
- [ ] Meaningful variable names using proper casing conventions
- [ ] Comments explain "why" not "what"
- [ ] Progress indication for long-running operations
- [ ] Proper indentation (4 spaces) and formatting
- [ ] Exit codes properly set for success/failure
- [ ] Script tested with various input parameters
- [ ] Logging implemented for troubleshooting
- [ ] Compatible with PowerShell 5.1 and PowerShell 7+

---

## Getting Help

If you have questions about PowerShell best practices:

1. Check the [PowerShell documentation](https://learn.microsoft.com/en-us/powershell/)
2. Review existing scripts in the `build/` directory for examples
3. Ask in the team's discussion channel
4. Open an issue for clarification on specific patterns

Happy scripting! 🚀
