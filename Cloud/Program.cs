using Cloud;
using Cloud.Components;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;

#region preliminary operations

//#if !DEBUG
AppDomain.CurrentDomain.UnhandledException += CloudSync.Util.UnhandledException; //it catches application errors in order to prepare a log of the events that cause the crash
AppDomain.CurrentDomain.ProcessExit += (s, e) =>
{
    // Unlock semaphore in exit request
    Static.SemaphoreCreateClient.Set();
    Static.Client?.Quit();
};
//#endif


Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory); // The UI fails if you launch the app from an external path without this command line

if (!SystemExtra.Util.IsAdmin())
{
#if DEBUG
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        // From MacOS run VS or Rider by the follow command line:
        // sudo /Applications/Visual\ Studio.app/Contents/MacOS/VisualStudio
        // sudo /Applications/Rider.app/Contents/MacOS/rider
        Debugger.Break();
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
        // From Linux in wsl edit the file /etc/wsl.conf: (nano /etc/wsl.conf) and add:
        // [user]
        // default = root
        Debugger.Break();
    }
#endif
    var errorRole = "The application requires the administrator role!";
    Console.WriteLine(errorRole);
    CloudSync.Util.RecordError(new Exception(errorRole));
    CloudSync.Util.DisallowRestartApplicationOnEnd = false;
    Environment.Exit(1);
    return;
}

if (!SpinWait.SpinUntil(() => !SystemExtra.Util.AppIsAlreadyRunning(), TimeSpan.FromSeconds(5)))
{
    Debugger.Break();
    Console.WriteLine("The application is already running!");
    CloudSync.Util.DisallowRestartApplicationOnEnd = false;
    Environment.Exit(1);
    return;
}

#endregion


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// ===== REGISTER THEME SERVICE =====
builder.Services.AddSingleton<Cloud.Services.ThemeService>();

var app = builder.Build();

var configuration = app.Configuration;

Static.CloudPath = CloudBox.CloudBox.GetCloudPath((string)configuration.GetValue(typeof(string), "CloudPath", null), false);

EncryptedMessaging.Context.UsePullPushDataChannel = (bool)configuration.GetValue(typeof(bool), "UsePullPushDataChannel", true); // Use pull-push data channel for encrypted messaging

//#if DEBUG
//if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
//{
//    var cloudTestPath = @"C:\TestCloud";
//    if (Directory.Exists(cloudTestPath))
//        Static.CloudPath = cloudTestPath;
//}
//#endif

if (!new FileInfo(Static.CloudPath).Directory.Exists)
{
    CloudBox.CloudBox.ResetAppData();
    throw new Exception($"ERROR: Invalid cloud path!, path: {Static.CloudPath}"); // Restart!
}

