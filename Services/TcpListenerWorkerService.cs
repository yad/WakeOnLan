using System.Net;
using System.Net.NetworkInformation;

public class TcpListenerWorkerService : BackgroundService
{
    private static IReadOnlyCollection<IPEndPoint> _tcpListenersCache { get; set; } = Array.Empty<IPEndPoint>();

    private readonly VisitService _visitService;

    public TcpListenerWorkerService(VisitService visitService)
    {
        _visitService = visitService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_visitService.IsActive)
            {
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                _tcpListenersCache = ipGlobalProperties.GetActiveTcpListeners();
            }
            else
            {
                // Console.WriteLine($"{GetType().Name} is sleeping");
                _tcpListenersCache = Array.Empty<IPEndPoint>();
            }

            await Task.Delay(5 * 1000, stoppingToken);
        }
    }

    public static IReadOnlyCollection<IPEndPoint> GetTcpListeners() => _tcpListenersCache;
}