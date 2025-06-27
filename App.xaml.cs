global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
using Tabavoco.Views;
using Tabavoco.Utils;

namespace Tabavoco
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window window = Window.Current;
        private Mutex? singleInstanceMutex;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            try
            {
                Logger.WriteInfo("App constructor started");
                
                // Add global exception handlers
                this.UnhandledException += OnUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
                
                Logger.WriteInfo("Exception handlers registered");
                
                // Check for single instance before initializing
                bool isNewInstance;
                singleInstanceMutex = new Mutex(true, "Tabavoco_SingleInstance", out isNewInstance);
                
                Logger.WriteInfo($"Single instance check: isNewInstance = {isNewInstance}");
                
                if (!isNewInstance)
                {
                    // Another instance is already running
                    Logger.WriteInfo("Another instance already running, showing dialog and exiting");
                    ShowAlreadyRunningDialog();
                    Environment.Exit(0);
                    return;
                }

                Logger.WriteInfo("Calling InitializeComponent");
                this.InitializeComponent();
                Logger.WriteInfo("App constructor completed successfully");
            }
            catch (Exception ex)
            {
                Logger.WriteError($"FATAL: App constructor failed: {ex}");
                ShowCrashDialog($"App failed to start: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            try
            {
                Logger.WriteInfo("OnLaunched called");
                window ??= new MiniVolumeWindow();
                Logger.WriteInfo("MiniVolumeWindow created, calling Activate");
                window.Activate();
                Logger.WriteInfo("Window activated successfully");
            }
            catch (Exception ex)
            {
                Logger.WriteError($"FATAL: OnLaunched failed: {ex}");
                ShowCrashDialog($"Failed to launch window: {ex.Message}");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Shows a dialog indicating the app is already running and exits
        /// </summary>
        private void ShowAlreadyRunningDialog()
        {
            // Use Win32 MessageBox since WinUI dialogs require a window to be already created
            MessageBox(
                IntPtr.Zero,
                "Tabavoco is already running. Only one instance can run at a time.",
                "Tabavoco Already Running",
                0x40); // MB_ICONINFORMATION
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        /// <summary>
        /// Global exception handler for unhandled exceptions
        /// </summary>
        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Logger.WriteError($"UNHANDLED EXCEPTION: {e.Exception}");
            ShowCrashDialog($"Unhandled exception: {e.Exception.Message}");
            e.Handled = true;
            Environment.Exit(1);
        }

        /// <summary>
        /// Domain-level exception handler
        /// </summary>
        private void OnDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Logger.WriteError($"DOMAIN UNHANDLED EXCEPTION: {ex?.ToString() ?? e.ExceptionObject?.ToString() ?? "Unknown"}");
            ShowCrashDialog($"Critical error: {ex?.Message ?? "Unknown error"}");
            Environment.Exit(1);
        }

        /// <summary>
        /// Shows a crash dialog with error details
        /// </summary>
        private void ShowCrashDialog(string message)
        {
            try
            {
                MessageBox(
                    IntPtr.Zero,
                    $"Tabavoco has encountered an error and needs to close.\n\n{message}\n\nCheck tabavoco-debug.log for details.",
                    "Tabavoco Error",
                    0x10); // MB_ICONERROR
            }
            catch
            {
                // If even the crash dialog fails, just exit silently
            }
        }

        /// <summary>
        /// Clean up mutex on app exit
        /// </summary>
        ~App()
        {
            singleInstanceMutex?.ReleaseMutex();
            singleInstanceMutex?.Dispose();
        }
    }
}
