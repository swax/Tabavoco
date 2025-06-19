using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;

namespace TaBaVoCo;

public sealed partial class MiniVolumeWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int HWND_TOPMOST = -1;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int GWL_STYLE = -16;
    private const int WS_SYSMENU = 0x80000;

    public MiniVolumeWindow()
    {
        InitializeComponent();
        SetupWindow();
        this.Activated += OnWindowActivated;
    }

    private void SetupWindow()
    {
        // Remove title bar completely in WinUI 3
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(null);
        
        // Hide title bar buttons using AppWindow presenter
        if (this.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }
        
        // Set window size to 300px width
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(300, 50));
        
        // Position at bottom left corner above taskbar
        var screenHeight = (int)GetSystemMetrics(SM_CYSCREEN);
        
        // Calculate position: 20px from left, 150px from bottom (accounting for taskbar)
        var x = 20;
        var y = screenHeight - 150;
        
        System.Diagnostics.Debug.WriteLine($"Screen height: {screenHeight}, Positioning at: ({x}, {y})");
        
        this.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
        
        // Make window always on top
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        SetWindowPos(hwnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    private void OnVolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (VolumeLabel == null) return;
        
        var volume = (int)e.NewValue;
        VolumeLabel.Text = $"{volume}%";
        
        // Here you would integrate with Windows volume control APIs
        SetSystemVolume(volume);
    }

    private void SetSystemVolume(int volume)
    {
        // Placeholder for actual volume control
        System.Diagnostics.Debug.WriteLine($"Setting volume to {volume}%");
        
        // You would implement Windows Core Audio API calls here
        // For now, this is just a placeholder
    }

    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Context menu will show automatically due to ContextFlyout
    }

    private void OnExitClicked(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs e)
    {
        // Only reposition on first activation
        this.Activated -= OnWindowActivated;
        
        // Delay positioning slightly to ensure window is fully initialized
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += (s, args) => {
            timer.Stop();
            PositionWindow();
        };
        timer.Start();
    }

    private void PositionWindow()
    {
        var screenHeight = (int)GetSystemMetrics(SM_CYSCREEN);
        var x = 20;
        var y = screenHeight - 150; // Moved up from -100 to -150 for better visibility
        
        System.Diagnostics.Debug.WriteLine($"Repositioning window - Screen height: {screenHeight}, Moving to: ({x}, {y})");
        this.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
    }
}