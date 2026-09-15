using NBitcoin;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Cloud
{
    static public partial class Static
    {
        static Static()
        {
            // Initialize OpenUI action based on the operating system
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OpenUI = () => SystemExtra.Util.ExecuteCommand("cmd.exe", "/C " + "start /max " + UIAddress);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OpenUI = () =>
                {
                    var browserOpenResult = SystemExtra.Util.ExecuteSystemCommand($"open {UIAddress}");
                    if (!browserOpenResult.Successful || browserOpenResult.Output.Contains("error", StringComparison.OrdinalIgnoreCase))
                        NotifyUIAddress();
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string? desktopEnvironment = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
                if (CloudSync.Util.DesktopEnvironmentIsStarted || Debugger.IsAttached)
                {
                    OpenUI = () =>
                    {
                        var userName = SystemExtra.Util.CurrentUIUser();
                        string? openBrowserCommand;
                        if (IsWSL)
                        {
                            Environment.SetEnvironmentVariable("DISPLAY", ":0");
                            openBrowserCommand = $"firefox {UIAddress}";
                        }
                        else
                        {
                            openBrowserCommand = $"runuser -l {userName} -c \"xdg-open {UIAddress}\"";
                        }

                        var browserOpenResult = SystemExtra.Util.ExecuteSystemCommand(openBrowserCommand);
                        if (!browserOpenResult.Successful || browserOpenResult.Output.Contains("error", StringComparison.OrdinalIgnoreCase))
                            NotifyUIAddress();
                        else if (Debugger.IsAttached)
                            Debugger.Break(); // Install the browser in WSL: sudo apt install firefox-esr
                    };
                }
            }
        }

        public static void OpenFileExplorer()
        {
            // Open file explorer based on the operating system
            if (CloudSync.Util.DesktopEnvironmentIsStarted && VirtualDiskManager.CloudPathIsReachable())
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Process.Start("explorer", Static.CloudPath);
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Process.Start("xdg-open", Static.CloudPath);
                    var userName = SystemExtra.Util.CurrentUIUser();
                    string? open;
                    open = $"runuser -l {userName} -c \"xdg-open {Static.CloudPath}\"";
                    var openResult = SystemExtra.Util.ExecuteSystemCommand(open);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start("open", Static.CloudPath);
            }
        }

        /// <summary>
        /// Check if the environment is WSL
        /// </summary>
        public static bool IsWSL => (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Debugger.IsAttached && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")));

        /// <summary>
        /// Create a notification indicating the browser page to access the cloud client control panel
        /// </summary>
        static public void NotifyUIAddress()
        {
            var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Notify!";
            var message = $"The cloud control panel is online at {UIAddress}";
            SystemExtra.Util.Notify(appName, NotifyUIAddressMessage);
        }

        /// <summary>
        /// Message indicating browser pages with GUI
        /// </summary>
        static public string NotifyUIAddressMessage => $"The cloud control panel is online at {UIAddress}";

        /// <summary>
        /// Calling this function starts the application's graphical interface (basically the browser with the app's settings page)
        /// </summary>
        public readonly static Action? OpenUI;

        /// <summary>
        /// Router entry point
        /// </summary>
        public static string? EntryPoint;

        /// <summary>
        /// Local cloud directory position (Path)
        /// </summary>
        public static string? CloudPath;

        /// <summary>
        /// Address of User Interface
        /// </summary>
        public static string? UIAddress;

        /// <summary>
        /// Port of User Interface
        /// </summary>
        public static int Port;

        /// <summary>
        /// Single-instance lock held for the whole life of the process. A second launch uses
        /// it to detect that the resident Cloud Client is already running, instead of colliding
        /// on the fixed UI port and stopping with "The port N is busy!" (issue #7).
        /// </summary>
        internal static System.Threading.Mutex? InstanceLock;

        public static SecureStorage.Storage Storage;

        // Check if the current user is an administrator
        public static readonly bool IsAdmin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)
            : Environment.UserName == "root";

        private static CloudClient.Client? _client;

        public static CloudClient.Client? Client
        {
            get { return _client; }
            private set
            {
                _client = value;
                Util.MonitorUpdates(); // Start automatic updating of executables (checks for new updates and proceeds with the update)
            }
        }

        /// <summary>
        /// Returns true if queried when is a suitable time to perform an update
        /// </summary>
        /// <returns></returns>
        public static bool CanUpdate()
        {
            return Client != null && Client.CanRestart() && !BackupManager.BackupIsRunning;
        }

        /// <summary>
        /// Create the client instance 
        /// </summary>
        /// <param name="connectToEntryPoint"></param>
        public static void CreateClient(string? connectToEntryPoint = null)
        {
            Client = new CloudClient.Client(CloudPath, VirtualDiskManager.CloudPathIsReachable());
            if (connectToEntryPoint != null)
            {
                Client.CreateContext(connectToEntryPoint);
            }
            SemaphoreCreateClient.Set();
        }

        internal static ManualResetEvent SemaphoreCreateClient = new ManualResetEvent(false);

        /// <summary>
        /// Create a new account and login to cloud server
        /// </summary>
        /// <param name="qr"></param>
        /// <param name="pin"></param>
        /// <param name="zeroKnowledgeEncryptionMasterKey">If set, zero knowledge proof is enabled, meaning files will be sent encrypted with keys derived from this, and once received, if encrypted, they will be decrypted.</param>
        /// <returns>True for Successful, or false if QR code is not valid (this routine don't check the pin)</returns>
        public static CloudClient.Client.LoginResult Login(string qr, string pin, byte[]? zeroKnowledgeEncryptionMasterKey = null)
        {
            if (Client == null)
                CreateClient();
            var result = Client == null ? CloudClient.Client.LoginResult.WrongQR : Client.Login(qr, pin, EntryPoint, zeroKnowledgeEncryptionMasterKey);
            if (result == CloudClient.Client.LoginResult.Successful)
            {
                AutoStart = true;
                Client?.Context.SecureStorage.Values.Set("QR", qr);
            }
            return result;
        }

        /// <summary>
        /// The QR code you logged in with, or null if you are not logged in to the cloud
        /// </summary>
        public static string? LoggedQr => Client?.Context?.SecureStorage.Values.Get("QR", null);

        /// <summary>
        /// Set to true, starts automatically when the operating system starts
        /// </summary>
        public static bool AutoStart
        {
            get { return SystemExtra.Util.AutoStart; }
            set { SystemExtra.Util.SetAutoStart(value, Static.Port); }
        }

        /// <summary>
        /// Disconnect from the cloud
        /// </summary>
        public static void Logout()
        {
            Client?.Context?.SecureStorage.Values.Set("QR", null);
            Client?.Logout();
            Client = null;
        }

        /// <summary>
        /// Recover an existing account
        /// </summary>
        /// <param name="qr"></param>
        /// <param name="passphrase"></param>
        /// <returns></returns>
        public static CloudClient.Client.LoginResult Restore(string qr, string passphrase)
        {
            if (Client != null)
                CreateClient();
            Client?.Logout();
            return Client?.CreateContext(qr, passphrase: passphrase) != null
                ? CloudClient.Client.LoginResult.Successful
                : CloudClient.Client.LoginResult.WrongQR;
        }



        public static ExtKey GenerateKeyFromPassphrase(string passphrase)
        {
            string cleanedPassphrase = CleanPassphrase(passphrase);
            Mnemonic mnemonic = new Mnemonic(cleanedPassphrase);
            return mnemonic.DeriveExtKey();
        }

        private static string CleanPassphrase(string passphrase)
        {
            string cleaned = passphrase.Replace("\n", " ")
                                       .Replace("\r", " ")
                                       .Replace(",", "")
                                       .Replace("!", "")
                                       .Replace(".", "")
                                       .Trim();
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");
            return cleaned;
        }



#if DEBUG
        public const bool IsDebug = true;
#else
        public const bool IsDebug = false;
#endif
    }
}