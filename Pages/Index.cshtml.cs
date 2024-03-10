using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace wol.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IOptions<WakeOnLanSettings> _settings;
    private readonly NetworkFinder _networkFinder;
    public IReadOnlyCollection<WakeOnLan> WakeOnLanServers = Array.Empty<WakeOnLan>();

    public IndexModel(ILogger<IndexModel> logger, IOptions<WakeOnLanSettings> settings, NetworkFinder networkFinder)
    {
        _logger = logger;
        _settings = settings;
        _networkFinder = networkFinder;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        WakeOnLanServers = _settings.Value.Select(server => new WakeOnLan()
        {
            Label = server.Label,
            Icon = $"/logo/{Uri.EscapeDataString(server.Label)}.png",
            MAC = server.MAC,
            Port = server.Port,
            Services = server.Services.Select(service => new Service()
            {
                Label = service.Label,
                Link = service.Link,
                Icon = $"/logo/{service.Process.Split(new[]{'/', '\\', '.'}).Reverse().Skip(1).First()}.png",
                Process = service.Process,
                Port = service.Port,
                OnDemand = service.OnDemand
            }).ToList()
        }).ToArray();

        foreach (var server in WakeOnLanServers)
        {
            server.IP = _networkFinder.FindIPFromMacAddress(server.MAC);
            if (string.IsNullOrEmpty(server.IP))
            {
                continue;
            }

            server.HostName = _networkFinder.GetHostName(server.IP);

            try
            {
                using (Ping ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(server.IP);
                    server.IsServerUp = reply.Status == IPStatus.Success;
                }
            }
            catch (PingException)
            {
                server.IsServerUp = true;
            }

            if (!server.IsServerUp)
            {
                continue;
            }

            if (server.Port == 0)
            {
                continue;
            }

            try
            {
                using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(500) })
                {
                    server.Api = $"http://{server.IP}:{server.Port}/api/status/wol";
                    var result = await client.GetAsync(server.Api);
                    server.IsApiUp = result.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (TaskCanceledException)
            {
            }

            if (server.IsApiUp)
            {
                foreach (var service in server.Services)
                {
                    try
                    {
                        using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(100) })
                        {
                            service.Api = $"http://{server.IP}:{server.Port}/api/status/{service.Label}";
                            var result = await client.GetAsync(service.Api);
                            // Console.WriteLine(result.StatusCode);
                            switch (result.StatusCode)
                            {
                                case HttpStatusCode.OK:
                                    service.ApiStatus = ApiStatus.Up;
                                    break;
                                case HttpStatusCode.Accepted:
                                    service.ApiStatus = ApiStatus.Loading;
                                    break;
                                default:
                                    service.ApiStatus = ApiStatus.Down;
                                    break;
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string ip, string port, string mode, string serviceLabel, string mac)
    {
        try
        {
            if (string.IsNullOrEmpty(mac) || mode == "stop")
            {
                using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(500) })
                {
                    var api = $"http://{ip}:{port}/api/{mode}/{serviceLabel}";
                    // Console.WriteLine(api);
                    var result = await client.GetAsync(api);
                }
            }
            else
            {
                using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) { EnableBroadcast = true })
                {
                    string payloadmac = mac.ToLower().Replace(":", "");
                    var payload = Enumerable.Repeat<byte>(255, 6).Concat(Enumerable.Repeat(Enumerable.Range(0, 6).Select(i => Convert.ToByte(payloadmac.Substring(i * 2, 2), 16)), 16).SelectMany(b => b)).ToArray();

                    sock.SendTo(payload, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 12345));
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
        return RedirectToPage("/Index");
    }
}
