using System.Diagnostics;
using System.Net.NetworkInformation;

public class NetworkFinder
{
    private Lazy<IReadOnlyCollection<IPAndMac>> _map = new Lazy<IReadOnlyCollection<IPAndMac>>(InitializeGetIPsAndMac);

    private static IReadOnlyCollection<IPAndMac> InitializeGetIPsAndMac()
    {
        // Console.WriteLine("InitializeGetIPsAndMac");

        var arpStream = ExecuteCommandLine("arp", "-a");
        List<string> result = new List<string>();
        while (!arpStream.EndOfStream)
        {
            var line = arpStream.ReadLine().Trim();
            // Console.WriteLine(line);
            result.Add(line);
        }

        // Console.WriteLine("=========");

        var map = result
            .Where(l => l.Length > 0 && int.TryParse(l[0].ToString(), out int _)) // skip headers
            .Select(x =>
                {
                    string[] parts = x.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    return new IPAndMac { IP = parts[0].Trim(), MAC = parts[1].Trim().ToUpper().Replace("-", ":") };
                })
            .ToList();

        var macAddresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .Where(macAddress => !string.IsNullOrEmpty(macAddress));

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

    private static StreamReader ExecuteCommandLine(string file, string arguments = "")
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.CreateNoWindow = true;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.FileName = file;
        startInfo.Arguments = arguments;

        Process process = Process.Start(startInfo);

        return process.StandardOutput;
    }    

    public string? FindIPFromMacAddress(string macAddress)
    {
        IPAndMac? item = _map.Value.SingleOrDefault(x => x.MAC == macAddress);
        return item?.IP;
    }

    public string? FindMacFromIPAddress(string ip)
    {
        IPAndMac? item = _map.Value.SingleOrDefault(x => x.IP == ip);
        return item?.MAC;
    }

    private class IPAndMac
    {
        public string? IP { get; set; }
        public string? MAC { get; set; }
    }
}