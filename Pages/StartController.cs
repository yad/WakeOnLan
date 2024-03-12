using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace wol.Pages;

[ApiController]
[Route("api/[controller]")]
public class StartController : ControllerBase
{
    public readonly List<WakeOnLan> WakeOnLanServers;
    private readonly ILogger<StartController> _logger;
    public StartController(ILogger<StartController> logger, IOptions<WakeOnLanSettings> settings)
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

        if (!service.OnDemand)
        {
            throw new InvalidOperationException($"{serviceLabel} - not OnDemand");
        }

        if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(service.Process)).Any())
        {
            return Content("Process UP");
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Minimized,
            UseShellExecute = true,

            WorkingDirectory = string.IsNullOrEmpty(service.WorkingDirectory) ? Path.GetDirectoryName(service.Process) : service.WorkingDirectory,
            Arguments = string.IsNullOrEmpty(service.Arguments) ? "" : service.Arguments,
            FileName = service.Process
        };

        Process.Start(startInfo);

        return Ok();
    }
}
