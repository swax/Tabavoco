using System;

namespace Tabavoco;

/// <summary>
/// High-level volume management interface that maintains cached state and uses AudioDeviceManager for COM interactions
/// Getting events for volume changes is complicated apparently, so this class provides a simple API for volume and mute management.
/// It caches the current volume and mute state, refreshing from the system on a one second iterval.
/// </summary>
public static class VolumeManager
{
    private static readonly AudioDeviceManager _audioDeviceManager = new AudioDeviceManager();
    
    // Cache for volume, mute state, and endpoint
    private static int? _cachedVolume = null;
    private static bool? _cachedMute = null;
    private static AudioDeviceManager.IAudioEndpointVolume? _cachedEndpoint = null;
    private static DateTime _lastRefresh = DateTime.MinValue;

    /// <summary>
    /// Gets the current cached volume as a percentage (0-100)
    /// </summary>
    public static int GetCurrentVolume()
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
    public static void SetVolume(int volumePercent)
    {
        EnsureEndpointCached();
        if (_cachedEndpoint == null) return;
        
        var level = Math.Max(0, Math.Min(100, volumePercent)) / 100.0f;
        _audioDeviceManager.SetMasterVolumeScalar(_cachedEndpoint, level);
        
        // Update cached value immediately
        _cachedVolume = volumePercent;
    }

    /// <summary>
    /// Gets whether the system is currently muted from cache
    /// </summary>
    public static bool IsMuted()
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
    public static void SetMute(bool mute)
    {
        EnsureEndpointCached();
        if (_cachedEndpoint == null) return;
        
        _audioDeviceManager.SetMuteState(_cachedEndpoint, mute);
        
        // Update cached value immediately
        _cachedMute = mute;
    }

    /// <summary>
    /// Refreshes cached values from the system - should be called periodically
    /// </summary>
    public static void RefreshFromSystem()
    {
        _cachedEndpoint = _audioDeviceManager.GetCurrentVolumeEndpoint();
        if (_cachedEndpoint == null) 
        {
            _cachedVolume = null;
            _cachedMute = null;
            return;
        }
        
        var level = _audioDeviceManager.GetMasterVolumeScalar(_cachedEndpoint);
        _cachedVolume = level.HasValue ? (int)(level.Value * 100) : null;
        
        var muteState = _audioDeviceManager.GetMuteState(_cachedEndpoint);
        _cachedMute = muteState ?? false;
        
        _lastRefresh = DateTime.Now;
    }

    /// <summary>
    /// Ensures endpoint is cached, refreshing if necessary
    /// </summary>
    private static void EnsureEndpointCached()
    {
        if (_cachedEndpoint == null)
        {
            RefreshFromSystem();
        }
    }
}