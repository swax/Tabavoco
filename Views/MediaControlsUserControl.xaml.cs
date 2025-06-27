using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Tabavoco.Services;
using Tabavoco.Utils;

namespace Tabavoco.Views;

public sealed partial class MediaControlsUserControl : UserControl
{
    #region Fields
    private readonly MediaControlManager _mediaControlManager = new MediaControlManager();
    private bool _disposed = false;
    #endregion


    #region Constructor & Lifecycle
    public MediaControlsUserControl()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Logger.WriteInfo("MediaControlsUserControl loaded - initializing");
        
        // Initialize media control service
        await _mediaControlManager.InitializeAsync();
        
        // Subscribe to state changes
        _mediaControlManager.PlaybackStateChanged += OnPlaybackStateChanged;
        
        // Update initial state
        UpdatePlayPauseButtonIcon();
        
        Logger.WriteInfo("MediaControlsUserControl initialization completed");
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Dispose();
    }
    #endregion

    #region Media Control Event Handlers
    private async void OnPreviousButtonClicked(object sender, RoutedEventArgs e)
    {
        await _mediaControlManager.PreviousTrackAsync();
    }

    private async void OnPlayPauseButtonClicked(object sender, RoutedEventArgs e)
    {
        await _mediaControlManager.PlayPauseAsync();
    }

    private async void OnNextButtonClicked(object sender, RoutedEventArgs e)
    {
        await _mediaControlManager.NextTrackAsync();
    }

    private void OnPlaybackStateChanged(bool isPlaying)
    {
        // Update UI on state change
        PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
        Logger.WriteInfo($"MediaControls: Playback state changed - icon updated to: {(isPlaying ? "pause" : "play")}");
    }
    #endregion

    #region Public Methods
    public void UpdatePlayPauseButtonIcon()
    {
        var isPlaying = _mediaControlManager.IsPlaying;
        PlayPauseButton.Content = isPlaying ? "⏸" : "▶";
        Logger.WriteInfo($"MediaControls: Play/Pause button icon updated - playing: {isPlaying}");
    }

    public async Task RefreshMediaStateAsync()
    {
        await _mediaControlManager.RefreshPlaybackStateAsync();
    }

    public async Task<(string title, string artist)?> GetCurrentMediaInfoAsync()
    {
        return await _mediaControlManager.GetCurrentMediaInfoAsync();
    }

    public bool HasActiveSession()
    {
        return _mediaControlManager.HasActiveSession();
    }
    #endregion

    #region Disposal
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_mediaControlManager != null)
            {
                _mediaControlManager.PlaybackStateChanged -= OnPlaybackStateChanged;
                _mediaControlManager.Dispose();
            }
            _disposed = true;
            Logger.WriteInfo("MediaControlsUserControl disposed");
        }
    }
    #endregion
}