using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace Tabavoco;

/// <summary>
/// Direct Windows SMTC API wrapper - stateless, async-heavy, provides detailed media info.
/// Uses GlobalSystemMediaTransportControlsSessionManager for precise session control.
/// </summary>
public class SmtcApiWrapper : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private bool _disposed = false;
    
    #region Helper Methods
    private GlobalSystemMediaTransportControlsSession? GetActiveSession()
    {
        return _sessionManager?.GetCurrentSession();
    }
    
    private async Task<bool> ExecuteSessionCommandAsync(string operation, Func<GlobalSystemMediaTransportControlsSession, Task<bool>> command)
    {
        var session = GetActiveSession();
        if (session == null)
        {
            Logger.WriteError($"No active media session found for {operation}");
            return false;
        }
        
        try
        {
            var success = await command(session);
            Logger.WriteInfo($"Attempted to {operation}: {success}");
            return success;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to {operation}: {ex.Message}");
            return false;
        }
    }
    
    private T ExecuteSessionOperation<T>(string operation, Func<GlobalSystemMediaTransportControlsSession, T> operation_func, T defaultValue = default!)
    {
        try
        {
            var session = GetActiveSession();
            if (session == null) return defaultValue;
            
            return operation_func(session);
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to {operation}: {ex.Message}");
            return defaultValue;
        }
    }
    #endregion
    
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            Logger.WriteInfo($"SmtcApiWrapper initialized: {_sessionManager != null}");
            return _sessionManager != null;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to initialize media control: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> PlayPauseAsync()
    {
        return await ExecuteSessionCommandAsync("play/pause media", async session =>
        {
            var playbackInfo = session.GetPlaybackInfo();
            if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                return await session.TryPauseAsync();
            }
            else
            {
                return await session.TryPlayAsync();
            }
        });
    }
    
    public async Task<bool> NextTrackAsync()
    {
        return await ExecuteSessionCommandAsync("skip to next track", async session => await session.TrySkipNextAsync());
    }
    
    public async Task<bool> PreviousTrackAsync()
    {
        return await ExecuteSessionCommandAsync("skip to previous track", async session => await session.TrySkipPreviousAsync());
    }
    
    public async Task<(string title, string artist)?> GetCurrentMediaInfoAsync()
    {
        var session = GetActiveSession();
        if (session == null) return null;
        
        try
        {
            var properties = await session.TryGetMediaPropertiesAsync();
            if (properties != null)
            {
                return (properties.Title ?? "Unknown", properties.Artist ?? "Unknown");
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to get current media info: {ex.Message}");
        }
        
        return null;
    }
    
    public bool HasActiveSession()
    {
        return ExecuteSessionOperation("check active session", _ => true, false);
    }
    
    public bool IsPlaying()
    {
        return ExecuteSessionOperation("get playback status", session =>
        {
            var playbackInfo = session.GetPlaybackInfo();
            return playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        }, false);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _sessionManager = null;
            _disposed = true;
            Logger.WriteInfo("SmtcApiWrapper disposed");
        }
    }
}