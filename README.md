# Tabavoco - Taskbar Volume Control App

A lightweight WinUI 3 volume control application that provides persistent, easily accessible system volume control from the bottom left corner of your screen.

![Screenshot](Assets/screenshot.png)

## Features

- **Always-on-top volume control** - Stays above all windows including taskbar
- **System volume integration** - Direct control of Windows master volume
- **Mute toggle** - Quick mute/unmute with visual feedback
- **Draggable interface** - Move the control anywhere on screen
- **Minimal footprint** - No taskbar presence, small memory usage
- **Right-click context menu** - Easy exit and run on startup option
- **External sync** - Automatically detects and syncs with volume changes made by other applications
- **Position persistence** - Remembers window position across application restarts

## Build & Run

### Requirements
- .NET 9.0
- Windows 10.0.19041.0 or higher
- Platforms: x64, x86, ARM64

### Build Commands
```bash
# From WSL2
dotnet.exe build

# From Windows Command Prompt
dotnet build

# Run the application
dotnet run
```

## Architecture

### Project Structure
```
├── App.xaml(.cs)                           # Application entry point and WinUI 3 setup
├── Views/
│   ├── MiniVolumeWindow.xaml(.cs)          # Main UI window with volume slider
│   └── MediaControlsUserControl.xaml(.cs)  # Media control buttons (play/pause/etc)
├── Services/
│   ├── VolumeManager.cs                    # High-level volume control API with caching
│   ├── AudioDeviceManager.cs               # Windows Core Audio API COM interop wrapper
│   ├── MediaControlManager.cs              # System Media Transport Controls integration
│   ├── ConfigurationService.cs             # JSON-based configuration management
│   └── StartupManager.cs                   # Windows startup registry management
├── Platform/
│   ├── Win32WindowManager.cs               # Native window positioning and topmost management
│   └── SmtcApiWrapper.cs                   # System Media Transport Controls wrapper
├── Utils/
│   └── Logger.cs                           # Debug logging utility with file output
└── appsettings.json                        # Application configuration (window position, etc.)
```

### Data Flow & Design
1. **MiniVolumeWindow** owns a VolumeManager instance and disposes it on close
2. **VolumeManager** caches volume/mute/endpoint state to avoid expensive COM calls
3. **MediaControlManager** handles system media transport controls (play/pause/next/previous)
4. **ConfigurationService** manages JSON-based app settings (window position, etc.)
5. **Periodic refresh** (1-second timer) syncs with external volume changes
6. **Immediate updates** when user interacts for responsive UI
7. **Separation of concerns**: UI → Services → Platform APIs → COM/Win32 APIs

### Technical Implementation
- Windows Core Audio API (IAudioEndpointVolume) for direct system volume access
- Win32 API calls ensure window stays above taskbar and doesn't appear in task switcher
- Timer-based topmost enforcement overcomes Windows taskbar z-index changes
- Pointer events enable drag-to-move functionality
- JSON configuration system (Microsoft.Extensions.Configuration) for settings persistence
- Polling approach used since Windows audio change events are complex

## Testing (Manual)

- App starts at bottom-left (or last saved position)
- App is initialized with current volume
- Slider changes system volume 
- Mute button toggles system mute 
- Window stays above taskbar 
- Dragging moves window and saves position
- Right click to exit

## Development

### Guidelines
- Always add comments explaining why code is implemented a particular way
- Test manually after changes to volume or window management code
- Use Debug.WriteLine/Error for troubleshooting COM/Win32 API issues

## Packaging

Build for distribution: Self-Contained (~80 MB includes .NET 9 and Windows App SDK)

```bash
dotnet publish -c Release -r win-x64
```

Output will be in `bin/Release/net9.0-windows10.0.19041.0/win-x64/publish/`

> **Note**: Trimming must be disabled to preserve COM interop functionality for Windows Audio API.

## License

MIT