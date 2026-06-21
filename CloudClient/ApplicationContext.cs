namespace CloudClient
{
    public class ApplicationContext
    {
        public bool ApplicationsEnabled { get; set; }
        public bool ServerIsLinux { get; set; }
        public bool ClientIsSocketClient { get; set; }
        public bool CanUseApplications { get; set; }
        public CloudBox.ApplicationContextReason ReasonCode { get; set; }
        public string[] SupportedApps { get; set; } = [];
    }
}
