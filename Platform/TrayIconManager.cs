using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Tabavoco.Services;
using Tabavoco.Utils;

namespace Tabavoco.Platform;

/// <summary>
/// Manages a system tray (notification area) icon using Win32 Shell_NotifyIcon API.
/// Loads the app icon from PNG using GDI+ and provides a right-click context menu.
/// </summary>
public class TrayIconManager : IDisposable
{
    private const uint WM_TRAYICON = NativeMethods.WM_APP + 1;
    private const int IDM_RESET_POSITION = 1000;
    private const int IDM_RUN_ON_STARTUP = 1001;
    private const int IDM_OPEN_CONFIG = 1002;
    private const int IDM_EXIT = 1003;
    private const string WindowClassName = "TabavocoTrayIconClass";

    private IntPtr _hwnd;
    private IntPtr _hIcon;
    private IntPtr _gdipToken;
    private NativeMethods.NOTIFYICONDATA _nid;
    private readonly NativeMethods.WndProcDelegate _wndProc; // prevent GC collection of delegate
    private bool _disposed;
    private bool _iconAdded;

    public event Action? ExitRequested;
    public event Action? ResetPositionRequested;

    public TrayIconManager()
    {
        _wndProc = WndProc;
        CreateHiddenWindow();
        LoadIconFromPng();
        AddTrayIcon();
    }

    private void CreateHiddenWindow()
    {
        var hInstance = NativeMethods.GetModuleHandle(null);

        var wc = new NativeMethods.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            lpszClassName = WindowClassName,
            lpszMenuName = null!
        };

        var atom = NativeMethods.RegisterClassEx(ref wc);
        if (atom == 0)
        {
            Logger.WriteError($"Failed to register tray window class, error: {Marshal.GetLastWin32Error()}");
            return;
        }

        // HWND_MESSAGE creates a message-only window (no visible UI, receives messages via main thread's message loop)
        _hwnd = NativeMethods.CreateWindowEx(0, WindowClassName, "Tabavoco Tray", 0,
            0, 0, 0, 0, NativeMethods.HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
            Logger.WriteError($"Failed to create tray hidden window, error: {Marshal.GetLastWin32Error()}");
        else
            Logger.WriteInfo("Tray hidden window created");
    }

    private void LoadIconFromPng()
    {
        try
        {
            var startupInput = new NativeMethods.GdiplusStartupInput { GdiplusVersion = 1 };
            if (NativeMethods.GdiplusStartup(out _gdipToken, ref startupInput, IntPtr.Zero) != 0)
            {
                Logger.WriteError("GDI+ startup failed");
                return;
            }

            var iconPath = FindIconPath();
            if (iconPath == null)
            {
                Logger.WriteError("Could not find Assets/icon.png");
                return;
            }

            Logger.WriteInfo($"Loading tray icon from: {iconPath}");

            int status = NativeMethods.GdipCreateBitmapFromFile(iconPath, out var gdipBitmap);
            if (status != 0)
            {
                Logger.WriteError($"GdipCreateBitmapFromFile failed: {status}");
                return;
            }

            status = NativeMethods.GdipCreateHICONFromBitmap(gdipBitmap, out _hIcon);
            NativeMethods.GdipDisposeImage(gdipBitmap);

            if (status != 0)
                Logger.WriteError($"GdipCreateHICONFromBitmap failed: {status}");
            else
                Logger.WriteInfo("Tray icon loaded from PNG");
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to load tray icon: {ex.Message}");
        }
    }

    private static string? FindIconPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Assets", "icon.png"),
            Path.Combine(baseDir, "..", "Assets", "icon.png"),
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }

        return null;
    }

    private void AddTrayIcon()
    {
        if (_hwnd == IntPtr.Zero || _hIcon == IntPtr.Zero)
        {
            Logger.WriteError("Cannot add tray icon: missing window handle or icon");
            return;
        }

        _nid = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NativeMethods.NIF_ICON | NativeMethods.NIF_MESSAGE | NativeMethods.NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _hIcon,
            szTip = "Tabavoco"
        };

        _iconAdded = NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref _nid);
        if (_iconAdded)
            Logger.WriteInfo("Tray icon added");
        else
            Logger.WriteError("Shell_NotifyIcon NIM_ADD failed");
    }

    private void RemoveTrayIcon()
    {
        if (_iconAdded)
        {
            NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref _nid);
            _iconAdded = false;
            Logger.WriteInfo("Tray icon removed");
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            var eventType = (uint)(lParam.ToInt64() & 0xFFFF);
            if (eventType == NativeMethods.WM_RBUTTONUP)
            {
                ShowContextMenu();
            }
            return IntPtr.Zero;
        }

        return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        var hMenu = NativeMethods.CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        try
        {
            // App title (disabled, display-only)
            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_STRING | NativeMethods.MF_GRAYED,
                IntPtr.Zero, GetAppTitle());
            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_SEPARATOR, IntPtr.Zero, null!);

            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_STRING, (IntPtr)IDM_RESET_POSITION, "Reset Position");

            var startupFlags = NativeMethods.MF_STRING;
            if (StartupManager.IsStartupEnabled())
                startupFlags |= NativeMethods.MF_CHECKED;
            NativeMethods.AppendMenu(hMenu, startupFlags, (IntPtr)IDM_RUN_ON_STARTUP, "Run on Startup");

            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_STRING, (IntPtr)IDM_OPEN_CONFIG, "Open Config Folder");
            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_SEPARATOR, IntPtr.Zero, null!);
            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_STRING, (IntPtr)IDM_EXIT, "Exit");

            NativeMethods.GetCursorPos(out var pt);

            // Required: makes menu dismiss when clicking outside
            NativeMethods.SetForegroundWindow(_hwnd);

            var cmd = NativeMethods.TrackPopupMenuEx(hMenu,
                NativeMethods.TPM_RIGHTBUTTON | NativeMethods.TPM_RETURNCMD,
                pt.X, pt.Y, _hwnd, IntPtr.Zero);

            // Post a benign message to fix the well-known menu dismiss bug (MSDN KB135788)
            NativeMethods.PostMessage(_hwnd, 0, IntPtr.Zero, IntPtr.Zero);

            switch (cmd)
            {
                case IDM_RESET_POSITION:
                    ResetPositionRequested?.Invoke();
                    break;
                case IDM_RUN_ON_STARTUP:
                    ToggleRunOnStartup();
                    break;
                case IDM_OPEN_CONFIG:
                    OpenConfigFolder();
                    break;
                case IDM_EXIT:
                    ExitRequested?.Invoke();
                    break;
            }
        }
        finally
        {
            NativeMethods.DestroyMenu(hMenu);
        }
    }

    private static string GetAppTitle()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"Tabavoco {version?.Major}.{version?.Minor}";
        }
        catch
        {
            return "Tabavoco";
        }
    }

    private static void ToggleRunOnStartup()
    {
        var current = StartupManager.IsStartupEnabled();
        StartupManager.SetStartupEnabled(!current);
    }

    private static void OpenConfigFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ConfigurationService.GetConfigDirectory(),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to open config folder: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        RemoveTrayIcon();

        if (_hIcon != IntPtr.Zero)
        {
            NativeMethods.DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }

        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }

        if (_gdipToken != IntPtr.Zero)
        {
            NativeMethods.GdiplusShutdown(_gdipToken);
            _gdipToken = IntPtr.Zero;
        }

        GC.SuppressFinalize(this);
    }

    ~TrayIconManager()
    {
        Dispose();
    }
}
