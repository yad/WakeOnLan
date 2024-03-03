using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace wol.Pages;

[ApiController]
[Route("api/[controller]")]
public class StartController : ControllerBase
{
     public readonly List<WakeOnLan> WakeOnLanServers;
    private readonly NetworkFinder _networkFinder;

    public StartController(IOptions<WakeOnLanSettings> settings, NetworkFinder networkFinder)
    {
        WakeOnLanServers = settings.Value;
        _networkFinder = networkFinder;
    }

    [HttpGet("{serviceLabel}")]
    public async Task<IActionResult> GetAsync(string serviceLabel)
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
            return Content("Process UP");
        }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        // startInfo.CreateNoWindow = true;
        startInfo.WindowStyle = ProcessWindowStyle.Minimized;
        startInfo.UseShellExecute = true;

        startInfo.WorkingDirectory = Path.GetDirectoryName(service.Process);
        startInfo.FileName = service.Process;
        // startInfo.Arguments = "";

        Process.Start(startInfo);

        return Ok();
    }    
}
