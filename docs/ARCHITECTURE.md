# Architecture

WinUI 3 application providing a persistent volume control overlay. Uses a layered architecture with clear separation of concerns.

## Project Structure

```
├── App.xaml(.cs)                           # Application entry point, single-instance check, global exception handling
├── Views/
│   ├── MiniVolumeWindow.xaml(.cs)          # Main UI window with volume slider
│   └── MediaControlsUserControl.xaml(.cs)  # Media control buttons (play/pause/next/previous)
├── Services/
│   ├── VolumeManager.cs                    # High-level volume API with state caching
│   ├── AudioDeviceManager.cs               # Windows Core Audio API COM interop wrapper
│   ├── MediaControlManager.cs              # System Media Transport Controls integration
│   ├── ConfigurationService.cs             # JSON-based configuration management
│   └── StartupManager.cs                   # Windows startup registry management
├── Platform/
│   ├── NativeMethods.cs                    # Win32/COM API declarations and constants
│   ├── Win32WindowManager.cs               # Native window positioning and topmost management
│   ├── SmtcApiWrapper.cs                   # System Media Transport Controls wrapper
│   └── TrayIconManager.cs                  # System tray icon and context menu
├── Utils/
│   └── Logger.cs                           # Debug logging with file output
└── appsettings.json                        # Application configuration (window position, etc.)
```

## Data Flow

1. **MiniVolumeWindow** owns a VolumeManager instance and disposes it on close
2. **VolumeManager** caches volume/mute/endpoint state to minimize expensive COM calls
3. **AudioDeviceManager** wraps Windows Core Audio COM APIs (IAudioEndpointVolume)
4. **MediaControlManager** coordinates between UI and system media sessions via SMTC
5. **ConfigurationService** persists settings to appsettings.json
6. Flow: UI → Services → Platform APIs → COM/Win32 APIs

## Key Implementation Details

### Volume Management

- VolumeManager caches state to minimize expensive COM calls
- 1-second timer refreshes cache when user is not interacting
- Immediate updates during user interaction for responsiveness
- Polling approach used (Windows audio change events are complex)
- `_isUserInteracting` flag prevents feedback loops during manual volume changes

### Window Behavior

- Dual approach for topmost: WinUI presenter + Win32 SetWindowPos
- Win32 event hooks (WinEventDelegate) re-enforce topmost when foreground/z-order changes
- Tool window configuration hides from taskbar and Alt+Tab
- DPI scaling via XamlRoot.RasterizationScale with Win32 fallback
- Window sized 240x40 logical pixels, scales automatically
- Pointer events enable drag-to-move with position saving

### COM Interop

- Manual COM interface definitions for Windows Core Audio API
- Project requires `BuiltInComInteropSupport=true` in the csproj
- `PublishTrimmed=false` required - trimming breaks COM interop
- COM objects rely on .NET garbage collection

### Media Controls

- System Media Transport Controls (SMTC) for global media playback
- SmtcApiWrapper provides clean interface to Windows media transport APIs
- Supports play/pause/next/previous commands sent to active media applications

### Configuration

- appsettings.json stores window position and settings
- ConfigurationService provides strongly-typed access via Microsoft.Extensions.Configuration
- Window position persisted automatically on drag

### Error Handling & Logging

- Logger.cs writes to both Debug output and file (tabavoco-debug.log)
- Info logging disabled by default (ENABLE_INFO_LOGGING = false)
- Global exception handlers in App.xaml.cs with user-friendly dialogs

### Resource Management

- VolumeManager disposed in window close handler
- Single instance enforcement using named Mutex

## Build Notes

- Target: .NET 9.0 with Windows 10.0.19041.0+
- Platforms: x64, x86, ARM64
- Uses Windows Core Audio API directly (no external audio dependencies)
- 1000ms volume sync timer interval
