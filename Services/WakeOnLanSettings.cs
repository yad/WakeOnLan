public class WakeOnLanSettings : List<WakeOnLan>
{
}

public class Service
{
    public string Label { get; set; } = "";
    public string Link { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Process { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public string Arguments { get; set; } = "";
    public int Port { get; set; }
    public bool OnDemand { get; set; }
    public string Api { get; internal set; } = "";
    public ApiStatus ApiStatus { get; internal set; }
}

    public enum ApiStatus
    {
        Down,
        Loading,
        Up
    }

public class WakeOnLan
{
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public string MAC { get; set; } = "";
    public int Port { get; set; }

    public string IP { get; internal set; } = "";
    public string HostName { get; internal set; } = "";
    public bool IsServerUp { get; internal set; }

    public string Api { get; internal set; } = "";
    public bool IsApiUp { get; internal set; }

    public List<Service> Services { get; set; } = new List<Service>();
}
