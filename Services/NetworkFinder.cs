using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public class NetworkFinder
{
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
                    var parts = line.ToUpper().Replace("-", ":").Split(' ').Select(p => p.Trim()).Select(p => p.Trim(new[] { '(', ')' })).ToArray();

                    var ip = parts.FirstOrDefault(p => IPAddress.TryParse(p, out IPAddress? _));
                    var mac = parts.FirstOrDefault(p => p.Length == 17 && p.Count(c => c == ':') == 5);

                    if (ip != null && mac != null)
                    {
                        map.Add(new IPAndMac { IP = ip, MAC = mac });
                    }
                }
            }
        }

        // Console.WriteLine("=========");

        var macAddresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .Where(macAddress => !string.IsNullOrEmpty(macAddress))
            .Where(macAddress => macAddress != "00:00:00:00:00:00");

        foreach (var macAddress in macAddresses)
        {
            map.Add(new IPAndMac { IP = "127.0.0.1", MAC = string.Join(':', macAddress.Chunk(2).Select(c => new string(c))) });
        };

        // foreach (var item in map)
        // {
        //     Console.WriteLine($"{item.IP} {item.MAC}");
        // }

        // Console.WriteLine("=========");

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
        IPAndMac? item = _map.Value.SingleOrDefault(x => x.MAC == macAddress);
        return item?.IP ?? "";
    }

    public string FindMacFromIPAddress(string ip)
    {
        IPAndMac? item = _map.Value.SingleOrDefault(x => x.IP == ip);
        return item?.MAC ?? "";
    }

    private class IPAndMac
    {
        public string? IP { get; set; }
        public string? MAC { get; set; }
    }

    public string GetHostName(string ipAddress)
    {
        try
        {
            IPHostEntry entry = Dns.GetHostEntry(ipAddress);
            if (entry != null)
            {
                return entry.HostName;
            }
        }
        catch (SocketException)
        {
        }

        return "";
    }
}