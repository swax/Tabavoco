using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Tabavoco.Utils;

namespace Tabavoco.Platform;

/// <summary>
/// Wraps Win32 API calls for window management, providing a clean interface
/// for positioning, styling, and managing window behavior
/// </summary>
public static class Win32WindowManager
{
    /// <summary>
    /// Gets the full bounds of the primary monitor (includes taskbar area)
    /// </summary>
    private static NativeMethods.RECT GetPrimaryMonitorBounds()
    {
        var point = new NativeMethods.POINT { X = 0, Y = 0 };
        var hMonitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTOPRIMARY);
        var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        NativeMethods.GetMonitorInfo(hMonitor, ref info);
        return info.rcMonitor;
    }

    /// <summary>
    /// Gets the screen dimensions of the primary monitor
    /// </summary>
    public static (int width, int height) GetScreenDimensions()
    {
        var bounds = GetPrimaryMonitorBounds();
        return (bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
    }

    /// <summary>
    /// Checks whether a given screen position is visible on any connected monitor.
    /// Returns false if the point is off-screen (e.g. a monitor was disconnected).
    /// </summary>
    public static bool IsPositionOnAnyMonitor(int x, int y)
    {
        var point = new NativeMethods.POINT { X = x, Y = y };
        var hMonitor = NativeMethods.MonitorFromPoint(point, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(hMonitor, ref info))
            return false;

        // MonitorFromPoint with DEFAULTTONEAREST always returns a monitor,
        // so check if the point actually falls within that monitor's bounds
        return x >= info.rcMonitor.Left && x < info.rcMonitor.Right &&
               y >= info.rcMonitor.Top && y < info.rcMonitor.Bottom;
    }

    /// <summary>
    /// Configures a window to behave as a tool window that doesn't appear in taskbar
    /// </summary>
    public static void ConfigureAsToolWindow(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);

        // Set as tool window and remove from taskbar
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
            (exStyle | NativeMethods.WS_EX_TOOLWINDOW) & ~NativeMethods.WS_EX_APPWINDOW);
    }

    /// <summary>
    /// Applies topmost styling to ensure window stays above all other windows including taskbar
    /// </summary>
    public static void ApplyTopmostStyle(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Dual approach: Extended style is more persistent, SetWindowPos has immediate effect
        // Standard WinUI 3 IsAlwaysOnTop doesn't guarantee positioning above taskbar
        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
            (exStyle | NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_TOOLWINDOW) & ~NativeMethods.WS_EX_APPWINDOW);

        NativeMethods.SetWindowPos(hwnd, (IntPtr)NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                     NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE);
    }

    /// <summary>
    /// Positions the window at the bottom left of the primary monitor (overlays taskbar).
    /// Padding is measured from the window's edges to the screen edges.
    /// </summary>
    public static void PositionAtBottomLeft(Window window, int paddingLeft = 8, int paddingBottom = 8)
    {
        var bounds = GetPrimaryMonitorBounds();
        var windowHeight = window.AppWindow.Size.Height;
        var x = bounds.Left + paddingLeft;
        var y = bounds.Bottom - windowHeight - paddingBottom;

        window.AppWindow.Move(new PointInt32(x, y));
    }

    /// <summary>
    /// Gets the window handle for a WinUI 3 window
    /// </summary>
    public static IntPtr GetWindowHandle(Window window)
    {
        return WinRT.Interop.WindowNative.GetWindowHandle(window);
    }

    /// <summary>
    /// Gets the DPI for a specific window
    /// </summary>
    public static uint GetDpiForWindowHandle(IntPtr hwnd)
    {
        return NativeMethods.GetDpiForWindow(hwnd);
    }

    #region Media Control Methods
    /// <summary>
    /// Sends a play/pause command to the foreground application via WM_APPCOMMAND
    /// </summary>
    public static bool SendMediaPlayPause()
    {
        try
        {
            var foregroundWindow = NativeMethods.GetForegroundWindow();
            var lParam = new IntPtr(NativeMethods.APPCOMMAND_MEDIA_PLAY_PAUSE << 16);
            NativeMethods.SendMessage(foregroundWindow, NativeMethods.WM_APPCOMMAND, IntPtr.Zero, lParam);
            Logger.WriteInfo("WM_APPCOMMAND play/pause sent");
            return true;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to send WM_APPCOMMAND play/pause: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a next track command to the foreground application via WM_APPCOMMAND
    /// </summary>
    public static bool SendMediaNextTrack()
    {
        try
        {
            var foregroundWindow = NativeMethods.GetForegroundWindow();
            var lParam = new IntPtr(NativeMethods.APPCOMMAND_MEDIA_NEXTTRACK << 16);
            NativeMethods.SendMessage(foregroundWindow, NativeMethods.WM_APPCOMMAND, IntPtr.Zero, lParam);
            Logger.WriteInfo("WM_APPCOMMAND next track sent");
            return true;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to send WM_APPCOMMAND next track: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sends a previous track command to the foreground application via WM_APPCOMMAND
    /// </summary>
    public static bool SendMediaPreviousTrack()
    {
        try
        {
            var foregroundWindow = NativeMethods.GetForegroundWindow();
            var lParam = new IntPtr(NativeMethods.APPCOMMAND_MEDIA_PREVIOUSTRACK << 16);
            NativeMethods.SendMessage(foregroundWindow, NativeMethods.WM_APPCOMMAND, IntPtr.Zero, lParam);
            Logger.WriteInfo("WM_APPCOMMAND previous track sent");
            return true;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to send WM_APPCOMMAND previous track: {ex.Message}");
            return false;
        }
    }
    #endregion
}
