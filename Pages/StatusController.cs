using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace wol.Pages;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    public readonly List<WakeOnLan> WakeOnLanServers;
    private readonly ILogger<StatusController> _logger;
    private readonly NetworkFinder _networkFinder;

    public StatusController(ILogger<StatusController> logger, IOptions<WakeOnLanSettings> settings, NetworkFinder networkFinder)
    {
        _logger = logger;
        WakeOnLanServers = settings.Value;
        _networkFinder = networkFinder;
    }

    [HttpGet("{serviceLabel}")]
    public IActionResult Get(string serviceLabel)
    {
        if (serviceLabel == "wol")
        {
            return Content("Minion UP");
        }

        var mac = _networkFinder.FindMacFromIPAddress("127.0.0.1");

        var server = WakeOnLanServers.FirstOrDefault(server => server.MAC == mac);
        if (server == null)
        {
            throw new Exception("127.0.0.1");
        }

        var service = server.Services.FirstOrDefault(service => service.Label == serviceLabel);
        if (service == null)
        {
            throw new Exception(serviceLabel);
        }

        if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(service.Process)).Any())
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpListener = ipGlobalProperties.GetActiveTcpListeners();

            if (tcpListener.Any(listener => listener.Port == service.Port))
            {
                return Content("Process UP");
            }

            return Accepted();
        }

        return NoContent();
    }
}
