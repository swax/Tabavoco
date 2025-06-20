using System;

namespace Tabavoco;

/// <summary>
/// High-level volume management interface that maintains cached state and uses AudioDeviceManager for COM interactions
/// Getting events for volume changes is complicated apparently, so this class provides a simple API for volume and mute management.
/// It caches the current volume and mute state, refreshing from the system on a one second iterval.
/// </summary>
public class VolumeManager : IDisposable
{
    private readonly AudioDeviceManager _audioDeviceManager = new AudioDeviceManager();
    
    // Cache for volume, mute state, and endpoint
    private int? _cachedVolume = null;
    private bool? _cachedMute = null;
    private AudioDeviceManager.IAudioEndpointVolume? _cachedEndpoint = null;
    private DateTime _lastRefresh = DateTime.MinValue;
    

    /// <summary>
    /// Gets the current cached volume as a percentage (0-100)
    /// </summary>
    public int GetCurrentVolume()
    {
        if (_cachedVolume.HasValue)
        {
            return _cachedVolume.Value;
        }
        
        // If no cached value, refresh from system
        RefreshFromSystem();
        return _cachedVolume ?? 0;
    }

    /// <summary>
    /// Sets the system volume to the specified percentage (0-100) and updates cache
    /// </summary>
    public void SetVolume(int volumePercent)
    {
        Logger.WriteInfo($"SetVolume called with: {volumePercent}%");
        EnsureEndpointCached();
        if (_cachedEndpoint == null) 
        {
            Logger.WriteError("SetVolume failed: No audio endpoint available");
            return;
        }
        
        var level = Math.Max(0, Math.Min(100, volumePercent)) / 100.0f;
        var success = _audioDeviceManager.SetMasterVolumeScalar(_cachedEndpoint, level);
        if (!success)
        {
            Logger.WriteError($"SetVolume failed - Level: {level:F2}");
        }
        
        // Update cached value immediately
        _cachedVolume = volumePercent;
    }

    /// <summary>
    /// Gets whether the system is currently muted from cache
    /// </summary>
    public bool IsMuted()
    {
        if (_cachedMute.HasValue)
        {
            return _cachedMute.Value;
        }
        
        // If no cached value, refresh from system
        RefreshFromSystem();
        return _cachedMute ?? false; // Default fallback
    }

    /// <summary>
    /// Sets the system mute state and updates cache
    /// </summary>
    public void SetMute(bool mute)
    {
        Logger.WriteInfo($"SetMute called with: {mute}");
        EnsureEndpointCached();
        if (_cachedEndpoint == null) 
        {
            Logger.WriteError("SetMute failed: No audio endpoint available");
            return;
        }
        
        var success = _audioDeviceManager.SetMuteState(_cachedEndpoint, mute);
        if (!success)
        {
            Logger.WriteError($"SetMute failed");
        }
        
        // Update cached value immediately
        _cachedMute = mute;
    }

    /// <summary>
    /// Refreshes cached values from the system - should be called periodically
    /// </summary>
    public void RefreshFromSystem()
    {
        Logger.WriteInfo("RefreshFromSystem called");
        _cachedEndpoint = _audioDeviceManager.GetCurrentVolumeEndpoint();
        if (_cachedEndpoint == null) 
        {
            Logger.WriteError("RefreshFromSystem: No audio endpoint available");
            _cachedVolume = null;
            _cachedMute = null;
            return;
        }
        
        var level = _audioDeviceManager.GetMasterVolumeScalar(_cachedEndpoint);
        _cachedVolume = level.HasValue ? (int)(level.Value * 100) : null;
        Logger.WriteInfo($"RefreshFromSystem: Volume level = {_cachedVolume}%");
        
        var muteState = _audioDeviceManager.GetMuteState(_cachedEndpoint);
        _cachedMute = muteState ?? false;
        Logger.WriteInfo($"RefreshFromSystem: Mute state = {_cachedMute}");
        
        _lastRefresh = DateTime.Now;
    }

    /// <summary>
    /// Ensures endpoint is cached, refreshing if necessary
    /// </summary>
    private void EnsureEndpointCached()
    {
        if (_cachedEndpoint == null)
        {
            RefreshFromSystem();
        }
    }

    /// <summary>
    /// Disposes the AudioDeviceManager
    /// </summary>
    public void Dispose()
    {
        _audioDeviceManager?.Dispose();
    }
}