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
            server.IsServerUp = server.IP != null; //TODO add ping ICMP

            if (!server.IsServerUp)
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
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
                using (var client = new UdpClient("255.255.255.255", 12287) { EnableBroadcast = true })
                {
                    var macPacket = mac.ToLower().Replace(":", "");
                    Console.WriteLine(macPacket);
                    await client.SendAsync(Enumerable.Repeat<byte>(255, 6).Concat(Enumerable.Repeat(Enumerable.Range(0, 6).Select(i => Convert.ToByte(macPacket.Substring(i * 2, 2), 16)), 16).SelectMany(b => b)).ToArray(), 102);
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return RedirectToPage("/Index");
    }
}

