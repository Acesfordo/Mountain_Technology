<#
.SYNOPSIS
    Create and push multi-architecture Docker manifests for eShop microservices.

.DESCRIPTION
    This script creates Docker manifests that combine Linux and Windows container images
    into single multi-architecture images. This allows Docker to automatically pull the
    correct image based on the host operating system.
    
    The script processes all eShop microservices and creates manifests for three tags:
    - latest: Most recent stable build
    - master: Production-ready main branch builds
    - dev: Development branch builds
    
    Each manifest combines:
    - Linux container images (linux-latest, linux-master, linux-dev)
    - Windows container images (win-latest, win-master, win-dev)

.PARAMETER registry
    The Docker registry URL where images are stored. REQUIRED.
    Examples:
    - Docker Hub: "myusername" or "myorg"
    - Azure Container Registry: "myacr.azurecr.io"
    - Private registry: "registry.company.com:5000"

.EXAMPLE
    .\create-manifests.ps1 -registry "myacr.azurecr.io"
    
    Creates and pushes multi-arch manifests for all services to Azure Container Registry.

.EXAMPLE
    .\create-manifests.ps1 -registry "myusername"
    
    Creates and pushes multi-arch manifests for all services to Docker Hub.

.NOTES
    Prerequisites:
    - Docker CLI must be installed and configured
    - You must be logged into the registry (docker login)
    - Platform-specific images must already exist in the registry:
      * linux-latest, linux-master, linux-dev
      * win-latest, win-master, win-dev
    - Experimental features must be enabled for manifest commands

    To enable experimental features, add to ~/.docker/config.json:
    {
        "experimental": "enabled"
    }

    Author: eShop Team
    Last Modified: 2026
#>

Param(
    [parameter(Mandatory=$true)][string]$registry
)

# Validate that the registry parameter is provided
if ([String]::IsNullOrEmpty($registry)) {
    Write-Host "ERROR: Registry parameter is required!" -ForegroundColor Red
    Write-Host "Usage: .\create-manifests.ps1 -registry <your-registry>" -ForegroundColor Yellow
    Write-Host "Example: .\create-manifests.ps1 -registry myacr.azurecr.io" -ForegroundColor Yellow
    exit 1 
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Multi-Architecture Manifest Creation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script creates multi-architecture Docker manifests" -ForegroundColor Yellow
Write-Host "that combine Linux and Windows container images." -ForegroundColor Yellow
Write-Host ""
Write-Host "Registry: $registry" -ForegroundColor Green
Write-Host ""
Write-Host "Source Tags (platform-specific):" -ForegroundColor Cyan
Write-Host "  - linux-master, win-master" -ForegroundColor Gray
Write-Host "  - linux-dev, win-dev" -ForegroundColor Gray
Write-Host "  - linux-latest, win-latest" -ForegroundColor Gray
Write-Host ""
Write-Host "Target Tags (multi-arch):" -ForegroundColor Cyan
Write-Host "  - master (production)" -ForegroundColor Gray
Write-Host "  - dev (development)" -ForegroundColor Gray
Write-Host "  - latest (most recent)" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Define all microservices that need multi-arch manifests
# These match the service names in the eShop architecture
$services = "identity.api", "basket.api", "catalog.api", "ordering.api", "orderprocessor", "paymentprocessor", "webhooks.api", "ocelotapigw", "mobileshoppingagg", "webshoppingagg", "ordering.signalrhub", "webstatus", "webspa", "webmvc", "webhooks.client"

Write-Host "Processing $($services.Count) services..." -ForegroundColor Yellow
Write-Host ""

# Iterate through each service to create and push manifests
foreach ($svc in $services) {
    Write-Host "Service: $svc" -ForegroundColor Cyan
    Write-Host "----------------------------------------"
    
    # Step 1: Create manifests for all three tags
    # A manifest is a list of images for different architectures/OS
    Write-Host "  Creating manifests for tags: :latest, :master, :dev" -ForegroundColor Yellow
    
    # Create master manifest (combines linux-master and win-master)
    Write-Host "    Creating :master manifest..." -ForegroundColor Gray
    docker manifest create $registry/${svc}:master $registry/${svc}:linux-master $registry/${svc}:win-master
    
    # Create dev manifest (combines linux-dev and win-dev)
    Write-Host "    Creating :dev manifest..." -ForegroundColor Gray
    docker manifest create $registry/${svc}:dev $registry/${svc}:linux-dev $registry/${svc}:win-dev
    
    # Create latest manifest (combines linux-latest and win-latest)
    Write-Host "    Creating :latest manifest..." -ForegroundColor Gray
    docker manifest create $registry/${svc}:latest $registry/${svc}:linux-latest $registry/${svc}:win-latest
    
    # Step 2: Push manifests to the registry
    # This makes the multi-arch images available for pulling
    Write-Host "  Pushing manifests to registry..." -ForegroundColor Yellow
    
    Write-Host "    Pushing :latest..." -ForegroundColor Gray
    docker manifest push $registry/${svc}:latest
    
    Write-Host "    Pushing :dev..." -ForegroundColor Gray
    docker manifest push $registry/${svc}:dev
    
    Write-Host "    Pushing :master..." -ForegroundColor Gray
    docker manifest push $registry/${svc}:master
    
    Write-Host "  ✓ Completed $svc" -ForegroundColor Green
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  All Manifests Created Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Verify manifests: docker manifest inspect $registry/<service>:latest" -ForegroundColor Gray
Write-Host "  2. Pull images: docker pull $registry/<service>:latest" -ForegroundColor Gray
Write-Host "  3. Docker will automatically select the correct platform image" -ForegroundColor Gray
Write-Host ""