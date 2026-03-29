# Tabavoco MSIX Signing Script
# Creates a self-signed certificate and signs the MSIX package for local testing.
#
# MUST be run as Administrator (for trusting the certificate):
#   powershell -ExecutionPolicy Bypass -File sign-msix.ps1

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$MsixPath = Get-ChildItem -Path "$ProjectDir\bin\Release" -Filter "*.msix" -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $MsixPath) {
    Write-Host "ERROR: No .msix file found. Run build-msix.ps1 first." -ForegroundColor Red
    exit 1
}

Write-Host "=== Tabavoco MSIX Signing ===" -ForegroundColor Cyan
Write-Host "Package: $($MsixPath.FullName)" -ForegroundColor White

# Step 1: Create or find self-signed certificate
# Publisher must match Package.appxmanifest Identity Publisher exactly
$publisher = "CN=Transparent Source"

Write-Host "`n[1/3] Checking for existing certificate..." -ForegroundColor Yellow
$cert = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq $publisher -and $_.NotAfter -gt (Get-Date) } |
    Sort-Object NotAfter -Descending | Select-Object -First 1

if ($cert) {
    Write-Host "Found existing certificate: $($cert.Thumbprint)" -ForegroundColor Green
} else {
    Write-Host "Creating new self-signed certificate..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $publisher `
        -KeyUsage DigitalSignature `
        -FriendlyName "Tabavoco Dev Certificate" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
        -NotAfter (Get-Date).AddYears(3)
    Write-Host "Created certificate: $($cert.Thumbprint)" -ForegroundColor Green
}

# Step 2: Trust the certificate (requires admin)
Write-Host "`n[2/3] Installing certificate to Trusted People store..." -ForegroundColor Yellow
try {
    $rootStore = [System.Security.Cryptography.X509Certificates.X509Store]::new(
        [System.Security.Cryptography.X509Certificates.StoreName]::TrustedPeople,
        [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
    $rootStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
    $rootStore.Add($cert)
    $rootStore.Close()
    Write-Host "Certificate trusted." -ForegroundColor Green
} catch {
    Write-Host "WARNING: Could not add to trusted store. Run as Administrator." -ForegroundColor Red
    Write-Host "Without this, you'll need to manually trust the cert before installing." -ForegroundColor Yellow
}

# Step 3: Sign the MSIX
Write-Host "`n[3/3] Signing MSIX package..." -ForegroundColor Yellow

# Find SignTool from NuGet packages (installed by Windows App SDK)
$signTool = Get-ChildItem "$env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools" -Filter "signtool.exe" -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "x64" } |
    Sort-Object FullName -Descending | Select-Object -First 1

if (-not $signTool) {
    # Fallback: Windows SDK install paths
    foreach ($path in @("C:\Program Files (x86)\Windows Kits\10\bin", "C:\Program Files\Windows Kits\10\bin")) {
        if (Test-Path $path) {
            $signTool = Get-ChildItem $path -Filter "signtool.exe" -Recurse -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match "x64" } | Select-Object -First 1
            if ($signTool) { break }
        }
    }
}

if (-not $signTool) {
    Write-Host "ERROR: SignTool.exe not found." -ForegroundColor Red
    exit 1
}

Write-Host "Using: $($signTool.FullName)" -ForegroundColor Gray
& $signTool.FullName sign /fd SHA256 /sha1 $cert.Thumbprint $MsixPath.FullName
if ($LASTEXITCODE -ne 0) { throw "Signing failed" }

Write-Host "`n=== Signing complete! ===" -ForegroundColor Cyan
Write-Host "Signed: $($MsixPath.FullName)" -ForegroundColor Green
Write-Host "`nInstall with:" -ForegroundColor Yellow
Write-Host "  Add-AppPackage -Path `"$($MsixPath.FullName)`"" -ForegroundColor White
