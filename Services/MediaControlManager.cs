using System;
using System.Threading.Tasks;
using Windows.Media.Control;
using Tabavoco.Platform;
using Tabavoco.Utils;

namespace Tabavoco.Services;

/// <summary>
/// High-level media control service - fast Win32 WM_APPCOMMAND primary, SMTC fallback.
/// Maintains state, optimistic updates, event-driven UI notifications for responsive control.
/// </summary>
public class MediaControlManager : IDisposable
{

    #region Fields
    private readonly SmtcApiWrapper _smtcManager = new SmtcApiWrapper();
    private bool _disposed = false;
    private bool _isPlaying = false;
    private DateTime _lastCommandTime = DateTime.MinValue;
    #endregion

    #region Events
    public event Action<bool>? PlaybackStateChanged;
    #endregion

    #region Initialization
    public async Task<bool> InitializeAsync()
    {
        Logger.WriteInfo("MediaControlManager initializing");
        
        // Initialize SMTC for state monitoring
        var success = await _smtcManager.InitializeAsync();
        if (success)
        {
            // Get initial state
            await RefreshPlaybackStateAsync();
            Logger.WriteInfo($"MediaControlManager initialized - initial state: {(_isPlaying ? "playing" : "paused")}");
        }
        else
        {
            Logger.WriteError("SMTC initialization failed - media controls will use commands only");
        }
        
        return true; // Always return true since we can still send commands via WM_APPCOMMAND
    }
    #endregion

    #region Public Media Control Methods
    public async Task PlayPauseAsync()
    {
        Logger.WriteInfo($"PlayPause command - current state: {(_isPlaying ? "playing" : "paused")}");
        
        // Optimistically update state immediately
        _isPlaying = !_isPlaying;
        _lastCommandTime = DateTime.Now;
        
        // Notify UI immediately
        PlaybackStateChanged?.Invoke(_isPlaying);
        Logger.WriteInfo($"Optimistically updated state to: {(_isPlaying ? "playing" : "paused")}");
        
        // Send fast command via WM_APPCOMMAND
        var success = Win32WindowManager.SendMediaPlayPause();
        if (!success)
        {
            // Fallback to SMTC
            try
            {
                await _smtcManager.PlayPauseAsync();
                Logger.WriteInfo("Fallback SMTC command sent");
            }
            catch (Exception ex)
            {
                Logger.WriteError($"Fallback SMTC command also failed: {ex.Message}");
                // Revert optimistic state change
                _isPlaying = !_isPlaying;
                PlaybackStateChanged?.Invoke(_isPlaying);
            }
        }
    }

    public async Task NextTrackAsync()
    {
        Logger.WriteInfo("Next track command");
        
        var success = Win32WindowManager.SendMediaNextTrack();
        if (!success)
        {
            // Fallback to SMTC
            await _smtcManager.NextTrackAsync();
        }
    }

    public async Task PreviousTrackAsync()
    {
        Logger.WriteInfo("Previous track command");
        
        var success = Win32WindowManager.SendMediaPreviousTrack();
        if (!success)
        {
            // Fallback to SMTC
            await _smtcManager.PreviousTrackAsync();
        }
    }
    #endregion

    #region State Management
    public bool IsPlaying => _isPlaying;

    public Task RefreshPlaybackStateAsync()
    {
        // This is a failsafe for if the play/pause states get out of sync in the app
        // Querying windows for the state can be delayed up to like 10 seconds
        // So after 10 seconds, if there still is a discrepancy, then use what windows says
        var timeSinceCommand = DateTime.Now - _lastCommandTime;
        if (timeSinceCommand.TotalMilliseconds < 10 * 1000)
        {
            Logger.WriteInfo($"Skipping state refresh - {timeSinceCommand.TotalMilliseconds}ms since last command");
            return Task.CompletedTask;
        }

        try
        {
            var actualState = _smtcManager.IsPlaying();
            if (actualState != _isPlaying)
            {
                _isPlaying = actualState;
                PlaybackStateChanged?.Invoke(_isPlaying);
                Logger.WriteInfo($"State refreshed from SMTC: {(_isPlaying ? "playing" : "paused")}");
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to refresh playback state: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }

    public async Task<(string title, string artist)?> GetCurrentMediaInfoAsync()
    {
        try
        {
            return await _smtcManager.GetCurrentMediaInfoAsync();
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to get media info: {ex.Message}");
            return null;
        }
    }

    public bool HasActiveSession()
    {
        try
        {
            return _smtcManager.HasActiveSession();
        }
        catch
        {
            return false;
        }
    }
    #endregion

    #region Disposal
    public void Dispose()
    {
        if (!_disposed)
        {
            _smtcManager?.Dispose();
            _disposed = true;
            Logger.WriteInfo("MediaControlManager disposed");
        }
    }
    #endregion
}