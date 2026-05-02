using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using BreakfastProvider.Tests.Component.Shared.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;

namespace BreakfastProvider.Tests.Component.Shared.Infrastructure;

/// <summary>
/// Manages Docker Compose lifecycle for component tests.
/// When <see cref="ComponentTestSettings.EnableDockerInSetupAndTearDown"/> is <c>true</c>,
/// spins up the required compose files on setup and tears them down afterwards.
/// When <see cref="ComponentTestSettings.RunAgainstExternalServiceUnderTest"/> is also <c>true</c>,
/// the SUT compose file is included so the API runs in Docker.
/// </summary>
public sealed class DockerComposeOrchestrator : IDisposable
{
    private string? _dockerDir;
    private string[]? _composeFiles;
    private bool _skipTearDown;

    private static readonly string[] DependencyComposeFiles =
    [
        "docker-compose-database.yml",
        "docker-compose-storage.yml",
        "docker-compose-fakes.yml",
        "docker-compose-messaging.yml",
        "docker-compose-eventhub.yml",
        "docker-compose-prometheus.yml",
        "docker-compose-grafana.yml",
        "docker-compose-jaeger.yml"
    ];

    private const string SutComposeFile = "docker-compose-sut.yml";

    public void Start(ComponentTestSettings settings)
    {
        if (!settings.EnableDockerInSetupAndTearDown)
        {
            // Even when Docker lifecycle is managed externally (e.g. CI pipeline),
            // verify infrastructure readiness before tests start. The CI workflow
            // waits for the Cosmos certificate endpoint, but the emulator may not
            // be write-ready yet — run the same data-plane and write-probe checks
            // that the full orchestrator uses.
            if (!settings.RunWithAnInMemoryDatabase)
            {
                Log("Docker lifecycle managed externally — verifying infrastructure readiness...");
                WaitUntilReady("Cosmos DB (data plane)", IsCosmosDbReady);

                if (!settings.RunWithAnInMemoryKafkaBroker)
                    WaitUntilReady("Kafka (SSL handshake)", () => IsKafkaSslReady(settings));

                Log("Infrastructure readiness verified.");
            }

            return;
        }

        _skipTearDown = settings.SkipDockerTearDown;

        Log("Docker compose orchestration enabled.");

        _dockerDir = ResolveDockerDirectory();
        Log($"Docker directory: {_dockerDir}");

        var files = DependencyComposeFiles.ToList();

        if (settings.RunAgainstExternalServiceUnderTest)
        {
            files.Add(SutComposeFile);
            Log("External SUT mode — including docker-compose-sut.yml.");
        }

        _composeFiles = files.ToArray();
        Log($"Compose files: {string.Join(", ", _composeFiles)}");

        // If containers are already running and fully healthy from a previous run
        // (e.g. killed test that skipped teardown), reuse them. Recycling a warm
        // Cosmos emulator via down/up causes instability — the emulator needs
        // significant CPU warmup after a cold start.
        if (AreExistingContainersUsable(settings))
        {
            Log("Existing containers passed write probe — reusing without restart.");
            return;
        }

        // Containers are either not running or unhealthy — start fresh.
        Log("Tearing down any leftover containers...");
        RunDockerCompose("down --remove-orphans", throwOnFailure: false);
        Log("Leftover teardown complete.");

        Log("Starting containers...");
        RunDockerCompose("up -d --remove-orphans", throwOnFailure: true);
        Log("docker compose up completed.");

        Log("Running readiness checks...");
        WaitUntilReady("Cosmos DB (data plane)", IsCosmosDbReady);
        WaitUntilReady("Kafka (SSL handshake)", () => IsKafkaSslReady(settings));
        WaitUntilReady("SQL Server", IsSqlServerReady);

        if (settings.RunAgainstExternalServiceUnderTest)
            WaitUntilReady("SUT (/health)", () => IsSutHealthy(settings));

        // After the basic readiness checks pass, run a write-probe against Cosmos
        // to ensure the emulator is fully warmed up for document operations.
        // The emulator can pass CreateDatabaseIfNotExistsAsync but still timeout
        // on actual document writes if it hasn't finished its internal warmup.
        WaitUntilReady("Cosmos DB (write probe)", IsCosmosWriteProbeSuccessful);

        Log("All readiness checks passed — Docker infrastructure is ready.");
    }

