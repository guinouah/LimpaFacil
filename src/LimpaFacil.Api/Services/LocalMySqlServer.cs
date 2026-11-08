using System.Diagnostics;
using System.Net.Sockets;

namespace LimpaFacil.Api.Services;

internal sealed class LocalMySqlServer(Process process) : IDisposable
{
    private readonly Process process = process;

    public static async Task<LocalMySqlServer?> StartIfAvailableAsync(
        string contentRoot,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        const int port = 3307;
        if (!connectionString.Contains("Port=3307", StringComparison.OrdinalIgnoreCase)) return null;
        if (await CanConnectAsync(port, cancellationToken)) return null;

        var executable = FindServerExecutable(contentRoot);
        if (executable is null) return null;

        var configuration = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(executable))!, "my.ini");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = $"--defaults-file=\"{configuration}\" --console",
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        }) ?? throw new InvalidOperationException("Não foi possível iniciar o MySQL local.");

        for (var attempt = 0; attempt < 30; attempt++)
        {
            if (process.HasExited)
                throw new InvalidOperationException($"O MySQL local encerrou com o código {process.ExitCode}.");
            if (await CanConnectAsync(port, cancellationToken)) return new LocalMySqlServer(process);
            await Task.Delay(500, cancellationToken);
        }

        process.Kill(entireProcessTree: true);
        throw new TimeoutException("O MySQL local não ficou disponível na porta 3307.");
    }

    private static string? FindServerExecutable(string contentRoot)
    {
        foreach (var seed in new[] { contentRoot, AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(seed);
            for (var level = 0; level < 8 && directory is not null; level++, directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "tools", "mysql-server", "bin", "mysqld.exe");
                if (File.Exists(candidate)) return candidate;
            }
        }

        return null;
    }

    private static async Task<bool> CanConnectAsync(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMilliseconds(350));
            await client.ConnectAsync("127.0.0.1", port, timeout.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        process.Dispose();
    }
}
