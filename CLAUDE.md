# TaBaVoCo - WinUI 3 Application

## Project Overview
This is a WinUI 3 application targeting .NET 9.0 for Windows 10/11.

## Build Instructions

### From WSL2
Use Windows dotnet to build the application:
```bash
dotnet.exe build
```

### From Windows
```cmd
dotnet build
```

## Project Structure
- **App.xaml/App.xaml.cs**: Application entry point
- **Views/MainPage.xaml**: Main UI page
- **Assets/**: Application icons and images
- **Package.appxmanifest**: App package configuration
- **TaBaVoCo.csproj**: Project configuration

## Target Framework
- .NET 9.0 with Windows 10.0.19041.0
- Minimum Windows version: 10.0.17763.0
- Platforms: x64, x86, ARM64

## Dependencies
- Microsoft.WindowsAppSDK
- Microsoft.Web.WebView2
- Microsoft.Windows.SDK.BuildTools

## Development Notes
- This application requires Windows to build and run
- From WSL2, use `dotnet.exe` instead of `dotnet` for building
- XAML compilation requires Windows environment