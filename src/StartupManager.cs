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
        catch
        {
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
                    return false;
                }
                else
                {
                    key?.DeleteValue(AppName, false);
                    return true;
                }
            }
        }
        catch
        {
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
            // For packaged WinUI apps, get the actual executable path
            var assembly = Assembly.GetExecutingAssembly();
            var location = assembly.Location;
            
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
            {
                return location;
            }
            
            // Fallback to process main module
            return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
        }
        catch
        {
            return "";
        }
    }
}