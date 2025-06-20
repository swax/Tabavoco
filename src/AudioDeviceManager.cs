using System;
using System.Runtime.InteropServices;

namespace Tabavoco;

/// <summary>
/// Manages Windows Core Audio API COM interactions for audio device access
/// </summary>
public class AudioDeviceManager : IDisposable
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
    public interface IAudioEndpointVolume
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

    private static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

    /// <summary>
    /// Gets the current default audio endpoint (always fresh to handle device switching)
    /// </summary>
    /// <returns>Audio endpoint volume interface or null if failed</returns>
    public IAudioEndpointVolume? GetCurrentVolumeEndpoint()
    {
        try
        {
            var deviceEnumerator = new MMDeviceEnumerator() as IMMDeviceEnumerator;
            if (deviceEnumerator?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out IMMDevice device) == 0)
            {
                var guid = IID_IAudioEndpointVolume;
                if (device?.Activate(ref guid, 0, IntPtr.Zero, out object o) == 0)
                {
                    return o as IAudioEndpointVolume;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to get current volume endpoint: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Gets the master volume level as a scalar (0.0 - 1.0)
    /// </summary>
    /// <param name="endpoint">Audio endpoint to query</param>
    /// <returns>Volume level or null if failed</returns>
    public float? GetMasterVolumeScalar(IAudioEndpointVolume endpoint)
    {
        try
        {
            var result = endpoint.GetMasterVolumeLevelScalar(out float level);
            return result == 0 ? level : null;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to get volume scalar: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sets the master volume level as a scalar (0.0 - 1.0)
    /// </summary>
    /// <param name="endpoint">Audio endpoint to modify</param>
    /// <param name="level">Volume level (0.0 - 1.0)</param>
    /// <returns>True if successful</returns>
    public bool SetMasterVolumeScalar(IAudioEndpointVolume endpoint, float level)
    {
        try
        {
            var guid = Guid.Empty;
            var result = endpoint.SetMasterVolumeLevelScalar(level, ref guid);
            return result == 0;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to set volume scalar: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the mute state
    /// </summary>
    /// <param name="endpoint">Audio endpoint to query</param>
    /// <returns>Mute state or null if failed</returns>
    public bool? GetMuteState(IAudioEndpointVolume endpoint)
    {
        try
        {
            var result = endpoint.GetMute(out bool isMuted);
            return result == 0 ? isMuted : null;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to get mute state: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sets the mute state
    /// </summary>
    /// <param name="endpoint">Audio endpoint to modify</param>
    /// <param name="mute">Mute state to set</param>
    /// <returns>True if successful</returns>
    public bool SetMuteState(IAudioEndpointVolume endpoint, bool mute)
    {
        try
        {
            var guid = Guid.Empty;
            var result = endpoint.SetMute(mute, ref guid);
            return result == 0;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to set mute state: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        // COM objects are garbage collected automatically in .NET
        // No explicit cleanup needed for this implementation
    }
}