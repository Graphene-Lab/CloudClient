using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CloudSync;
namespace CloudClient
{
    /// <summary>
    /// Set of commands that allow you to carry out some operations on the cloud and access special functions
    /// </summary>
    public partial class Client : CloudBox.CloudBox
    {
        public static readonly Dictionary<Command, uint> Counter = []; // used for statistical purposes(saves numbers of executed commands)
        /// <summary>
        /// Generally this method is called when the server responds to a command
        /// </summary>
        /// <param name="userId">Id of the client which sent command </param>
        /// <param name="responseToCommand"> command sent by the client (SaveData, LoadData, LoadAllData, DeleteData, PushNotification)</param>
        /// <param name="parameters"> list of parameters needed for the command:
        public void OnServerCommand(ulong userId, Command responseToCommand, List<byte[]> parameters)
        {
            if (!Counter.ContainsKey(responseToCommand))
                Counter[responseToCommand] = 0;
            Counter[responseToCommand] += 1;
            lock (this)
            {
                try
                {
                    switch (responseToCommand)
                    {
                        case Command.GetEncryptedQR:
                            var encryptedPubKey = parameters[0];
                            var serverPublicKey = CloudBox.EncryptionXorAB.Decrypt(EncryptedQR.EncryptionKey, encryptedPubKey);
                            EncryptedQR = null;
                            ConnectToServer(serverPublicKey.ToBase64());
                            // CloudBox.Login(qrCode.ToBase64(), CloudBox.Context.SecureStorage.Values.Get("pin", null));
                            break;
                        case Command.GetSSHAccess:
                            var ToExecute = parameters[0];
                            if (parameters[1].Length == 0)
                            {
                                CommandFeedback(responseToCommand, "The cloud server does not have administrator powers to give external access to the client");
                            }
                            else
                            {
                                var password = parameters[1].ToHex();
                                var localIP = parameters[2];
                                var publicIP = parameters[3];
                                var myLocalIp = Util.GetLocalIpAddress().GetAddressBytes();
                                string key = null;
                                string keyHex = null;
                                bool keyIsStored = false;
                                if (parameters.Count > 4)
                                {
                                    key = Encoding.ASCII.GetString(parameters[4]);
                                    keyHex = parameters[4].ToHex();
                                    keyIsStored = Context.SecureStorage.Values.Get(keyHex, false);
                                }
                                connectedTo = default;

                                if (ToExecute.Length > 1) // if is 1 byte, then is a error message
                                {
                                    var programToExecute = Encoding.UTF8.GetString(ToExecute);
                                    try
                                    {
                                        if (new IPAddress(publicIP).ToString() == Util.GetPublicIpAddress()?.ToString())
                                        {
                                            //if (!Debugger.IsAttached)
                                            //    isLocal = true;
                                            //else
                                            //{
                                            if (localIP[0] == myLocalIp[0] && localIP[1] == myLocalIp[1] && localIP[2] == myLocalIp[2])
                                                connectedTo = Connection.localIP;
                                            else
                                            {
                                                var virtualMachine = false;
                                                if (virtualMachine)
                                                    connectedTo = Connection.localhost; // The cloud run in Virtual Machine in localhost (for testing in debug mode)
                                                else
                                                    connectedTo = Connection.router;
                                            }
                                            //}
                                        }
                                    }
                                    catch (Exception) { }
                                    try
                                    {
                                        if (connectedTo == Connection.publicIP)
                                            SshIP = publicIP;
                                        else if (connectedTo == Connection.localIP)
                                            SshIP = localIP;
                                        else if (connectedTo == Connection.localhost)
                                            SshIP = IPAddress.Loopback.GetAddressBytes();
                                        else if (connectedTo == Connection.router)
                                            SshIP = localIP;
                                        var ipToConnect = new IPAddress(SshIP).ToString();
                                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                        {
                                            var puttyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "extra");
                                            // var putty = Path.Combine(puttyPath, "putty.exe");
                                            // var fileName = "putty.exe";

                                            var fileName = "plink.exe";
                                            var putty = Path.Combine(puttyPath, fileName);
                                            var xming = Path.Combine(puttyPath, "Xming.exe");
                                            // var run = Path.Combine(puttyPath, "run.txt");
                                            // File.WriteAllText(run, programToExecute);
                                            if (File.Exists(putty) && File.Exists(xming))
                                            {
                                                if (Process.GetProcessesByName("Xming").Length == 0)
                                                {
                                                    Process.Start(xming, ":0 -clipboard -multiwindow -dpi 108");
                                                }
                                                SSHClientProcess = new Process();
                                                SSHClientProcess.StartInfo.RedirectStandardInput = true;
                                                SSHClientProcess.StartInfo.RedirectStandardOutput = true;
                                                SSHClientProcess.StartInfo.EnvironmentVariables["PATH"] = puttyPath;
                                                SSHClientProcess.StartInfo.FileName = fileName;
                                                if (!keyIsStored)
                                                    SSHClientProcess.StartInfo.Arguments = "-ssh " + " -X -pw " + password + " " + Context.My.Id + "@" + ipToConnect + " \"" + programToExecute + "\"";
                                                else
                                                    SSHClientProcess.StartInfo.Arguments = "-batch -ssh " + " -X -pw " + password + " " + Context.My.Id + "@" + ipToConnect + " \"" + programToExecute + "\"";

                                                SSHClientProcess.StartInfo.UseShellExecute = false;
#if RELEASE
                                                SSHClientProcess.StartInfo.CreateNoWindow = true;
#endif
                                                SSHClientProcess.Start();
                                                if (!keyIsStored)
                                                {
                                                    StreamWriter myStreamWriter = SSHClientProcess.StandardInput;
                                                    myStreamWriter.WriteLine("y");
                                                    myStreamWriter.WriteLine("");
                                                    myStreamWriter.Close();
                                                    Context?.SecureStorage?.Values.Set(keyHex, true);
                                                }
                                                //SSHClientProcess.BeginOutputReadLine();
                                            }
                                        }

                                    }
                                    catch (Exception) { }
                                }
                                else
                                {
                                    CommandFeedback(responseToCommand, "Application not supported in the cloud");
                                }
                            }
                            GetSSHAccessSemaphore.Set();
                            break;
                        case Command.GetSupportedApps:
                            _SupportedApps = SplitApps(parameters[0]);
                            GetSupportedAppsSemaphore?.Set(); // unlock semaphore
                            break;
                        case Command.GetApplicationContext:
                            _ApplicationContextResponse = new ApplicationContext
                            {
                                ApplicationsEnabled = parameters.Count > 0 && BitConverter.ToBoolean(parameters[0], 0),
                                ServerIsLinux = parameters.Count > 1 && BitConverter.ToBoolean(parameters[1], 0),
                                ClientIsSocketClient = parameters.Count > 2 && BitConverter.ToBoolean(parameters[2], 0),
                                CanUseApplications = parameters.Count > 3 && BitConverter.ToBoolean(parameters[3], 0),
                                ReasonCode = parameters.Count > 4 && parameters[4].Length > 0
                                    ? (CloudBox.ApplicationContextReason)parameters[4][0]
                                    : CloudBox.ApplicationContextReason.Unknown,
                                SupportedApps = parameters.Count > 5 ? SplitApps(parameters[5]) : Array.Empty<string>()
                            };
                            GetApplicationContextSemaphore?.Set(); // unlock semaphore
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // There is probably an error with system dependent files (files open or access denied)
                    Debug.WriteLine(ex.Message);
                }
            }
        }
        private void CommandFeedback(Command command, string feedback)
        {
            Feedbacks[command] = feedback;
            OnCommandFeedback?.Invoke(command, feedback);
        }
        private Dictionary<Command, string> Feedbacks = [];
        public Action<Command, string> OnCommandFeedback { get; set; }

