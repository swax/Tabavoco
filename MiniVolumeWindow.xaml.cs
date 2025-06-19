using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace Tabavoco;

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

    private bool _isDragging = false;
    private Point _lastPointerPosition;

    public MiniVolumeWindow()
    {
        InitializeComponent();
        SetupWindow();
        this.Activated += OnWindowActivated;
    }

    private void SetupWindow()
    {
        // Use CompactOverlay presenter for truly borderless window
        //this.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.CompactOverlay);
        
        // Remove title bar completely in WinUI 3
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(null);

        // Hide title bar buttons using AppWindow presenter
        if (this.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(true, false);
            presenter.IsResizable = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }
        
        // Set window size to 300px width
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(300, 50));
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
            SetInitialWindowPosition();
        };
        timer.Start();
    }

    private void SetInitialWindowPosition()
    {
        var screenHeight = (int)GetSystemMetrics(SM_CYSCREEN);
        var x = 10;
        var y = screenHeight - 50; // Moved up from -100 to -150 for better visibility
        
        System.Diagnostics.Debug.WriteLine($"Repositioning window - Screen height: {screenHeight}, Moving to: ({x}, {y})");
        this.AppWindow.Move(new Windows.Graphics.PointInt32(x, y));
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var grid = sender as Grid;
        if (grid != null)
        {
            _isDragging = true;
            _lastPointerPosition = e.GetCurrentPoint(grid).Position;
            grid.CapturePointer(e.Pointer);
        }
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            var grid = sender as Grid;
            if (grid != null)
            {
                var currentPosition = e.GetCurrentPoint(grid).Position;
                var deltaX = currentPosition.X - _lastPointerPosition.X;
                var deltaY = currentPosition.Y - _lastPointerPosition.Y;

                var currentPos = this.AppWindow.Position;
                var newX = currentPos.X + (int)deltaX;
                var newY = currentPos.Y + (int)deltaY;

                this.AppWindow.Move(new Windows.Graphics.PointInt32(newX, newY));

                System.Diagnostics.Debug.WriteLine($"Dragging window to: ({newX}, {newY})");
            }
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            var grid = sender as Grid;
            grid?.ReleasePointerCapture(e.Pointer);
        }
    }
}