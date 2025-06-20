using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace Tabavoco;

/// <summary>
/// Manages Windows startup registry entries for the application
/// </summary>
public static class StartupManager
{
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Tabavoco";

    /// <summary>
    /// Checks if the application is currently set to run on Windows startup
    /// </summary>
    /// <returns>True if startup is enabled, false otherwise</returns>
    public static bool IsStartupEnabled()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false))
            {
                return key?.GetValue(AppName) != null;
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to check startup status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Enables or disables the application from running on Windows startup
    /// </summary>
    /// <param name="enable">True to enable startup, false to disable</param>
    /// <returns>True if the operation succeeded, false otherwise</returns>
    public static bool SetStartupEnabled(bool enable)
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
            {
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
                else
                {
                    key?.DeleteValue(AppName, false);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to {(enable ? "enable" : "disable")} startup: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the current executable path for registry entry
    /// </summary>
    /// <returns>The executable path, or empty string if not found</returns>
    private static string GetExecutablePath()
    {
        try
        {
            // First try Environment.ProcessPath (preferred for .NET 6+)
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
            {
                return processPath;
            }
            
            // Fallback to process main module
            var mainModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            if (mainModule?.FileName != null && File.Exists(mainModule.FileName))
            {
                return mainModule.FileName;
            }
            
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