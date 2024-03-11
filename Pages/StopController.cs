﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace wol.Pages;

[ApiController]
[Route("api/[controller]")]
public class StopController : ControllerBase
{
    public readonly List<WakeOnLan> WakeOnLanServers;
    private readonly ILogger<StopController> _logger;
    private readonly NetworkFinder _networkFinder;

    public StopController(ILogger<StopController> logger, IOptions<WakeOnLanSettings> settings, NetworkFinder networkFinder)
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
            try
            {
                Process.Start("shutdown", "/s /t 30");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "shutdown /s /t 30");
            }
            return Content("Minion UP");
        }

        var mac = _networkFinder.FindMacFromLoopBackIPAddress();

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

        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(service.Process)))
        {
            if (!process.CloseMainWindow())
            {
                process.Kill();
            }
        }

        return Ok();
    }
}