        // ============== StartProgram ===========================
        private Process SSHClientProcess;
        byte[] SshIP;
        enum Connection { publicIP, localIP, router, localhost }
        Connection connectedTo;
        //bool isLocal = false;
        //bool isLocalhost = false;

        /// <summary>
        /// Requires SSH access to the cloud to run a specific program locally
        /// </summary>
        /// <param name="programToExecute">Name of program to execute</param>
        public void StartApplication(string programToExecute)
        {
            lock (Feedbacks)
            {
                connectedTo = default;
                //isLocal = default;
                //isLocalhost = default;
                SSHClientProcess = default;
                Feedbacks.Remove(Command.GetSSHAccess);
                if (SendCommand(ServerCloud, Command.GetSSHAccess, [Encoding.UTF8.GetBytes(programToExecute)]))
                {
                    GetSSHAccessSemaphore = new AutoResetEvent(false);
                    if (!GetSSHAccessSemaphore.WaitOne(20000))
                        throw new Exception("Timeout error: The cloud is probably unreachable!");
                    else if (SSHClientProcess != null)
                    {
                        if (!SSHClientProcess.HasExited)
                            SSHClientProcess.WaitForExit(10000);
                        if (SSHClientProcess.HasExited)
                        {
                            if (connectedTo == Connection.publicIP)
                                throw new Exception("The cloud is not reachable at IP address " + new IPAddress(SshIP).ToString() + ", if it is connected to a router try redirecting port 22");
                            else
                                throw new Exception("The cloud is not reachable in the local network at IP address: " + new IPAddress(SshIP).ToString());
                        }
                    }
                }
                else
                    throw new Exception("The client is not connected to the cloud!");
                if (Feedbacks.TryGetValue(Command.GetSSHAccess, out var feedback))
                    throw new Exception(feedback);
            }
        }
        private AutoResetEvent GetSSHAccessSemaphore;

