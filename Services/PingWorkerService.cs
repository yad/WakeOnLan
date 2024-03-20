using System.Net.NetworkInformation;

public class PingWorkerService : BackgroundService
{
    private readonly VisitService _visitService;

    public PingWorkerService(VisitService visitService)
    {
        _visitService = visitService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_visitService.IsActive)
            {

                var tasks = new List<Task>();

                for (var i = 1; i < 255; i++)
                {
                    var ip = $"192.168.1.{i}";
                    using (Ping ping = new Ping())
                    {
                        tasks.Add(ping.SendPingAsync(ip));
                    }
                }

                await Task.WhenAll(tasks);
            }

            await Task.Delay(5 * 1000, stoppingToken);
        }
    }
}