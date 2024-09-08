using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace wol.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly WakeOnLanServersBuilder _wakeOnLanServersBuilder;

    public IReadOnlyCollection<WakeOnLan> WakeOnLanServers = Array.Empty<WakeOnLan>();

    public IndexModel(ILogger<IndexModel> logger, WakeOnLanServersBuilder wakeOnLanServersBuilder)
    {
        _logger = logger;
        _wakeOnLanServersBuilder = wakeOnLanServersBuilder;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        WakeOnLanServers = await _wakeOnLanServersBuilder.Build(WakeOnLanServers);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string ip, string port, string mode, string serviceLabel, string mac)
    {
        // Console.WriteLine($"ip {ip}, port {port}, mode {mode}, serviceLabel {serviceLabel}, mac {mac}");
        try
        {
            if (string.IsNullOrEmpty(mac) || mode == "stop")
            {
                using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(2000) })
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
