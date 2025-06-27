using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Reflection;
using Windows.Foundation;
using Tabavoco.Services;
using Tabavoco.Platform;
using Tabavoco.Utils;

namespace Tabavoco.Views;

public sealed partial class MiniVolumeWindow : Window
{
    #region Constants
    private const int LOGICAL_WINDOW_WIDTH = 240; // Base width at 100% scale (300px at 125% = 240px logical)
    private const int LOGICAL_WINDOW_WIDTH_WITH_MEDIA = 320; // Extended width with media controls
    private const int LOGICAL_WINDOW_HEIGHT = 40; // Base height at 100% scale (50px at 125% = 40px logical)
    private const int POSITIONING_OFFSET_X = 10;
    private const int POSITIONING_OFFSET_Y = 50;
    private const int TOPMOST_TIMER_INTERVAL_MS = 500;
    private const int VOLUME_SYNC_TIMER_INTERVAL_MS = 1000;
    private const double VOLUME_TOLERANCE = 0.5;
    #endregion

    #region Fields
    private bool _isDragging = false;
    private Point _lastPointerPosition;
    private DispatcherTimer _topmostTimer = new DispatcherTimer();
    private DispatcherTimer _volumeSyncTimer = new DispatcherTimer();
    private bool _isUserInteracting = false;
    private readonly VolumeManager _volumeManager = new VolumeManager();
    private double _dpiScaleFactor = 1.0;
    private readonly ConfigurationService _config = new ConfigurationService();
    #endregion

    #region Constructor & Lifecycle
    public MiniVolumeWindow()
    {
        Logger.WriteInfo("MiniVolumeWindow constructor started");
        InitializeComponent();
        SetupWindowHidden();
        
        // Set app title with version in context menu
        SetAppTitleWithVersion();
        
        // Position window immediately after setup but before showing
        SetInitialWindowPosition();
        
        StartVolumeSyncTimer();
        InitializeStartupMenuState();
        InitializeMediaControls();
        this.Closed += OnWindowClosed;
        Logger.WriteInfo("MiniVolumeWindow constructor completed");
    }

    private void OnWindowClosed(object sender, WindowEventArgs e)
    {
        _volumeManager?.Dispose();
        MediaControlsPanel?.Dispose();
    }

    public new void Activate()
    {
        // Window is already positioned in constructor, just show and activate
        this.AppWindow.Show();
        base.Activate();
    }
    #endregion

