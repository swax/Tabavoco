# Tabavoco MSIX Local Install Script
# Trusts the dev certificate and installs the MSIX package.
#
# Run as Administrator:
#   powershell -ExecutionPolicy Bypass -File install-msix.ps1

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$MsixPath = Get-ChildItem -Path "$ProjectDir\bin\Release" -Filter "*.msix" -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $MsixPath) {
    Write-Host "ERROR: No .msix file found. Run build-msix.ps1 first." -ForegroundColor Red
    exit 1
}

Write-Host "=== Tabavoco MSIX Install ===" -ForegroundColor Cyan

# Step 1: Trust the certificate
Write-Host "`n[1/2] Trusting dev certificate..." -ForegroundColor Yellow
$cert = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq "CN=Transparent Source" -and $_.NotAfter -gt (Get-Date) } |
    Sort-Object NotAfter -Descending | Select-Object -First 1

if (-not $cert) {
    Write-Host "ERROR: No certificate found. Run sign-msix.ps1 first." -ForegroundColor Red
    exit 1
}

try {
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPeople", "LocalMachine")
    $store.Open("ReadWrite")
    $store.Add($cert)
    $store.Close()
    Write-Host "Certificate trusted." -ForegroundColor Green
} catch {
    Write-Host "ERROR: Failed to trust certificate. Are you running as Administrator?" -ForegroundColor Red
    exit 1
}

# Step 2: Install the package
Write-Host "`n[2/2] Installing $($MsixPath.Name)..." -ForegroundColor Yellow
Add-AppPackage -Path $MsixPath.FullName

Write-Host "`n=== Tabavoco installed! ===" -ForegroundColor Cyan
Write-Host "Launch it from the Start menu." -ForegroundColor Green
Write-Host "`nTo uninstall: Get-AppPackage *Tabavoco* | Remove-AppPackage" -ForegroundColor Gray
