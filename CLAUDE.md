# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# From WSL2
dotnet.exe build
dotnet.exe run

# From Windows Command Prompt  
dotnet build
dotnet run

# Publish for distribution (self-contained, ~80MB)
dotnet publish -c Release -r win-x64
```

**Critical**: Must use `PublishTrimmed=false` to preserve COM interop functionality for Windows Core Audio API.

## Architecture Overview

WinUI 3 application providing persistent volume control overlay. Uses layered architecture with clear separation of concerns.

### Core Components & Data Flow

**Application Layer:**
1. **App.xaml.cs** - Application entry with single-instance checking and global exception handling

**Views Layer:**
2. **Views/MiniVolumeWindow** - Main UI that owns VolumeManager and handles all user interactions
3. **Views/MediaControlsUserControl** - Media control buttons (play/pause/next/previous) with SMTC integration

**Services Layer:**
4. **Services/VolumeManager** - High-level API that caches volume/mute state, uses AudioDeviceManager for COM calls
5. **Services/AudioDeviceManager** - Direct Windows Core Audio API COM wrapper using IAudioEndpointVolume
6. **Services/MediaControlManager** - System Media Transport Controls integration for media playback control
7. **Services/ConfigurationService** - JSON-based settings persistence using Microsoft.Extensions.Configuration
8. **Services/StartupManager** - Windows registry management for run-on-startup functionality

**Platform Layer:**
9. **Platform/Win32WindowManager** - Native Win32 API calls for topmost positioning and tool window behavior
10. **Platform/SmtcApiWrapper** - System Media Transport Controls wrapper for media transport events

**Utils Layer:**
11. **Utils/Logger** - Debug logging utility with file output and configurable log levels

### Key Implementation Patterns

**Volume Management**:
- VolumeManager caches state to minimize expensive COM calls
- 1-second timer refreshes cache when user not interacting
- Immediate updates during user interaction for responsiveness
- Polling approach used (Windows audio events are complex)

**Window Behavior**:
- Uses dual approach for topmost: WinUI presenter + Win32 SetWindowPos
- Timer-based topmost enforcement (500ms) overcomes taskbar z-index changes  
- Tool window configuration hides from taskbar and Alt+Tab
- DPI scaling handled with fallback detection methods

**COM Interop**:
- Manual COM interface definitions for Windows Core Audio API
- Project requires `BuiltInComInteropSupport=true` and no trimming
- Proper disposal patterns implemented throughout

**Configuration System**:
- appsettings.json stores window position and settings
- ConfigurationService provides strongly-typed access
- Window position persisted automatically on drag

### Development Patterns

**Error Handling**:
- Logger.cs writes to both Debug output and file (tabavoco-debug.log)
- Info logging disabled by default (ENABLE_INFO_LOGGING = false)
- Global exception handlers in App.xaml.cs with user-friendly dialogs

**Resource Management**:
- VolumeManager disposed in window close handler
- COM objects rely on .NET garbage collection
- Single instance enforcement using named Mutex

**UI Interaction**:
- _isUserInteracting flag prevents feedback loops during manual volume changes
- Pointer events handle drag-to-move with position saving
- Context menu provides exit and startup toggle functionality
- MediaControlsUserControl provides play/pause/next/previous buttons with SMTC integration

**Media Control Integration**:
- System Media Transport Controls (SMTC) for global media playback control
- MediaControlManager coordinates between UI and system media sessions
- SmtcApiWrapper provides clean interface to Windows media transport APIs
- Supports play/pause/next/previous commands sent to active media applications

## Important Technical Details

- Target: .NET 9.0 with Windows 10.0.19041.0+ 
- Platforms: x64, x86, ARM64
- Uses Windows Core Audio API directly (no external dependencies)
- Constants define timing (500ms topmost timer, 1000ms volume sync)
- DPI scaling calculated with XamlRoot.RasterizationScale + Win32 fallback
- Window sized 240×40 logical pixels, scales automatically