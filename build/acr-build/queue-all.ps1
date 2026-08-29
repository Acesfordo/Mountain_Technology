<#
.SYNOPSIS
    Queue Azure Container Registry (ACR) build tasks for all eShop microservices.

.DESCRIPTION
    This script creates ACR build tasks for each microservice in the eShop application.
    Each task is configured to automatically build Docker images from a GitHub repository
    when code is pushed to the specified branch.

.PARAMETER acrName
    The name of your Azure Container Registry (e.g., "myeshopacr").
    This is the registry where Docker images will be stored.

.PARAMETER gitUser
    Your GitHub username or organization name (e.g., "myusername").
    This is used to construct the full repository URL.

.PARAMETER repoName
    The GitHub repository name. Defaults to "eShopOnContainers".
    Change this if you've forked the repository with a different name.

.PARAMETER gitBranch
    The Git branch to monitor for changes. Defaults to "dev".
    Common values: "main", "master", "develop", "dev"

.PARAMETER patToken
    Personal Access Token (PAT) for GitHub authentication. REQUIRED.
    This token must have repo access permissions.
    Generate one at: https://github.com/settings/tokens

.EXAMPLE
    .\queue-all.ps1 -acrName "myeshopacr" -gitUser "myusername" -patToken "ghp_xxxxxxxxxxxx"
    
    Creates ACR build tasks for all services using default repository name and dev branch.

.EXAMPLE
    .\queue-all.ps1 -acrName "myeshopacr" -gitUser "myorg" -repoName "eShop" -gitBranch "main" -patToken "ghp_xxxxxxxxxxxx"
    
    Creates ACR build tasks using custom repository name and main branch.

.NOTES
    Prerequisites:
    - Azure CLI must be installed and configured (az login)
    - You must have permissions to create build tasks in the specified ACR
    - GitHub PAT must be valid and have appropriate permissions

    Author: eShop Team
    Last Modified: 2026
#>

Param(
    [parameter(Mandatory=$false)][string]$acrName,
    [parameter(Mandatory=$false)][string]$gitUser,
    [parameter(Mandatory=$false)][string]$repoName="eShopOnContainers",
    [parameter(Mandatory=$false)][string]$gitBranch="dev",
    [parameter(Mandatory=$true)][string]$patToken
)

# Construct the full GitHub repository URL
# Example: https://github.com/myusername/eShopOnContainers
$gitContext = "https://github.com/$gitUser/$repoName"

# Define all microservices that need to be built
# Each service includes:
#   - Name: Unique identifier for the ACR build task
#   - Image: Docker image name that will be created
#   - File: Path to the Dockerfile within the repository
$services = @( 
    @{ Name="eshopbasket"; Image="eshop/basket.api"; File="src/Services/Basket/Basket.API/Dockerfile" },
    @{ Name="eshopcatalog"; Image="eshop/catalog.api"; File="src/Services/Catalog/Catalog.API/Dockerfile" },
    @{ Name="eshopidentity"; Image="eshop/identity.api"; File="src/Services/Identity/Identity.API/Dockerfile" },
    @{ Name="eshopordering"; Image="eshop/ordering.api"; File="src/Services/Ordering/Ordering.API/Dockerfile" },
	@{ Name="eshoporderingbg"; Image="eshop/orderprocessor"; File="src/Services/Ordering/OrderProcessor/Dockerfile" },
    @{ Name="eshopwebspa"; Image="eshop/webspa"; File="src/Web/WebSPA/Dockerfile" },
    @{ Name="eshopwebmvc"; Image="eshop/webmvc"; File="src/Web/WebMVC/Dockerfile" },
    @{ Name="eshopwebstatus"; Image="eshop/webstatus"; File="src/Web/WebStatus/Dockerfile" },
    @{ Name="eshoppayment"; Image="eshop/paymentprocessor"; File="src/Services/Payment/PaymentProcessor/Dockerfile" },
    @{ Name="eshopocelotapigw"; Image="eshop/ocelotapigw"; File="src/ApiGateways/ApiGw-Base/Dockerfile" },
    @{ Name="eshopmobileshoppingagg"; Image="eshop/mobileshoppingagg"; File="src/ApiGateways/Mobile.Bff.Shopping/aggregator/Dockerfile" },
    @{ Name="eshopwebshoppingagg"; Image="eshop/webshoppingagg"; File="src/ApiGateways/Web.Bff.Shopping/aggregator/Dockerfile" },
    @{ Name="eshoporderingsignalrhub"; Image="eshop/ordering.signalrhub"; File="src/Services/Ordering/Ordering.SignalrHub/Dockerfile" }
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ACR Build Task Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Registry: $acrName" -ForegroundColor Yellow
Write-Host "Git Context: $gitContext" -ForegroundColor Yellow
Write-Host "Branch: $gitBranch" -ForegroundColor Yellow
Write-Host "Services to configure: $($services.Count)" -ForegroundColor Yellow
Write-Host ""

# Iterate through each service and create an ACR build task
# The build task will automatically trigger when code is pushed to the specified branch
$services |% {
    # Extract service details
    $bname = $_.Name        # Build task name
    $bimg = $_.Image        # Docker image name
    $bfile = $_.File        # Dockerfile path
    
    Write-Host "Configuring ACR build task for: $bname" -ForegroundColor Green
    Write-Host "  - Image: ${bimg}:$gitBranch" -ForegroundColor Gray
    Write-Host "  - Dockerfile: $bfile" -ForegroundColor Gray
    
    # Create the ACR build task using Azure CLI
    # This task will:
    #   1. Monitor the specified GitHub branch
    #   2. Automatically trigger builds when commits are pushed
    #   3. Build the Docker image using the specified Dockerfile
    #   4. Tag the image with the branch name
    #   5. Push the image to your ACR
    az acr build-task create `
        --registry $acrName `
        --name $bname `
        --image ${bimg}:$gitBranch `
        --context $gitContext `
        --branch $gitBranch `
        --git-access-token $patToken `
        --file $bfile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Successfully configured $bname" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Failed to configure $bname" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Configuration Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
