using System;
using System.Runtime.InteropServices;

namespace Tabavoco;

/// <summary>
/// Manages system volume using Windows Core Audio API
/// </summary>
public static class VolumeManager
{
    #region COM Interfaces
    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator
    {
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int NotImpl1();
        [PreserveSig]
        int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig]
        int NotImpl1();
        [PreserveSig]
        int NotImpl2();
        [PreserveSig]
        int GetChannelCount(out int pnChannelCount);
        [PreserveSig]
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig]
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        [PreserveSig]
        int GetMasterVolumeLevel(out float pfLevelDB);
        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);
        [PreserveSig]
        int SetChannelVolumeLevel(int nChannel, float fLevelDB, ref Guid pguidEventContext);
        [PreserveSig]
        int SetChannelVolumeLevelScalar(int nChannel, float fLevel, ref Guid pguidEventContext);
        [PreserveSig]
        int GetChannelVolumeLevel(int nChannel, out float pfLevelDB);
        [PreserveSig]
        int GetChannelVolumeLevelScalar(int nChannel, out float pfLevel);
        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] Boolean bMute, ref Guid pguidEventContext);
        [PreserveSig]
        int GetMute(out bool pbMute);
    }
    #endregion

    #region Enums
    private enum DataFlow
    {
        Render,
        Capture,
        All
    }

    private enum Role
    {
        Console,
        Multimedia,
        Communications
    }
    #endregion

    private static IAudioEndpointVolume? _volumeEndpoint;
    private static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

    /// <summary>
    /// Initializes the volume manager by getting the default audio endpoint
    /// </summary>
    private static void Initialize()
    {
        if (_volumeEndpoint != null) return;

        try
        {
            var deviceEnumerator = new MMDeviceEnumerator() as IMMDeviceEnumerator;
            if (deviceEnumerator?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out IMMDevice device) == 0)
            {
                var guid = IID_IAudioEndpointVolume;
                if (device?.Activate(ref guid, 0, IntPtr.Zero, out object o) == 0)
                {
                    _volumeEndpoint = o as IAudioEndpointVolume;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize volume endpoint: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current system volume as a percentage (0-100)
    /// </summary>
    public static int GetCurrentVolume()
    {
        Initialize();
        
        if (_volumeEndpoint == null) return 50; // Default fallback
        
        try
        {
            _volumeEndpoint.GetMasterVolumeLevelScalar(out float level);
            return (int)(level * 100);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get volume: {ex.Message}");
            return 50; // Default fallback
        }
    }

    /// <summary>
    /// Sets the system volume to the specified percentage (0-100)
    /// </summary>
    public static void SetVolume(int volumePercent)
    {
        Initialize();
        
        if (_volumeEndpoint == null) return;
        
        try
        {
            var level = Math.Max(0, Math.Min(100, volumePercent)) / 100.0f;
            var guid = Guid.Empty;
            _volumeEndpoint.SetMasterVolumeLevelScalar(level, ref guid);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set volume: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets whether the system is currently muted
    /// </summary>
    public static bool IsMuted()
    {
        Initialize();
        
        if (_volumeEndpoint == null) return false;
        
        try
        {
            _volumeEndpoint.GetMute(out bool isMuted);
            return isMuted;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get mute status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets the system mute state
    /// </summary>
    public static void SetMute(bool mute)
    {
        Initialize();
        
        if (_volumeEndpoint == null) return;
        
        try
        {
            var guid = Guid.Empty;
            _volumeEndpoint.SetMute(mute, ref guid);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set mute: {ex.Message}");
        }
    }
}