using System.Diagnostics;
using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed class ConcordeTravelingSalesmanExactSolver : ITravelingSalesmanExactSolver
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
    private readonly string concordeExecutable;
    private readonly string? cygwinRuntime;

    public ConcordeTravelingSalesmanExactSolver(string concordePath, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(concordePath);

        if (Directory.Exists(concordePath))
        {
            concordeExecutable = Path.Combine(concordePath, "concorde.exe");
            cygwinRuntime = Path.Combine(concordePath, "cygwin1.dll");
        }
        else
        {
            concordeExecutable = concordePath;
            cygwinRuntime = Path.Combine(Path.GetDirectoryName(concordePath) ?? string.Empty, "cygwin1.dll");
        }

        Timeout = timeout ?? DefaultTimeout;
    }

    public TimeSpan Timeout { get; }
    public bool PreserveWorkingDirectoryOnFailure { get; init; } = true;
    public bool AcceptSolutionFileOnNonZeroExit { get; init; } = true;

    public TravelingSalesmanExactSolution Solve(ITravelingSalesmanProblemData problemData, IReadOnlyList<int> cities,
                                                CancellationToken cancellationToken = default)
    {
        if (!File.Exists(concordeExecutable))
        {
            throw new FileNotFoundException("The Concorde executable was not found.", concordeExecutable);
        }

        return cities.Count switch
        {
            0 => new TravelingSalesmanExactSolution([], 0.0),
            1 => new TravelingSalesmanExactSolution([cities[0]], 0.0),
            2 => SolveTwoCityProblem(problemData, cities),
            _ => SolveWithConcorde(problemData, cities, cancellationToken)
        };
    }

    private static TravelingSalesmanExactSolution SolveTwoCityProblem(ITravelingSalesmanProblemData problemData,
                                                                      IReadOnlyList<int> cities)
    {
        var tour = ImmutableArray.Create(cities[0], cities[1]);
        return new TravelingSalesmanExactSolution(tour,
            TravelingSalesmanExactSolverExtensions.EvaluateTour(problemData, tour));
    }

    private TravelingSalesmanExactSolution SolveWithConcorde(ITravelingSalesmanProblemData problemData,
                                                             IReadOnlyList<int> cities,
                                                             CancellationToken cancellationToken)
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), $"HeuristicLib_Concorde_{Guid.NewGuid():N}");
        Directory.CreateDirectory(workingDirectory);
        var solved = false;

        try
        {
            var localConcorde = Path.Combine(workingDirectory, "concorde.exe");
            File.Copy(concordeExecutable, localConcorde);
            if (!string.IsNullOrEmpty(cygwinRuntime) && File.Exists(cygwinRuntime))
            {
                File.Copy(cygwinRuntime, Path.Combine(workingDirectory, "cygwin1.dll"));
            }

            var instanceFile = "instance.tsp";
            var solutionFile = "solution.tour";
            var instancePath = Path.Combine(workingDirectory, instanceFile);
            var solutionPath = Path.Combine(workingDirectory, solutionFile);
            var arguments = $"-o {solutionFile} -x {instanceFile}";
            File.WriteAllText(instancePath, WriteExplicitFullMatrixTsplib(problemData, cities));

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(localConcorde)
            {
                Arguments = arguments,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory
            };

            process.Start();
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            if (!process.WaitForExit(Timeout))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process may have exited between timeout detection and Kill.
                }

                throw new TimeoutException($"Concorde did not finish within {Timeout}.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var stdoutText = stdout.GetAwaiter().GetResult();
            var stderrText = stderr.GetAwaiter().GetResult();

            if (!File.Exists(solutionPath))
            {
                throw CreateConcordeException(process.ExitCode, localConcorde, arguments, workingDirectory,
                    instancePath, solutionPath, stdoutText, stderrText,
                    process.ExitCode == 0
                        ? "Concorde exited successfully but did not create the expected solution file."
                        : "Concorde exited with a non-zero code and did not create the expected solution file.");
            }

            if (process.ExitCode != 0 && !AcceptSolutionFileOnNonZeroExit)
            {
                throw CreateConcordeException(process.ExitCode, localConcorde, arguments, workingDirectory,
                    instancePath, solutionPath, stdoutText, stderrText);
            }

            var localTour = ReadConcordeTour(solutionPath, cities.Count);
            var tour = localTour.Select(city => cities[city]).ToImmutableArray();
            var quality = TravelingSalesmanExactSolverExtensions.EvaluateTour(problemData, tour);

            solved = true;
            return new TravelingSalesmanExactSolution(tour, quality);
        }
        finally
        {
            if (solved || !PreserveWorkingDirectoryOnFailure)
            {
                TryDeleteDirectory(workingDirectory);
            }
        }
    }

    private static string WriteExplicitFullMatrixTsplib(ITravelingSalesmanProblemData problemData, IReadOnlyList<int> cities)
    {
        var builder = new StringBuilder();
        builder.AppendLine("NAME: dynamic_epoch");
        builder.AppendLine("TYPE: TSP");
        builder.AppendLine($"DIMENSION: {cities.Count.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine("EDGE_WEIGHT_TYPE: EXPLICIT");
        builder.AppendLine("EDGE_WEIGHT_FORMAT: FULL_MATRIX");
        builder.AppendLine("EDGE_WEIGHT_SECTION");

        for (var i = 0; i < cities.Count; i++)
        {
            for (var j = 0; j < cities.Count; j++)
            {
                if (j > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(ToConcordeDistance(problemData.GetDistance(cities[i], cities[j]))
                               .ToString(CultureInfo.InvariantCulture));
            }

            builder.AppendLine();
        }

        builder.AppendLine("EOF");
        return builder.ToString();
    }

    private static int ToConcordeDistance(double distance)
    {
        if (!double.IsFinite(distance) || distance < 0)
        {
            throw new InvalidOperationException("Concorde requires finite non-negative edge weights.");
        }

        var rounded = Math.Round(distance);
        if (Math.Abs(distance - rounded) > 1e-9)
        {
            throw new InvalidOperationException("Concorde integration currently supports integer TSP distances only.");
        }

        return checked((int)rounded);
    }

    private static ImmutableArray<int> ReadConcordeTour(string tourFile, int dimension)
    {
        var values = File.ReadAllText(tourFile)
                         .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                         .Select(int.Parse)
                         .Where(x => x >= 0 && x < dimension)
                         .ToImmutableArray();

        if (values.Length != dimension)
        {
            throw new InvalidDataException("Concorde returned a tour with an unexpected number of cities.");
        }

        return values;
    }

    private static InvalidOperationException CreateConcordeException(int exitCode,
                                                                     string executable,
                                                                     string arguments,
                                                                     string workingDirectory,
                                                                     string instancePath,
                                                                     string solutionPath,
                                                                     string stdout,
                                                                     string stderr,
                                                                     string? reason = null)
    {
        var message = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(reason))
        {
            message.AppendLine(reason);
        }

        message.AppendLine($"Concorde failed with exit code {exitCode}.");
        message.AppendLine($"Executable: {executable}");
        message.AppendLine($"Arguments: {arguments}");
        message.AppendLine($"Working directory: {workingDirectory}");
        message.AppendLine($"Input file: {instancePath}");
        message.AppendLine($"Expected solution file: {solutionPath}");
        message.AppendLine($"Solution file exists: {File.Exists(solutionPath)}");
        message.AppendLine("Standard output:");
        message.AppendLine(string.IsNullOrWhiteSpace(stdout) ? "<empty>" : stdout);
        message.AppendLine("Standard error:");
        message.AppendLine(string.IsNullOrWhiteSpace(stderr) ? "<empty>" : stderr);

        return new InvalidOperationException(message.ToString());
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only; failed temp cleanup should not hide solver results.
        }
    }
}