    #region App Title & Version
    private void SetAppTitleWithVersion()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var majorMinor = $"{version?.Major}.{version?.Minor}";
            AppTitleMenuItem.Text = $"Tabavoco {majorMinor}";
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to get version: {ex.Message}");
            AppTitleMenuItem.Text = "Tabavoco";
        }
    }
    #endregion

    #region Window Setup & Configuration
    private void SetupWindowHidden()
    {
        // Remove title bar completely in WinUI 3
        this.ExtendsContentIntoTitleBar = true;
        this.SetTitleBar(null);

        // Hide title bar buttons using AppWindow presenter
        if (this.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(true, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }
        
        // Calculate DPI scale factor and set window size accordingly
        CalculateDpiScaleFactor();
        UpdateWindowSize();
        
        // Configure as tool window to hide from taskbar
        Win32WindowManager.ConfigureAsToolWindow(this);
        
        // Start timer to continuously enforce topmost status
        StartTopmostTimer();
    }

    private void CalculateDpiScaleFactor()
    {
        // Get the DPI scale factor using XamlRoot if available
        if (this.Content?.XamlRoot != null)
        {
            _dpiScaleFactor = this.Content.XamlRoot.RasterizationScale;
            Logger.WriteInfo($"Got DPI scale factor from XamlRoot: {_dpiScaleFactor}");
            return;
        }
        
        // Fallback: Use Win32 API to get DPI
        try
        {
            var hwnd = Win32WindowManager.GetWindowHandle(this);
            if (hwnd != IntPtr.Zero)
            {
                uint dpi = Win32WindowManager.GetDpiForWindowHandle(hwnd);
                _dpiScaleFactor = dpi / 96.0; // 96 is the standard DPI
                Logger.WriteInfo($"Got DPI scale factor from Win32 API: {_dpiScaleFactor} (DPI: {dpi})");
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to get DPI from Win32 API: {ex.Message}");
        }
        
        // Final fallback: assume 125% scaling as default since that's what it was designed for
        _dpiScaleFactor = 1.25;
        Logger.WriteInfo($"Using fallback DPI scale factor: {_dpiScaleFactor}");
    }

    private void UpdateWindowSize()
    {
        var baseWidth = _config.ShowMediaControls ? LOGICAL_WINDOW_WIDTH_WITH_MEDIA : LOGICAL_WINDOW_WIDTH;
        var scaledWidth = (int)(baseWidth * _dpiScaleFactor);
        var scaledHeight = (int)(LOGICAL_WINDOW_HEIGHT * _dpiScaleFactor);
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(scaledWidth, scaledHeight));
        Logger.WriteInfo($"Window resized to {scaledWidth}x{scaledHeight} (DPI: {_dpiScaleFactor}, Media Controls: {_config.ShowMediaControls})");
    }
    #endregion

    #region Media Control Management
    private void InitializeMediaControls()
    {
        Logger.WriteInfo("Initializing media controls");
        
        // Set initial visibility and menu state
        var showMediaControls = _config.ShowMediaControls;
        MediaControlsPanel.Visibility = showMediaControls ? Visibility.Visible : Visibility.Collapsed;
        ShowMediaControlsMenuItem.IsChecked = showMediaControls;
        
        Logger.WriteInfo($"Media controls initialized - visible: {showMediaControls}");
    }

    private void OnShowMediaControlsClicked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem menuItem)
        {
            var showControls = menuItem.IsChecked;
            _config.ShowMediaControls = showControls;
            _config.Save();
            
            MediaControlsPanel.Visibility = showControls ? Visibility.Visible : Visibility.Collapsed;
            UpdateWindowSize();
            
            Logger.WriteInfo($"Media controls visibility changed to: {showControls}");
        }
    }

    #endregion

    #region Volume Control Event Handlers
    private void UpdateMuteButtonIcon()
    {
        var isMuted = _volumeManager.IsMuted();
        MuteButton.Content = isMuted ? "🔇" : "🔊";
    }

    private void OnVolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        var volume = (int)e.NewValue;
        _volumeManager.SetVolume(volume);
    }

    private void OnVolumeSliderManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        // User started interacting with volume slider
        _isUserInteracting = true;
    }

    private void OnVolumeSliderManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        // User finished interacting with volume slider
        _isUserInteracting = false;
        
        // Play beep sound when slider manipulation is completed
        try
        {
            // Kinda annoying
            //Console.Beep();
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to play beep sound: {ex.Message}");
        }
    }

    private void OnVolumeSliderPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Disable sync while hovering over slider
        _isUserInteracting = true;
    }

    private void OnVolumeSliderPointerExited(object sender, PointerRoutedEventArgs e)
    {
        // Re-enable sync when no longer hovering
        _isUserInteracting = false;
    }

    private void OnMuteButtonClicked(object sender, RoutedEventArgs e)
    {
        // Toggle mute state
        var currentMuteState = _volumeManager.IsMuted();
        _volumeManager.SetMute(!currentMuteState);
        
        // Update button icon to reflect new state
        UpdateMuteButtonIcon();
    }

    private void OnMuteButtonPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // User started interacting with mute button
        _isUserInteracting = true;
    }

    private void OnMuteButtonPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // User finished interacting with mute button
        _isUserInteracting = false;
    }
    #endregion

    #region Context Menu & Application Event Handlers
    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // Context menu will show automatically due to ContextFlyout
    }

    private void OnExitClicked(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    private void InitializeStartupMenuState()
    {
        // Check if app is currently set to run on startup and update menu item
        RunOnStartupMenuItem.IsChecked = StartupManager.IsStartupEnabled();
    }

    private void OnRunOnStartupClicked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem menuItem)
        {
            var newState = menuItem.IsChecked;
            var success = StartupManager.SetStartupEnabled(newState);
            
            // Verify the operation succeeded and update UI accordingly
            menuItem.IsChecked = StartupManager.IsStartupEnabled();
        }
    }
    #endregion


    private void SetInitialWindowPosition()
    {
        // Check if we have saved position
        var savedLeft = _config.WindowLeft;
        var savedTop = _config.WindowTop;
        
        if (savedLeft > 0 && savedTop > 0)
        {
            // Use saved position
            this.AppWindow.Move(new Windows.Graphics.PointInt32((int)savedLeft, (int)savedTop));
        }
        else
        {
            // Position window at bottom left of screen (default)
            Win32WindowManager.PositionAtBottomLeft(this, POSITIONING_OFFSET_X, POSITIONING_OFFSET_Y);
        }
        
        // Apply extended topmost style
        Win32WindowManager.ApplyTopmostStyle(this);
    }

    private void SaveWindowPosition()
    {
        var position = this.AppWindow.Position;
        _config.WindowLeft = position.X;
        _config.WindowTop = position.Y;
        _config.Save();
    }

    private void StartTopmostTimer()
    {
        // Timer continuously enforces topmost status because Windows taskbar 
        // actively changes our app's z-index when the taskbar is clicked
        _topmostTimer = new DispatcherTimer();
        _topmostTimer.Interval = TimeSpan.FromMilliseconds(TOPMOST_TIMER_INTERVAL_MS);
        _topmostTimer.Tick += (s, e) => Win32WindowManager.ApplyTopmostStyle(this);
        _topmostTimer.Start();
    }

    #region Volume Management & Synchronization
    private void StartVolumeSyncTimer()
    {
        Logger.WriteInfo("Starting volume sync timer");
        // Timer to periodically sync volume and mute state when user isn't interacting
        _volumeSyncTimer = new DispatcherTimer();
        _volumeSyncTimer.Interval = TimeSpan.FromMilliseconds(VOLUME_SYNC_TIMER_INTERVAL_MS);
        _volumeSyncTimer.Tick += OnVolumeSyncTimerTick;
        _volumeSyncTimer.Start();
        Logger.WriteInfo($"Volume sync timer started with {VOLUME_SYNC_TIMER_INTERVAL_MS}ms interval");
    }

    private void OnVolumeSyncTimerTick(object? sender, object e)
    {
        // Only sync if user is not actively interacting with controls
        if (!_isUserInteracting)
        {
            SyncVolumeAndMuteState();
        }
    }

    private void SyncVolumeAndMuteState()
    {
        // Refresh cached state from system
        _volumeManager.RefreshFromSystem();
        
        // Get current cached volume and mute state
        var currentVolume = _volumeManager.GetCurrentVolume();
        var isMuted = _volumeManager.IsMuted();
        
        // Update slider if it doesn't match current system volume
        if (Math.Abs(VolumeSlider.Value - currentVolume) > VOLUME_TOLERANCE)
        {
            // Temporarily remove event handler to prevent feedback loop
            VolumeSlider.ValueChanged -= OnVolumeChanged;
            VolumeSlider.Value = currentVolume;
            VolumeSlider.ValueChanged += OnVolumeChanged;
        }
        
        // Update mute button if state has changed
        var currentButtonText = MuteButton.Content?.ToString();
        var expectedButtonText = isMuted ? "🔇" : "🔊";
        if (currentButtonText != expectedButtonText)
        {
            MuteButton.Content = expectedButtonText;
        }
        
        // Update media control button state if media controls are visible
        if (_config.ShowMediaControls && MediaControlsPanel.Visibility == Visibility.Visible)
        {
            _ = Task.Run(async () => await MediaControlsPanel.RefreshMediaStateAsync());
        }
    }
    #endregion

    #region Window Dragging Event Handlers
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
            SaveWindowPosition();
        }
    }
    #endregion
}