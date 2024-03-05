using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace wol.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IOptions<WakeOnLanSettings> _settings;
    private readonly NetworkFinder _macFinder;
    public IReadOnlyCollection<WakeOnLan> WakeOnLanServers = Array.Empty<WakeOnLan>();

    public IndexModel(ILogger<IndexModel> logger, IOptions<WakeOnLanSettings> settings, NetworkFinder networkFinder)
    {
        _logger = logger;
        _settings = settings;
        _macFinder = networkFinder;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        WakeOnLanServers = _settings.Value.Select(server => new WakeOnLan()
        {
            Label = server.Label,
            MAC = server.MAC,
            Port = server.Port,
            Services = server.Services.Select(service => new Service()
            {
                Label = service.Label,
                Process = service.Process,
                Port = service.Port
            }).ToList()
        }).ToArray();

        foreach (var server in WakeOnLanServers)
        {
            server.IP = _macFinder.FindIPFromMacAddress(server.MAC);
            if (server.IP == null)
            {
                continue;
            }

            try
            {
                using (Ping ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(server.IP);
                    Console.WriteLine(reply.Status);
                    server.IsServerUp = reply.Status == IPStatus.Success;
                }
            }
            catch (PingException ex)
            {
                server.IsServerUp = true;
                Console.WriteLine(ex.ToString());
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
                    server.Api = $"http://{server.IP}:{server.Port}/api/test/wol"; // todo add check socket 
                    var result = await client.GetAsync(server.Api);
                    server.IsApiUp = result.StatusCode == System.Net.HttpStatusCode.OK;
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
                        using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(500) })
                        {
                            service.Api = $"http://{server.IP}:{server.Port}/api/test/{service.Label}"; // todo add check socket 
                            var result = await client.GetAsync(service.Api);
                            service.IsUp = result.StatusCode == System.Net.HttpStatusCode.OK;
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

    public async Task<IActionResult> OnPostAsync(string ip, string port, string serviceLabel, string mac)
    {
        try
        {
            if (string.IsNullOrEmpty(mac))
            {
                using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(500) })
                {
                    var api = $"http://{ip}:{port}/api/start/{serviceLabel}";
                    Console.WriteLine(api);
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