Func<int, bool> PortIsRearchable = (port) =>
{
    using var tcpClient = new TcpClient();
    try
    {
        var result = tcpClient.BeginConnect(IPAddress.Loopback.ToString(), port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
        tcpClient.EndConnect(result);
    }
    catch (Exception ex) { return false; }
    return true;

};

Static.EntryPoint = (string?)configuration.GetValue(typeof(string), "EntryPoint", null);

if (Debugger.IsAttached) // In debug mode, If the server is running locally then set localhost as entry point to use the local server
{
    if (PortIsRearchable(5001))
        Static.EntryPoint = IPAddress.Loopback.ToString(); // Connect to local server for debug!
}

if (string.IsNullOrEmpty(Static.EntryPoint))
    Static.EntryPoint = "server.tc0.it"; // default entry point 

var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Split(';');

if (!Debugger.IsAttached)
{
    var settingsUrls = (string)configuration.GetValue(typeof(string), "Urls", null); // Used for release
    if (urls == null)
    {
        urls = settingsUrls?.Split(';');
        if (urls != null)
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", string.Join(';', urls));
    }
}

Debug.Assert(urls != null, nameof(urls) + " != null");
var firstUrlParts = urls[0].Split(':');
Static.Port = int.Parse(firstUrlParts[2]);
#if DEBUG
// if port i busy Increment port of +1 in debug mode
if (PortIsRearchable(Static.Port))
{
    Static.Port++;
    firstUrlParts[2] = Static.Port.ToString();
    urls[0] = string.Join(':', firstUrlParts);
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", string.Join(';', urls));
}
#endif
Static.UIAddress = urls[0];
foreach (var url in urls)
{
    app.Urls.Add(url);
}
Static.Storage = new SecureStorage.Storage(Static.UIAddress);

var virtualDisk = (bool)configuration.GetValue(typeof(bool), "VirtualDisk", false);
var cloudPath = new DirectoryInfo(Static.CloudPath);
if (virtualDisk)
{
    var virtualDiskRepository = "";
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        virtualDiskRepository = "/Volumes";
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        virtualDiskRepository = Environment.SystemDirectory;
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        virtualDiskRepository = "/var/lib";
    virtualDiskRepository = Path.Combine([virtualDiskRepository, ".$Sys"]);
    var hashPath = CloudSync.Util.HashFileName(Static.CloudPath, true).GetBytes().ToHex();
    var extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".vhdx" : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".sparsebundle" : ".encrypted";
    VirtualDiskManager.VirtualDiskFullFileName = Path.Combine(virtualDiskRepository, hashPath + extension);
    static bool Exists(string fullName) => File.Exists(fullName) || Directory.Exists(fullName);
    var virtualDiskFileInfo = new FileInfo(VirtualDiskManager.VirtualDiskFullFileName);
    var vdPassword = VirtualDiskManager.VirtualDiskPassword;


    if (false)
    {   // ====== RESET ======
        Static.Logout();
        SystemExtra.Util.UnmountVirtualDiskWindow(VirtualDiskManager.VirtualDiskFullFileName);
        var pathInfo = new DirectoryInfo(Static.CloudPath);
        pathInfo.Attributes &= ~FileAttributes.ReadOnly;
        pathInfo.Delete(true);
        File.Delete(VirtualDiskManager.VirtualDiskFullFileName);
        Console.WriteLine("Reset done!");
        Debugger.Break(); // RESET DONE       
        Environment.Exit(0);
    }

    if (!Exists(VirtualDiskManager.VirtualDiskFullFileName) && !Exists(Path.ChangeExtension(VirtualDiskManager.VirtualDiskFullFileName, ".sys")))
    {
        // Create Encrypted Virtual Disk
        VirtualDiskManager.CreateVirtualDisk(vdPassword);
    }
    else if (!VirtualDiskManager.VirtualDiskIsMounted && !VirtualDiskManager.VirtualDiskIsLocked)
    {
        // Mount Encrypted Virtual Disk
        if (vdPassword == null && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Debugger.Break(); // No password configured!  Investigate!
        else
            SystemExtra.Util.MountVirtualDisk(VirtualDiskManager.VirtualDiskFullFileName, Static.CloudPath, vdPassword);
    }
}
else
{
    cloudPath.Create();
    cloudPath.Refresh();
    if (!cloudPath.Exists)
    {
        throw new Exception("Invalid cloud path: '" + cloudPath.FullName + "'");
    }
}

#if RELEASE
if (Static.EntryPoint != null && Static.EntryPoint.Contains("test")) { Console.WriteLine("WARNING: Test entry point in use: Change entry point in application settings before deployment!"); };

if (SystemExtra.Util.IsService(Static.Port) ==  false)
{
    // is a first startup of application - then, set the service
    SystemExtra.Util.SetAutoStart(true, Static.Port);
    Static.NotifyUIAddress();
    Console.WriteLine(Static.NotifyUIAddressMessage);
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        CloudSync.Util.DisallowRestartApplicationOnEnd = false;
        Environment.Exit(0);
        return;
    }
}
#endif

var lastEntryPoint = CloudBox.CloudBox.LastEntryPoint(); // last entry point is a value that is set if the client was previously logged in to the server
if (Debugger.IsAttached && lastEntryPoint != null && lastEntryPoint != Static.EntryPoint)
    lastEntryPoint = null; // If the server has been changed in debug, we simulate the log out

if (lastEntryPoint != null)
{
    // If you were already logged in then:
    Static.CreateClient(lastEntryPoint); 
}

BackupManager.Initialize(Static.CloudPath);

Func<bool> portIsAvailable = () =>
{
    try
    {
        using var client = new TcpClient("localhost", Static.Port);
        Thread.Sleep(500);
        return false;
    }
    catch (SocketException)
    {
        return true;
    }
};

if (!SpinWait.SpinUntil(portIsAvailable, TimeSpan.FromSeconds(180)))
{
    Debugger.Break();
    throw new Exception("The port " + Static.Port + " is busy!");
}
else
{
    if (lastEntryPoint == null || Debugger.IsAttached)
    {
        // Open the browser
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(() =>
        {
            if (!SpinWait.SpinUntil(() => !portIsAvailable(), TimeSpan.FromSeconds(180)))
            {
                Debugger.Break();
                throw new Exception("Web interface not found on port " + Static.Port);
            }
            else
            {
                // if (!Debugger.IsAttached) // In debug mode the browser is open automatically by the system
                Static.OpenUI?.Invoke();
            }
        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed


        if (Directory.Exists(CloudSync.Util.DesktopPath()))
        {
            try
            {
                var htmlFile = Path.Combine(CloudSync.Util.DesktopPath(), "Cloud Settings " + Static.Port + ".htm");
                var html = File.ReadAllText("redirect.html").Replace("{address}", Static.UIAddress);
                File.WriteAllText(htmlFile, html);
                SystemExtra.Util.Chmod(777, htmlFile);
                var desktopUser = SystemExtra.Util.CurrentUIUser();
                var userInfo = SystemExtra.Util.GetUserInfo(desktopUser);
                if (userInfo != null)
                {
                    SystemExtra.Util.Chown(htmlFile, userInfo.Value.pw_uid, userInfo.Value.pw_gid);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

//app.Run();

new Thread(() => app.Run()).Start();

if (Static.Client == null)
    Static.SemaphoreCreateClient.WaitOne();
Static.Client?.EnableOSFeatures(Static.OpenUI);