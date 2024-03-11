using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public class NetworkFinder
{
    public static string LoopBackIp = "127.0.0.1";

    private Lazy<IReadOnlyCollection<IPAndMac>> _map = new Lazy<IReadOnlyCollection<IPAndMac>>(InitializeGetIPsAndMac);

    private static IReadOnlyCollection<IPAndMac> InitializeGetIPsAndMac()
    {
        // Console.WriteLine("InitializeGetIPsAndMac");
        List<IPAndMac> map = new List<IPAndMac>();

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
                        map.Add(new IPAndMac { Ip = ip, Mac = mac });
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
                        map.Add(new IPAndMac { Ip = ipAddress, Mac = macAddress, IsLoopBack = true });
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

    public string FindIPFromMacAddress(string macAddress)
    {
        IPAndMac? item = _map.Value.SingleOrDefault(x => x.Mac == macAddress);
        return item?.Ip ?? "";
    }

    public string FindMacFromLoopBackIPAddress()
    {
        IPAndMac? item = _map.Value.SingleOrDefault(x => x.IsLoopBack);
        return item?.Mac ?? "";
    }

    public string FindMacFromIPAddress(string ip)
    {
        IPAndMac? item = _map.Value.SingleOrDefault(x => x.Ip == ip);
        return item?.Mac ?? "";
    }

    private class IPAndMac
    {
        public string Ip { get; set; } = "";
        public string Mac { get; set; } = "";
        public bool IsLoopBack { get; internal set; }
    }

    public string GetHostName(string ipAddress)
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