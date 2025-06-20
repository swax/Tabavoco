
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
            // Check for single instance before initializing
            bool isNewInstance;
            singleInstanceMutex = new Mutex(true, "Tabavoco_SingleInstance", out isNewInstance);
            
            if (!isNewInstance)
            {
                // Another instance is already running
                ShowAlreadyRunningDialog();
                Environment.Exit(0);
                return;
            }

            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new MiniVolumeWindow();
            window.Activate();
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
        /// Clean up mutex on app exit
        /// </summary>
        ~App()
        {
            singleInstanceMutex?.ReleaseMutex();
            singleInstanceMutex?.Dispose();
        }
    }
}
