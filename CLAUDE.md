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

## Architecture

### Core Components
- **src/App.xaml.cs**: Application entry point and WinUI 3 setup
- **src/MiniVolumeWindow.xaml/.cs**: Main UI window with volume slider, mute button, and event handling
- **src/VolumeManager.cs**: High-level volume control API with caching layer that owns AudioDeviceManager instance
- **src/AudioDeviceManager.cs**: Low-level Windows Core Audio API COM interop wrapper
- **src/Win32WindowManager.cs**: Native window positioning and topmost management utilities
- **src/StartupManager.cs**: Windows startup registry management for auto-start functionality
- **src/Logger.cs**: Debug logging utility with configurable info/error level support and file output
- **Package.appxmanifest**: App package configuration

### Data Flow
1. **MiniVolumeWindow** owns a VolumeManager instance and disposes it on close
2. **VolumeManager** caches volume/mute/endpoint state for performance
3. **Periodic refresh** (1-second timer) calls VolumeManager.RefreshFromSystem() to sync with external changes
4. **Immediate updates** when user interacts - cache updated instantly for responsive UI

### Key Design Decisions
- **Caching strategy**: VolumeManager caches state to avoid expensive COM calls on every UI update
- **Polling approach**: 1-second timer refreshes cache since Windows audio change events are complex
- **Instance ownership**: Window creates/owns VolumeManager for clear lifecycle management
- **Separation of concerns**: UI → VolumeManager → AudioDeviceManager → COM APIs

## Requirements
- .NET 9.0
- Windows 10.0.19041.0 or higher
- Platforms: x64, x86, ARM64

## Development Guidelines
- Always add comments explaining why code is implemented a particular way, especially for workarounds or non-obvious solutions
