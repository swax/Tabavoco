using System;
using System.IO;
using System.Reflection;

namespace Tabavoco.Utils;

/// <summary>
/// Simple logging utility that writes to both Debug output and a log file
/// </summary>
public static class Logger
{
    private static readonly object _lockObject = new object();
    private static readonly string _logFilePath;
    
    // Configuration: Set to true to enable info logging, false to only log errors
    private const bool ENABLE_INFO_LOGGING = false;

    static Logger()
    {
        try
        {
            // Get the directory where the executable is located
            var exeLocation = Assembly.GetExecutingAssembly().Location;
            var exeDirectory = Path.GetDirectoryName(exeLocation) ?? Environment.CurrentDirectory;
            _logFilePath = Path.Combine(exeDirectory, "tabavoco-debug.log");
        }
        catch
        {
            // Fallback to current directory if assembly location fails
            _logFilePath = Path.Combine(Environment.CurrentDirectory, "tabavoco-debug.log");
        }
    }

    /// <summary>
    /// Writes an info message to both Debug output and log file (only if info logging is enabled)
    /// </summary>
    /// <param name="message">Info message to log</param>
    public static void WriteInfo(string message)
    {
#pragma warning disable CS0162 // Unreachable code detected
        if (!ENABLE_INFO_LOGGING) return;
        
        WriteMessage("INFO", message);
#pragma warning restore CS0162 // Unreachable code detected
    }

    /// <summary>
    /// Writes an error message to both Debug output and log file (always logged)
    /// </summary>
    /// <param name="message">Error message to log</param>
    public static void WriteError(string message)
    {
        WriteMessage("ERROR", message);
    }

    /// <summary>
    /// Internal method to write a message with level prefix
    /// </summary>
    /// <param name="level">Log level (INFO, ERROR)</param>
    /// <param name="message">Message to log</param>
    private static void WriteMessage(string level, string message)
    {
        var timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        
        // Write to Debug output (works in development)
        System.Diagnostics.Debug.WriteLine(timestampedMessage);
        
        // Write to log file (works in published exe)
        lock (_lockObject)
        {
            try
            {
                File.AppendAllText(_logFilePath, timestampedMessage + Environment.NewLine);
            }
            catch
            {
                // Silently ignore file write errors to prevent logging from breaking the app
            }
        }
    }

    /// <summary>
    /// Gets the path to the log file
    /// </summary>
    /// <returns>Full path to the log file</returns>
    public static string GetLogFilePath() => _logFilePath;
}