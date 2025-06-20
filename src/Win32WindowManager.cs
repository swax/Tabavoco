using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace Tabavoco;

/// <summary>
/// Wraps Win32 API calls for window management, providing a clean interface
/// for positioning, styling, and managing window behavior
/// </summary>
public static class Win32WindowManager
{
    #region Win32 API Imports
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);
    #endregion

    #region Win32 Constants
    private const int HWND_TOPMOST = -1;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_SYSMENU = 0x80000;
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    #endregion

    /// <summary>
    /// Gets the screen dimensions
    /// </summary>
    public static (int width, int height) GetScreenDimensions()
    {
        var width = (int)GetSystemMetrics(SM_CXSCREEN);
        var height = (int)GetSystemMetrics(SM_CYSCREEN);
        return (width, height);
    }

    /// <summary>
    /// Configures a window to behave as a tool window that doesn't appear in taskbar
    /// </summary>
    public static void ConfigureAsToolWindow(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        
        // Set as tool window and remove from taskbar
        SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
    }

    /// <summary>
    /// Applies topmost styling to ensure window stays above all other windows including taskbar
    /// </summary>
    public static void ApplyTopmostStyle(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        
        // Dual approach: Extended style is more persistent, SetWindowPos has immediate effect
        // Standard WinUI 3 IsAlwaysOnTop doesn't guarantee positioning above taskbar
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, (exStyle | WS_EX_TOPMOST | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);
        
        SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, 
                     SWP_NOACTIVATE | SWP_NOSIZE | SWP_NOMOVE);
    }

    /// <summary>
    /// Positions the window at the bottom left of the screen with specified offset
    /// </summary>
    public static void PositionAtBottomLeft(Window window, int offsetX = 10, int offsetY = 50)
    {
        var (_, screenHeight) = GetScreenDimensions();
        var x = offsetX;
        var y = screenHeight - offsetY;
        
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
        return GetDpiForWindow(hwnd);
    }
}