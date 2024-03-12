using System.Net;
using System.Net.NetworkInformation;

public class TcpListenerWorkerService : BackgroundService
{
    private static IReadOnlyCollection<IPEndPoint> TcpListenerCache { get; set; } = Array.Empty<IPEndPoint>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpListenerCache = ipGlobalProperties.GetActiveTcpListeners();

            await Task.Delay(5 * 1000, stoppingToken);
        }
    }

    public static IReadOnlyCollection<IPEndPoint> GetTcpListenerCache() => TcpListenerCache;
}