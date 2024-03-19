using System.Diagnostics;

public class ProcessWorkerService : BackgroundService
{
    private IReadOnlyCollection<ProcessAndArguments> _processArgumentsMapCache { get; set; } = Array.Empty<ProcessAndArguments>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _processArgumentsMapCache = InitializeGetProcessesAndArguments();
            await Task.Delay(5 * 1000, stoppingToken);
        }
    }

    private static IReadOnlyCollection<ProcessAndArguments> InitializeGetProcessesAndArguments()
    {
        List<ProcessAndArguments> processesAndArguments = new List<ProcessAndArguments>();

        var arpStream = ExecuteCommandLine("wmic", "process get processid,commandline");
        if (arpStream != null)
        {
            while (!arpStream.EndOfStream)
            {
                var line = arpStream.ReadLine()?.Trim();

                if (!string.IsNullOrEmpty(line))
                {
                    while (line.Contains("  "))
                    {
                        line = line.Replace("  ", " ");
                    }

                    if (line.StartsWith("\\??\\"))
                    {
                        continue;
                    }

                    if (line.StartsWith("\""))
                    {
                        var parts = line.Split("\" ");
                        var process = parts.First().Trim();
                        var innerParts = string.Join(' ', parts.Skip(1));
                        parts = innerParts.Split(' ');
                        var processId = parts.Reverse().First().Trim();
                        var arguments = parts.SkipLast(1);
                        processesAndArguments.Add(new ProcessAndArguments()
                        {
                            Process = process,
                            ProcessId = processId,
                            Arguments = string.Join(' ', arguments).Trim()
                        });
                    }
                    else
                    {
                        var parts = line.Split(' ');
                        var process = parts.First().Trim();
                        var processId = parts.Reverse().First().Trim();
                        var arguments = parts.Skip(1).SkipLast(1);
                        processesAndArguments.Add(new ProcessAndArguments()
                        {
                            Process = process,
                            ProcessId = processId,
                            Arguments = string.Join(' ', arguments).Trim()
                        });
                    }
                }
            }
        }

        // foreach (var processAndArguments in processesAndArguments)
        // {
        //     Console.WriteLine(processAndArguments.Process);
        //     Console.WriteLine($"[{processAndArguments.ProcessId}]");
        //     Console.WriteLine($"\t{processAndArguments.Arguments}");
        // }

        return processesAndArguments.ToArray();
    }

    private static StreamReader? ExecuteCommandLine(string file, string arguments = "")
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            FileName = file,
            Arguments = arguments
        };

        Process? process = Process.Start(startInfo);

        return process?.StandardOutput;
    }

    private class ProcessAndArguments
    {
        public string Process { get; set; } = "";
        public string ProcessId { get; set; } = "";
        public string Arguments { get; set; } = "";
    }
}