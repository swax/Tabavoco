# Tabavoco MSIX Build Script
# Builds the app as an MSIX bundle (.msixupload) for Microsoft Store submission,
# targeting x64, x86, and ARM64 architectures.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File build-msix.ps1
#
# For Store submission, upload the .msixupload to Partner Center (Store handles signing).
# For sideloading, use the individual .msix packages from the output directory.

$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
$OutputDir = "$ProjectDir\installer-output"
$Architectures = @("x64", "x86", "arm64")

Write-Host "=== Tabavoco MSIX Store Build ===" -ForegroundColor Cyan
Write-Host "Architectures: $($Architectures -join ', ')" -ForegroundColor Cyan

New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null

# Build MSIX for each architecture
$msixFiles = @()
foreach ($arch in $Architectures) {
    Write-Host "`n[Building] $arch..." -ForegroundColor Yellow

    $rid = "win-$arch"
    dotnet publish TaBaVoCo.csproj -c Release -r $rid `
        -p:WindowsPackageType=MSIX `
        -p:GenerateAppxPackageOnBuild=true `
        -p:AppxPackageSigningEnabled=false `
        -p:AppxBundle=Never
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $arch" }

    # Find the generated .msix
    $appPackagesDir = "$ProjectDir\bin\Release\net9.0-windows10.0.19041.0\$rid\AppPackages"
    $msix = Get-ChildItem -Path $appPackagesDir -Filter "*.msix" -Recurse | Select-Object -First 1
    if (-not $msix) {
        throw "MSIX package not found for $arch in $appPackagesDir"
    }

    $destPath = "$OutputDir\$($msix.Name)"
    Copy-Item -Path $msix.FullName -Destination $destPath -Force
    $msixFiles += $destPath
    Write-Host "  Built: $($msix.Name) ($([math]::Round($msix.Length / 1MB, 1)) MB)" -ForegroundColor Green
}

# Create .msixbundle from individual .msix packages
Write-Host "`n[Bundling] Creating .msixbundle..." -ForegroundColor Yellow

$version = Select-Xml -Path "$ProjectDir\Package.appxmanifest" -XPath "//*[local-name()='Identity']/@Version" |
    Select-Object -ExpandProperty Node | Select-Object -ExpandProperty Value

$bundleName = "Tabavoco_${version}.msixbundle"
$bundlePath = "$OutputDir\$bundleName"

# Use makeappx from Windows SDK or NuGet packages
$sdkToolPath = $null
$sdkPaths = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\makeappx.exe",
    "${env:ProgramFiles}\Windows Kits\10\bin\*\x64\makeappx.exe",
    "${env:USERPROFILE}\.nuget\packages\microsoft.windows.sdk.buildtools\*\bin\*\x64\makeappx.exe"
)
foreach ($pattern in $sdkPaths) {
    $found = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
    if ($found) { $sdkToolPath = $found.FullName; break }
}

if (-not $sdkToolPath) {
    throw "makeappx.exe not found. Install the Windows 10/11 SDK."
}

Write-Host "  Using: $sdkToolPath" -ForegroundColor Gray

# Create a mapping file for the bundle
$mappingFile = "$OutputDir\bundle_mapping.txt"
$mappingContent = "[Files]`n"
foreach ($msixPath in $msixFiles) {
    $fileName = Split-Path $msixPath -Leaf
    $mappingContent += "`"$msixPath`" `"$fileName`"`n"
}
Set-Content -Path $mappingFile -Value $mappingContent

& $sdkToolPath bundle /f $mappingFile /p $bundlePath /o
if ($LASTEXITCODE -ne 0) { throw "Bundle creation failed" }

Remove-Item -Path $mappingFile -Force

# Create .msixupload (a zip containing the .msixbundle)
Write-Host "`n[Packaging] Creating .msixupload..." -ForegroundColor Yellow

$uploadName = "Tabavoco_${version}.msixupload"
$uploadPath = "$OutputDir\$uploadName"
$tempZipPath = "$OutputDir\Tabavoco_${version}_bundle.zip"

if (Test-Path $uploadPath) { Remove-Item $uploadPath -Force }
if (Test-Path $tempZipPath) { Remove-Item $tempZipPath -Force }

Compress-Archive -Path $bundlePath -DestinationPath $tempZipPath
Rename-Item -Path $tempZipPath -NewName $uploadName

# Summary
Write-Host "`n=== Build complete! ===" -ForegroundColor Cyan
Write-Host "Individual packages:" -ForegroundColor Green
foreach ($msixPath in $msixFiles) {
    $f = Get-Item $msixPath
    Write-Host "  $($f.Name) ($([math]::Round($f.Length / 1MB, 1)) MB)" -ForegroundColor Green
}
$bundle = Get-Item $bundlePath
$upload = Get-Item $uploadPath
Write-Host "Bundle: $bundleName ($([math]::Round($bundle.Length / 1MB, 1)) MB)" -ForegroundColor Green
Write-Host "Upload: $uploadName ($([math]::Round($upload.Length / 1MB, 1)) MB)" -ForegroundColor Green
Write-Host "`nFor Store: Upload $uploadName to Partner Center" -ForegroundColor Yellow
Write-Host "For sideloading: Use individual .msix files from $OutputDir" -ForegroundColor Yellow