        // ============== GetSupportedApps ===========================

        // Legacy command. New code should call GetApplicationContext()
        // because it returns both availability and the app list in one response.
        private AutoResetEvent GetSupportedAppsSemaphore;
        private string[] _SupportedApps;
        private bool GetSupportedAppsRunning;
        public string[] GetSupportedApps(out bool timeout)
        {
            timeout = false;
            if (_SupportedApps != null)
                return _SupportedApps;
            else
            {
                if (!GetSupportedAppsRunning)
                {
                    GetSupportedAppsRunning = true;
                    if (SendCommand(ServerCloud, Command.GetSupportedApps, null))
                    {
                        GetSupportedAppsSemaphore = new AutoResetEvent(false);
                        timeout = !GetSupportedAppsSemaphore.WaitOne(10000);
                    }
                    GetSupportedAppsRunning = false;
                }
            }
            return _SupportedApps;
        }

        // ============== GetApplicationContext ===========================

        private AutoResetEvent GetApplicationContextSemaphore;
        private ApplicationContext _ApplicationContextResponse;
        private bool GetApplicationContextRunning;
        public ApplicationContext GetApplicationContext(out bool timeout)
        {
            timeout = false;
            _ApplicationContextResponse = null;
            if (!GetApplicationContextRunning)
            {
                GetApplicationContextRunning = true;
                if (SendCommand(ServerCloud, Command.GetApplicationContext, null))
                {
                    GetApplicationContextSemaphore = new AutoResetEvent(false);
                    timeout = !GetApplicationContextSemaphore.WaitOne(10000);
                }
                GetApplicationContextRunning = false;
            }
            return _ApplicationContextResponse;
        }

        private static string[] SplitApps(byte[] bytes)
        {
            var apps = Encoding.UTF8.GetString(bytes);
            return string.IsNullOrEmpty(apps) ? [] : apps.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        }

    }
}
