using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.ApplicationModel;
using Tabavoco.Utils;

namespace Tabavoco.Services;

/// <summary>
/// Manages application startup registration.
/// Uses StartupTask API when running as MSIX, registry when unpackaged.
/// </summary>
public static class StartupManager
{
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Tabavoco";
    private const string StartupTaskId = "TabavocoStartup";

    public static bool IsStartupEnabled()
    {
        try
        {
            if (IsPackaged())
            {
                var task = Task.Run(() => StartupTask.GetAsync(StartupTaskId).AsTask()).GetAwaiter().GetResult();
                return task.State == StartupTaskState.Enabled;
            }

            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to check startup status: {ex.Message}");
            return false;
        }
    }

    public static bool SetStartupEnabled(bool enable)
    {
        try
        {
            if (IsPackaged())
                return SetStartupEnabledPackaged(enable);

            return SetStartupEnabledUnpackaged(enable);
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to {(enable ? "enable" : "disable")} startup: {ex.Message}");
            return false;
        }
    }

    private static bool SetStartupEnabledPackaged(bool enable)
    {
        var task = Task.Run(() => StartupTask.GetAsync(StartupTaskId).AsTask()).GetAwaiter().GetResult();

        if (enable)
        {
            if (task.State == StartupTaskState.DisabledByUser)
            {
                Logger.WriteError("Startup was disabled by user in Task Manager and cannot be re-enabled programmatically.");
                return false;
            }
            var newState = Task.Run(() => task.RequestEnableAsync().AsTask()).GetAwaiter().GetResult();
            return newState == StartupTaskState.Enabled;
        }

        task.Disable();
        return true;
    }

    private static bool SetStartupEnabledUnpackaged(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
        if (enable)
        {
            var exePath = GetExecutablePath();
            if (!string.IsNullOrEmpty(exePath))
            {
                key?.SetValue(AppName, exePath);
                return true;
            }
            Logger.WriteError("Failed to enable startup: Could not determine executable path");
            return false;
        }

        key?.DeleteValue(AppName, false);
        return true;
    }

    private static bool IsPackaged()
    {
        try
        {
            _ = Package.Current.Id;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetExecutablePath()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                return processPath;

            var mainModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            if (mainModule?.FileName != null && File.Exists(mainModule.FileName))
                return mainModule.FileName;

            Logger.WriteError("Failed to determine executable path: All methods returned null or non-existent paths");
            return "";
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to get executable path: {ex.Message}");
            return "";
        }
    }
}
