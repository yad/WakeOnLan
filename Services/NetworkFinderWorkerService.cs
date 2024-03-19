using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public static class Ext
{
    public static void AddUniq(this List<IpMac> that, IpMac ipMac)
    {
        if (!that.Any(i => i.Ip == ipMac.Ip && i.Mac == ipMac.Mac))
        {
            that.Add(ipMac);
        }
    }
}

public class IpMac
{
    public string Ip { get; set; } = "";
    public string Mac { get; set; } = "";
    public bool IsLoopBack { get; internal set; }
}

public class NetworkFinderWorkerService : BackgroundService
{
    public static string LoopBackIp = "127.0.0.1";

    private static IReadOnlyCollection<IpMac> _ipMacMapCache { get; set; } = Array.Empty<IpMac>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _ipMacMapCache = InitializeGetIPsAndMac();
            await Task.Delay(5 * 1000, stoppingToken);
        }
    }

    private static IReadOnlyCollection<IpMac> InitializeGetIPsAndMac()
    {
        // Console.WriteLine("InitializeGetIPsAndMac");
        List<IpMac> map = new List<IpMac>();

        var arpStream = ExecuteCommandLine("arp", "-a");
        if (arpStream != null)
        {
            while (!arpStream.EndOfStream)
            {
                var line = arpStream.ReadLine()?.Trim();
                // Console.WriteLine(line);

                if (line != null)
                {
                    var parts = line.ToUpper().Replace("-", ":").Split(' ').Select(p => p.Trim()).Select(p => p.Trim(['(', ')'])).ToArray();

                    var ip = parts.FirstOrDefault(p => IPAddress.TryParse(p, out IPAddress? _));
                    var mac = parts.FirstOrDefault(p => p.Length == 17 && p.Count(c => c == ':') == 5);

                    if (ip != null && mac != null)
                    {
                        map.AddUniq(new IpMac { Ip = ip, Mac = mac });
                    }
                }
            }
        }

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up))
        {
            var macAddress = string.Join(':', networkInterface.GetPhysicalAddress().ToString().Chunk(2).Select(c => new string(c)));
            if (!string.IsNullOrEmpty(macAddress) && macAddress != "00:00:00:00:00:00")
            {
                var addresses = networkInterface.GetIPProperties().UnicastAddresses;
                foreach (var address in addresses
                    .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork))
                {
                    var ipAddress = address.Address.ToString();
                    if (!string.IsNullOrEmpty(ipAddress) && ipAddress != LoopBackIp)
                    {
                        map.AddUniq(new IpMac { Ip = ipAddress, Mac = macAddress, IsLoopBack = true });
                    }
                }
            }
        }

        return map.ToArray();
    }

    private static StreamReader? ExecuteCommandLine(string file, string arguments = "")
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            FileName = file,
            Arguments = arguments
        };

        Process? process = Process.Start(startInfo);

        return process?.StandardOutput;
    }

    public static string FindIPFromMacAddress(string macAddress)
    {
        IpMac? item = _ipMacMapCache.SingleOrDefault(x => x.Mac == macAddress);
        return item?.Ip ?? "";
    }

    public static string FindMacFromLoopBackIPAddress()
    {
        IpMac? item = _ipMacMapCache.SingleOrDefault(x => x.IsLoopBack);
        return item?.Mac ?? "";
    }

    public static string FindMacFromIPAddress(string ip)
    {
        IpMac? item = _ipMacMapCache.SingleOrDefault(x => x.Ip == ip);
        return item?.Mac ?? "";
    }

    public static string GetHostName(string ipAddress)
    {
        try
        {
            IPHostEntry entry = Dns.GetHostEntry(ipAddress);
            if (entry != null)
            {
                return entry.HostName
                    .Replace(".local", "")
                    .Replace(".home", "")
                    .ToUpper();
            }
        }
        catch (SocketException)
        {
        }

        return "";
    }
}