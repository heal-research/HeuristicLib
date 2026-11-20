using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Scenarios.Core.Algorithms;

public sealed class ColdObservationBindingScenarios(ITestOutputHelper output)
{
    private const string ChildMode = "HEURISTICLIB_COLD_OBSERVATION_CHILD";
    private const string CompletionMessage = "All observation workers verified on ";
    // Each launch samples a new scheduling window for the first binding.
    private const int Attempts = 16;
    private const int WorkerCount = 64;

    [Fact]
    public async Task ObservationHooksBindConcurrentlyOnFirstUse()
    {
        // The runtime raced when concurrently binding the old constrained static abstract Wrap hooks.
        // Only a fresh process per attempt keeps both hooks cold, regardless of the rest of the test suite.
        if (Environment.GetEnvironmentVariable(ChildMode) == "1")
        {
            Console.WriteLine($"Child runtime: {RuntimeInformation.FrameworkDescription}");
            await VerifyConcurrentObservations();
            Console.WriteLine($"{CompletionMessage}{RuntimeInformation.FrameworkDescription}");
            return;
        }

        var timer = Stopwatch.StartNew();
        for (var attempt = 1; attempt <= Attempts; attempt++)
            await RunChild(attempt, TestContext.Current.CancellationToken);

        output.WriteLine($"{Attempts} fresh processes completed in {timer.Elapsed.TotalSeconds:F2} s.");
    }

    private async Task RunChild(int attempt, CancellationToken cancellationToken)
    {
        var suite = typeof(ColdObservationBindingScenarios);
        // The SDK builds a platform-specific app host next to the test assembly.
        var executable = Path.ChangeExtension(suite.Assembly.Location, OperatingSystem.IsWindows() ? ".exe" : null);
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            ArgumentList = { "-method", $"{suite.FullName}.{nameof(ObservationHooksBindConcurrentlyOnFirstUse)}", "-noColor", "-noAutoReporters" },
            Environment = { [ChildMode] = "1" }
        };

        using var child = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the observation test child.");
        var stdout = child.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderr = child.StandardError.ReadToEndAsync(CancellationToken.None);
        var timedOut = false;
        try
        {
            await child.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }
        catch (TimeoutException)
        {
            timedOut = true;
        }
        finally
        {
            if (!child.HasExited)
            {
                child.Kill();
                await child.WaitForExitAsync(CancellationToken.None);
            }
        }

        var standardOutput = await stdout;
        var standardError = await stderr;
        var completion = standardOutput.Split('\n').FirstOrDefault(line => line.StartsWith(CompletionMessage, StringComparison.Ordinal));
        if (timedOut || child.ExitCode != 0 || completion is null)
        {
            var status = timedOut ? "timed out after 30 seconds" : $"exited with code {child.ExitCode}";
            Assert.Fail($"Observation child {attempt}/{Attempts} {status}.\nstdout:\n{standardOutput}\nstderr:\n{standardError}");
        }

        output.WriteLine($"Attempt {attempt}/{Attempts}: {completion.TrimEnd()}");
    }

    private static async Task VerifyConcurrentObservations()
    {
        // Dedicated threads let every worker reach the barrier even on runners with few thread-pool threads.
        using var barrier = new Barrier(WorkerCount);
        var workers = Enumerable.Range(0, WorkerCount).Select(seed => Task.Factory.StartNew(
            () => RunWorker(barrier, seed), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        await Task.WhenAll(workers);
    }

    private static void RunWorker(Barrier barrier, int seed)
    {
        if (!barrier.SignalAndWait(TimeSpan.FromSeconds(15)))
            throw new TimeoutException("The observation workers did not reach the barrier.");

        // Value-type candidates force distinct native instantiations of the observation hooks.
        switch (seed % 4)
        {
            case 0:
                VerifyObservations(seed, 0);
                break;
            case 1:
                VerifyObservations(seed, 0L);
                break;
            case 2:
                VerifyObservations(seed, 0.0);
                break;
            default:
                VerifyObservations(seed, 0.0m);
                break;
        }
    }

    // Keep the first JIT of the observation workload after the barrier.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void VerifyObservations<TCandidate>(int seed, TCandidate candidate) where TCandidate : struct
    {
        var problem = FuncProblem.Create(static (TCandidate _) => 0.0, new SingletonSpace<TCandidate>(candidate), SingleObjective.Minimize);
        var terminator = new AfterIterationsTerminator<TCandidate>(2);
        var algorithm = new GeneticAlgorithm<TCandidate>
        {
            Creator = new ConstantCreator<TCandidate>(candidate),
            Crossover = SelectFirstParentCrossover<TCandidate>.Instance,
            Mutator = new NoChangeMutator<TCandidate>(),
            Terminator = terminator,
            PopulationSize = 4,
            MutationRate = 1.0,
            MaximumGenerations = 3
        };

        var creations = 0;
        var evaluations = 0;
        var mutations = 0;
        var terminationDecisions = new List<bool>();
        var iterations = new Counter();
        var callEvents = new List<AlgorithmCallEvent>();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed),
        [
            algorithm.Creator.Invocations.Observing(_ => creations++),
            algorithm.Evaluator.Invocations.Observing(_ => evaluations++),
            algorithm.Mutator.Invocations.Observing(_ => mutations++),
            terminator.Invocations.Observing(observation => terminationDecisions.Add(observation.IsTerminal)),
            algorithm.Iterations.Counting(iterations),
            algorithm.Calls.Observing(observation => callEvents.Add(observation.Event))
        ]);

        run.Complete();

        creations.ShouldBe(1);
        evaluations.ShouldBe(2);
        mutations.ShouldBe(1);
        terminationDecisions.ShouldBe([false, true]);
        run.Result(iterations).CurrentCount.ShouldBe(2);
        callEvents.ShouldBe([AlgorithmCallEvent.Started, AlgorithmCallEvent.Completed]);
    }

    private sealed record ConstantCreator<TCandidate>(TCandidate Candidate) : StatelessCreator<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random) =>
            Enumerable.Repeat(Candidate, count).ToArray();
    }

    private sealed record SingletonSpace<TCandidate>(TCandidate Candidate) : SearchSpace<TCandidate>
    {
        public override bool Contains(TCandidate candidate) => EqualityComparer<TCandidate>.Default.Equals(candidate, Candidate);
    }
}
