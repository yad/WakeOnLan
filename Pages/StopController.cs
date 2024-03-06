using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace wol.Pages;

[ApiController]
[Route("api/[controller]")]
public class StopController : ControllerBase
{
     public readonly List<WakeOnLan> WakeOnLanServers;
    private readonly NetworkFinder _networkFinder;

    public StopController(IOptions<WakeOnLanSettings> settings, NetworkFinder networkFinder)
    {
        WakeOnLanServers = settings.Value;
        _networkFinder = networkFinder;
    }

    [HttpGet("{serviceLabel}")]
    public async Task<IActionResult> GetAsync(string serviceLabel)
    {
        if (serviceLabel == "wol")
        {
            try
            {
                Process.Start("shutdown", "/s /t 30");
            }
            catch
            {                
            }
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

        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(service.Process)))
        {
            if (process.HasExited)
            {
                continue;                
            }

            try
            {
                process.CloseMainWindow();
            }
            catch(InvalidOperationException)
            {                
            }

            process.Close();
        }

        return Ok();
    }    
}
