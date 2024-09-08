using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace wol.Pages;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    public readonly List<WakeOnLan> WakeOnLanServers;
    private readonly ILogger<StatusController> _logger;

    public StatusController(ILogger<StatusController> logger, IOptions<WakeOnLanSettings> settings)
    {
        _logger = logger;
        WakeOnLanServers = settings.Value;
    }

    [HttpGet("{serviceLabel}")]
    public IActionResult Get(string serviceLabel)
    {
        if (serviceLabel == "wol")
        {
            return Content("Minion UP");
        }

        var mac = NetworkFinderWorkerService.FindMacFromLoopBackIPAddress();

        var server = WakeOnLanServers.FirstOrDefault(server => server.MAC == mac);
        if (server == null)
        {
            throw new InvalidOperationException($"{mac} - no IP");
        }

        var service = server.Services.FirstOrDefault(service => service.Label == serviceLabel);
        if (service == null)
        {
            throw new InvalidOperationException(serviceLabel);
        }

        var processes = Process.GetProcesses().Select(p => p.ProcessName);
        if (processes.Any(p => p == service.Process))
        {
            var tcpListener = TcpListenerWorkerService.GetTcpListeners();
            if (tcpListener.Any(listener => listener.Port == service.Port))
            {
                return Content("Process UP");
            }

            return Accepted();
        }

        return NoContent();
    }
}
