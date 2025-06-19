using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.Foundation;

namespace Tabavoco;

public sealed partial class MiniVolumeWindow : Window
{
    private bool _isDragging = false;
    private Point _lastPointerPosition;
    private DispatcherTimer _topmostTimer = new DispatcherTimer();
    private DispatcherTimer _volumeSyncTimer = new DispatcherTimer();
    private bool _isUserInteracting = false;
    private readonly VolumeManager _volumeManager = new VolumeManager();

    public MiniVolumeWindow()
    {
        InitializeComponent();
        SetupWindow();
        StartVolumeSyncTimer();
        this.Activated += OnWindowActivated;
        this.Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object sender, WindowEventArgs e)
    {
        _volumeManager?.Dispose();
    }

    private void SetupWindow()
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
        
        // Set window size to 300px width
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(300, 50));
        
        // Configure as tool window to hide from taskbar
        Win32WindowManager.ConfigureAsToolWindow(this);
        
        // Start timer to continuously enforce topmost status
        StartTopmostTimer();
    }

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

    private void OnVolumeSliderPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // User started interacting with volume slider
        _isUserInteracting = true;
    }

    private void OnVolumeSliderPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // User finished interacting with volume slider
        _isUserInteracting = false;
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
        // Position window at bottom left of screen
        Win32WindowManager.PositionAtBottomLeft(this, 10, 50);
        
        // Apply extended topmost style
        Win32WindowManager.ApplyTopmostStyle(this);
    }

    private void StartTopmostTimer()
    {
        // Timer continuously enforces topmost status because Windows taskbar 
        // actively changes our app's z-index when the taskbar is clicked
        _topmostTimer = new DispatcherTimer();
        _topmostTimer.Interval = TimeSpan.FromMilliseconds(500); // Check every 500ms
        _topmostTimer.Tick += (s, e) => Win32WindowManager.ApplyTopmostStyle(this);
        _topmostTimer.Start();
    }

    private void StartVolumeSyncTimer()
    {
        // Timer to periodically sync volume and mute state when user isn't interacting
        _volumeSyncTimer = new DispatcherTimer();
        _volumeSyncTimer.Interval = TimeSpan.FromSeconds(1); // Check every second
        _volumeSyncTimer.Tick += OnVolumeSyncTimerTick;
        _volumeSyncTimer.Start();
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
        if (Math.Abs(VolumeSlider.Value - currentVolume) > 0.5)
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