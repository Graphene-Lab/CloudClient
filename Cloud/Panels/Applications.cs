using System.Diagnostics;

namespace Cloud.Panels
{
    /// <summary>
    /// Panel for launching cloud applications in local terminal mode
    /// </summary>
    public static class Applications
    {
        static Applications()
        {

        }

        internal static bool Hidden => false;

        const string loginRequired = "Log in to your personal CloudBox to use remote applications.";
        const string notConnected = "The client is not connected to the cloud.";
        const string timeoutError = "Timeout error: The cloud is currently unreachable.";
        const string applicationContextUnavailable = "Application context is not available from this cloud server.";
        const string applicationsAvailable = "Remote applications are available.";
        const string applicationsDisabled = "Remote applications are available only with your personal CloudBox.";
        const string serverNotLinux = "Remote applications require a Linux CloudBox.";
        const string clientNotSupported = "Remote applications are available only from the desktop CloudClient.";
        const string noSupportedApps = "No supported applications are installed on this CloudBox.";


        /// <summary>
        /// Current status of the connection with the cloud
        /// </summary>
        [DebuggerHidden] // Don't break at throw
        public static string Status
        {
            get
            {
                return GetApplicationPanelState().Message;
            }
        }

        /// <summary>
        /// Select a cloud application to run in a local virtual environment
        /// </summary>
        public static string[]? ApplicationToRun
        {
            get
            {
                var state = GetApplicationPanelState();
                return state.CanUseApplications ? state.Context?.SupportedApps : null;
            }
        }

        private static bool ApplicationToRun_Hidden => !GetApplicationPanelState().CanUseApplications;
        private static bool ExecuteApplication_Hidden => ApplicationToRun_Hidden;
        private static bool Retry_Hidden => !GetApplicationPanelState().CanRetry;

        /// <summary>
        /// Retry loading the application context from the cloud
        /// </summary>
        public static string Retry()
        {
            return GetApplicationPanelState(forceRefresh: true).Message;
        }

        /// <summary>
        /// Run the selected application
        /// </summary>
        private static void OnSelectApplicationToRun(int id)
        {
            SelectedApplication = ApplicationToRun?[id];
        }

        /// <summary>
        /// Virtually run the application on the cloud. The execution leaves no traces on the local computer, technically you are working directly in the cloud, without performing any operations on the local machine.
        /// </summary>
        public static void ExecuteApplication()
        {
            var state = GetApplicationPanelState(forceRefresh: true);
            if (state.TimedOut)
                throw new Exception(timeoutError);
            if (!state.CanUseApplications)
                throw new Exception(state.Message);
            if (SelectedApplication == null)
                throw new Exception("No applications selected!");
            if (state.Context != null && Array.IndexOf(state.Context.SupportedApps, SelectedApplication) < 0)
                throw new Exception("Selected application is not available.");
            if (Static.Client == null || !Static.Client.IsConnected)
                throw new Exception(notConnected);
            Static.Client.StartApplication(SelectedApplication);
        }
        private static string? SelectedApplication;

        private sealed class ApplicationPanelState
        {
            public CloudClient.ApplicationContext? Context { get; init; }
            public bool TimedOut { get; init; }
            public bool CanRetry { get; init; }
            public string Message { get; init; } = applicationContextUnavailable;
            public bool CanUseApplications => Context?.CanUseApplications == true;
        }

        private static ApplicationPanelState? CachedState;
        private static object? CachedClient;
        private static object? CachedSync;
        private static object? CachedServerCloud;
        private static bool? CachedIsLogged;
        private static bool? CachedIsConnected;

        private static ApplicationPanelState GetApplicationPanelState(bool forceRefresh = false)
        {
            var client = Static.Client;
            var sync = client?.Sync;
            var serverCloud = client?.ServerCloud;
            var isLogged = client?.IsLogged == true;
            var isConnected = client?.IsConnected == true;

            var keyChanged =
                !ReferenceEquals(CachedClient, client) ||
                !ReferenceEquals(CachedSync, sync) ||
                !ReferenceEquals(CachedServerCloud, serverCloud) ||
                CachedIsLogged != isLogged ||
                CachedIsConnected != isConnected;

            if (keyChanged)
                SelectedApplication = null;

            if (!forceRefresh && !keyChanged && CachedState != null)
                return CachedState!;

            var state = LoadApplicationPanelState(client, isLogged, isConnected);
            if (!state.CanUseApplications || state.Context == null || SelectedApplication == null || Array.IndexOf(state.Context.SupportedApps, SelectedApplication) < 0)
                SelectedApplication = null;

            CachedClient = client;
            CachedSync = sync;
            CachedServerCloud = serverCloud;
            CachedIsLogged = isLogged;
            CachedIsConnected = isConnected;
            CachedState = state;
            return state;
        }

        private static ApplicationPanelState LoadApplicationPanelState(CloudClient.Client? client, bool isLogged, bool isConnected)
        {
            if (client == null || client.Sync == null || !isLogged)
                return new ApplicationPanelState { Message = loginRequired };
            if (!isConnected)
                return new ApplicationPanelState { Message = notConnected };

            var context = client.GetApplicationContext(out var timedOut);
            if (timedOut)
                return new ApplicationPanelState { TimedOut = true, CanRetry = true, Message = timeoutError };
            return context == null
                ? new ApplicationPanelState { CanRetry = true, Message = applicationContextUnavailable }
                : new ApplicationPanelState { Context = context, Message = ReasonMessage(context.ReasonCode) };
        }

        private static string ReasonMessage(CloudBox.ApplicationContextReason reasonCode)
        {
            return reasonCode switch
            {
                CloudBox.ApplicationContextReason.Available => applicationsAvailable,
                CloudBox.ApplicationContextReason.ApplicationsDisabled => applicationsDisabled,
                CloudBox.ApplicationContextReason.ServerNotLinux => serverNotLinux,
                CloudBox.ApplicationContextReason.ClientNotSupported => clientNotSupported,
                CloudBox.ApplicationContextReason.NoSupportedApps => noSupportedApps,
                _ => applicationContextUnavailable
            };
        }
    }
}
