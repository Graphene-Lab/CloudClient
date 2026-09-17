using System.IO;
using System.Runtime.InteropServices;
using GitHubAppSync;

namespace Cloud
{
    public static class Util
    {
        /// <summary>
        /// Updates are published as GitHub Releases of this repository, so the client pulls them
        /// from there instead of from a private store server.
        /// </summary>
        private const string UpdateRepository = "Graphene-Lab/CloudClient";

        /// <summary>
        /// The release channel this running build belongs to, which selects the asset pair the
        /// updater reads. The framework-dependent ("portable") build ships Cloud.dll next to the
        /// executable; the self-contained single-file builds do not, and update from their own RID.
        /// This mirrors the detection the install scripts already use.
        /// </summary>
        private static string UpdateChannel =>
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Cloud.dll"))
                ? "portable"
                : RuntimeInformation.RuntimeIdentifier;

        /// <summary>
        /// Check for updates and update the current application with the latest version published
        /// on this channel's GitHub release, when that version is newer than the running one.
        /// </summary>
        static public string UpdateApplication() =>
            Update.CheckAndUpdate(UpdateRepository, Static.CanUpdate, UpdateChannel).ToString();

        /// <summary>
        /// Start a timer that periodically checks for app updates from this channel's GitHub release.
        /// </summary>
        static public void MonitorUpdates() =>
            Update.MonitoringUpdates(UpdateRepository, Static.CanUpdate, UpdateChannel);
    }
}
