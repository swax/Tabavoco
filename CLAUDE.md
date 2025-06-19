# Tabavoco - Volume Control App

## Overview
A simple WinUI 3 volume control application that sits in the bottom left corner of the screen, providing persistent and easily accessible volume control.

## Build
```bash
# From WSL2
dotnet.exe build

# From Windows
dotnet build
```

## Key Files
- **App.xaml.cs**: Application entry point
- **MiniVolumeWindow.xaml**: Main volume control window
- **Package.appxmanifest**: App package configuration

## Requirements
- .NET 9.0
- Windows 10.0.19041.0 or higher
- Platforms: x64, x86, ARM64

## Development Guidelines
- Always add comments explaining why code is implemented a particular way, especially for workarounds or non-obvious solutions
