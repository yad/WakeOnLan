using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;

public class WakeOnLanServersBuilder
{
    private readonly ILogger<WakeOnLanServersBuilder> _logger;
    private readonly IOptions<WakeOnLanSettings> _settings;

    private IReadOnlyCollection<WakeOnLan> _wakeOnLanServers;

    public WakeOnLanServersBuilder(ILogger<WakeOnLanServersBuilder> logger, IOptions<WakeOnLanSettings> settings)
    {
        _logger = logger;
        _settings = settings;

        _wakeOnLanServers = Array.Empty<WakeOnLan>();
    }

    public async Task<IReadOnlyCollection<WakeOnLan>> Build(IReadOnlyCollection<WakeOnLan> previousStates)
    {
        BuildWakeOnLanServers();
        await TestAllUp();
        await UpdateAllServers();
        await UpdateAllServices(previousStates);
        return _wakeOnLanServers;
    }

    private WakeOnLanServersBuilder BuildWakeOnLanServers()
    {
        _wakeOnLanServers = _settings.Value.Select(server => new WakeOnLan()
        {
            Label = server.Label,
            Icon = $"/logo/{Uri.EscapeDataString(server.Label)}.png",
            MAC = server.MAC,
            Port = server.Port,
            Services = server.Services.Select(service => new Service()
            {
                Label = service.Label,
                Link = service.Link,
                Icon = $"/logo/{service.Label}.png",
                Process = service.Process,
                // Arguments = service.Arguments,
                // WorkingDirectory = service.WorkingDirectory,
                Port = service.Port,
                OnDemand = service.OnDemand
            }).ToList()
        }).ToArray();
        return this;
    }

    private async Task<WakeOnLanServersBuilder> TestAllUp()
    {
        var tasks = new List<Task>();
        foreach (var server in _wakeOnLanServers)
        {
            server.IP = NetworkFinderWorkerService.FindIPFromMacAddress(server.MAC);
            if (string.IsNullOrEmpty(server.IP))
            {
                continue;
            }

            server.HostName = NetworkFinderWorkerService.GetHostName(server.IP);

            tasks.Add(TestUp(server));
        }
        await Task.WhenAll(tasks);
        return this;
    }

    private static async Task TestUp(WakeOnLan server)
    {
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
    }

    private async Task<WakeOnLanServersBuilder> UpdateAllServers()
    {
        List<Task> tasks = new List<Task>();
        foreach (var server in _wakeOnLanServers)
        {
            if (!server.IsServerUp)
            {
                continue;
            }

            if (server.Port == 0)
            {
                continue;
            }

            tasks.Add(UpdateServer(server));
        }
        await Task.WhenAll(tasks);
        return this;
    }

    private static async Task UpdateServer(WakeOnLan server)
    {
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
            server.IsApiUp = false;
        }
    }

    private async Task<WakeOnLanServersBuilder> UpdateAllServices(IReadOnlyCollection<WakeOnLan> previousStates)
    {
        List<Task> tasks = new List<Task>();
        foreach (var server in _wakeOnLanServers)
        {
            if (server.IsApiUp)
            {
                var previousServer = previousStates.FirstOrDefault(s => s.Label == server.Label);
                foreach (var service in server.Services)
                {
                    var previousService = previousServer?.Services.FirstOrDefault(s => s.Label == service.Label);
                    tasks.Add(UpdateService(server, service, previousService));
                }
            }
        }
        await Task.WhenAll(tasks);
        return this;
    }

    private static async Task UpdateService(WakeOnLan server, Service service, Service? previousService)
    {
        try
        {
            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(500) })
            {
                service.Api = $"http://{server.IP}:{server.Port}/api/status/{service.Label}";
                var result = await client.GetAsync(service.Api);
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
            service.ApiStatus = previousService?.ApiStatus ?? ApiStatus.Down;
        }
    }
}