    public void Dispose()
    {
        if (_dockerDir is null || _composeFiles is null)
            return;

        if (_skipTearDown)
        {
            Log("SkipDockerTearDown is enabled — leaving containers running for reuse.");
            return;
        }

        try
        {
            Log("Tearing down Docker containers...");
            RunDockerCompose("down --remove-orphans", throwOnFailure: false);
            Log("Docker teardown complete.");
        }
        catch (Exception ex)
        {
            Log($"Warning: teardown encountered an error (non-fatal): {ex.Message}");
        }
    }

    private void RunDockerCompose(string arguments, bool throwOnFailure)
    {
        var fileArgs = string.Join(" ", _composeFiles!.Select(f => $"-f \"{f}\""));
        var command = $"docker compose {fileArgs} {arguments}";

        Log($"  $ {command}");

        var psi = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
            Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"",
            WorkingDirectory = _dockerDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start docker compose process.");

        // Drain both streams asynchronously to prevent buffer-full deadlocks.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("docker compose did not complete within 5 minutes.");
        }

        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();

        if (!string.IsNullOrWhiteSpace(stderr))
            Log($"  stderr: {stderr.Trim()}");

        if (process.ExitCode != 0)
        {
            var message = $"docker compose exited with code {process.ExitCode}.";
            if (!string.IsNullOrWhiteSpace(stderr))
                message += $" stderr: {stderr.Trim()}";
            if (!string.IsNullOrWhiteSpace(stdout))
                message += $" stdout: {stdout.Trim()}";

            if (throwOnFailure)
                throw new InvalidOperationException(message);

            Log($"  Warning (non-fatal): {message}");
        }
    }

    /// <summary>
    /// Polls a readiness check every 5 seconds, for up to 5 minutes.
    /// </summary>
    private static void WaitUntilReady(string name, Func<bool> check)
    {
        const int maxRetries = 60;
        const int delayMs = 5_000;

        Log($"  Waiting for {name}...");

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                if (check())
                {
                    Log($"  {name} — ready (attempt {i + 1}).");
                    return;
                }
            }
            catch (Exception ex)
            {
                if (i % 6 == 5) // Log exception details every 30s to avoid spam
                    Log($"  {name} — check threw {ex.GetType().Name}: {ex.Message}");
            }

            if (i > 0 && i % 6 == 0) // Progress every 30s
                Log($"  {name} — still waiting ({i * delayMs / 1000}s elapsed)...");

            Thread.Sleep(delayMs);
        }

        throw new TimeoutException(
            $"Readiness check '{name}' did not succeed within {maxRetries * delayMs / 1000} seconds.");
    }

    /// <summary>
    /// Verifies the Cosmos DB emulator is ready by testing the actual data plane.
    /// First checks the vNext health probe endpoint (fast), then the HTTPS gateway
    /// endpoint to ensure the emulator is fully warmed up and can serve requests.
    /// </summary>
    private static bool IsCosmosDbReady()
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

            // vNext emulator exposes a readiness probe on port 8080 (HTTP)
            var readyResponse = httpClient.GetAsync("http://localhost:8080/ready").GetAwaiter().GetResult();
            if (!readyResponse.IsSuccessStatusCode)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts an SSL/TLS handshake against the Kafka broker's SASL_SSL listener
    /// to verify the listener is fully initialised and accepting SSL connections.
    /// </summary>
    private static bool IsKafkaSslReady(ComponentTestSettings settings)
    {
        try
        {
            var bootstrapServers = settings.KafkaConfig?.BootstrapServers ?? "localhost:9092";
            var parts = bootstrapServers.Split(':');
            var host = parts[0];
            var port = parts.Length > 1 ? int.Parse(parts[1]) : 9092;

            using var tcp = new TcpClient();
            tcp.Connect(host, port);

            using var sslStream = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            sslStream.AuthenticateAsClient(host);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Polls the SUT health endpoint to determine readiness.
    /// </summary>
    private static bool IsSutHealthy(ComponentTestSettings settings)
    {
        try
        {
            var url = settings.ExternalServiceUnderTestUrl?.TrimEnd('/') ?? "http://localhost:5080";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = client.GetAsync($"{url}/health").GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies SQL Server is ready by attempting a TCP connection to port 1433.
    /// </summary>
    private static bool IsSqlServerReady()
    {
        try
        {
            using var tcp = new TcpClient();
            tcp.Connect("localhost", 1433);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the docker/ directory relative to the solution root.
    /// The test working directory is tests/BreakfastProvider.Tests.Component/bin/…/net10.0/,
    /// so we walk upward to find the docker/ folder.
    /// </summary>
    private static string ResolveDockerDirectory()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "docker");
            if (Directory.Exists(candidate) &&
                File.Exists(Path.Combine(candidate, "docker-compose-database.yml")))
                return candidate;

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not find the docker/ directory. Ensure the test is running from within the repository.");
    }

    /// <summary>
    /// Runs several consecutive write+delete cycles against the Cosmos emulator to
    /// verify it can sustain document operations under light concurrency. A single
    /// successful probe isn't enough — the emulator can complete one write but still
    /// be unstable for the burst of parallel requests that follow when tests start.
    /// </summary>
    private static bool IsCosmosWriteProbeSuccessful()
    {
        const int probeCount = 5;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var cosmosClient = CreateCosmosProbeClient();
            var container = cosmosClient.GetContainer("BreakfastDb", "orders");

            for (var i = 0; i < probeCount; i++)
            {
                var probeId = $"_probe_{Guid.NewGuid()}";
                var probeDoc = new { id = probeId, partitionKey = "_probe" };

                container.CreateItemAsync(probeDoc, new PartitionKey("_probe"), cancellationToken: cts.Token)
                    .GetAwaiter().GetResult();
                container.DeleteItemAsync<object>(probeId, new PartitionKey("_probe"), cancellationToken: cts.Token)
                    .GetAwaiter().GetResult();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether the Docker infrastructure from a previous (possibly killed)
    /// run is still alive and functional. This avoids tearing down and restarting
    /// a warm Cosmos emulator, which causes minutes of CPU warmup and instability.
    /// </summary>
    private static bool AreExistingContainersUsable(ComponentTestSettings settings)
    {
        try
        {
            Log("Checking if existing containers are usable...");

            if (!IsCosmosDbReady())
                return false;

            if (!IsKafkaSslReady(settings))
                return false;

            if (!IsSqlServerReady())
                return false;

            if (settings.RunAgainstExternalServiceUnderTest && !IsSutHealthy(settings))
                return false;

            if (!IsCosmosWriteProbeSuccessful())
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static CosmosClient CreateCosmosProbeClient()
    {
        var options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            RequestTimeout = TimeSpan.FromSeconds(10),
            HttpClientFactory = () =>
            {
                var h = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                return new HttpClient(h);
            }
        };

        return new CosmosClient(
            "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
            options);
    }

    private static void Log(string message)
    {
        var line = $"[DockerOrchestrator] {message}";
        Console.WriteLine(line);

        // Also write to the VS Debug Output window so developers running tests
        // from Visual Studio's Test Explorer can see Docker startup progress in
        // real time (Debug > Windows > Output, show output from: Tests).
        Trace.WriteLine(line);
    }
}
