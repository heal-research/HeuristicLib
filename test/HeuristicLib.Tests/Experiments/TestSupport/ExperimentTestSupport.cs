using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments.TestSupport;

internal sealed record FixedExperiment<TAlgorithm>(ImmutableArray<ExperimentCase<TAlgorithm, int>> Cases)
    : Experiment<int, TAlgorithm, PopulationState<int>, int>
    where TAlgorithm : class, IAlgorithm<int, PopulationState<int>>
{
    public override ImmutableArray<ExperimentCase<TAlgorithm, int>> MaterializeCases() => Cases;
}

internal sealed record ProbeAlgorithm(
    int Value,
    ExecutionProbe? Probe = null,
    int DelayMilliseconds = 0,
    bool UseRandomValue = false,
    bool FailDuringSetup = false,
    bool FailDuringExecution = false,
    bool YieldState = true,
    int HoldAfterYieldMilliseconds = 0)
    : Algorithm<ProbeAlgorithm, int, PopulationState<int>>
{
    public override IAlgorithmInstance<int, TRunSearchSpace, TRunProblem, PopulationState<int>> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
    {
        Probe?.RecordSetup();
        if (FailDuringSetup)
        {
            throw new InvalidOperationException($"Setup failed for {Value}.");
        }

        return new Instance<TRunSearchSpace, TRunProblem>(Value, Probe, DelayMilliseconds, UseRandomValue, FailDuringExecution, YieldState, HoldAfterYieldMilliseconds);
    }

    private sealed class Instance<TSearchSpace, TProblem>(int value, ExecutionProbe? probe, int delayMilliseconds, bool useRandomValue, bool failDuringExecution, bool yieldState, int holdAfterYieldMilliseconds)
        : AlgorithmInstance<int, TSearchSpace, TProblem, PopulationState<int>>
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
    {
        public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
            TProblem problem,
            IRandomNumberGenerator random,
            PopulationState<int>? initialState = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            probe?.BeginExecution();
            try
            {
                ct.ThrowIfCancellationRequested();
                if (delayMilliseconds > 0)
                {
                    await Task.Delay(delayMilliseconds, ct);
                }

                if (failDuringExecution)
                {
                    throw new InvalidOperationException($"Execution failed for {value}.");
                }

                if (yieldState)
                {
                    var candidate = useRandomValue ? random.NextInt() : value;
                    probe?.RecordCandidate(candidate);
                    yield return ExperimentTestSupport.CreateState(candidate);
                    if (holdAfterYieldMilliseconds > 0)
                    {
                        await Task.Delay(holdAfterYieldMilliseconds, ct);
                    }
                }
            }
            finally
            {
                probe?.EndExecution();
            }
        }
    }
}

internal sealed class ExecutionProbe
{
    private int activeExecutions;
    private int maximumActiveExecutions;
    private int setupCount;
    private int executionCount;
    private readonly ConcurrentQueue<int> candidates = new();

    public int SetupCount => setupCount;

    public int ExecutionCount => executionCount;

    public int ActiveExecutions => activeExecutions;

    public int MaximumActiveExecutions => maximumActiveExecutions;

    public IReadOnlyList<int> Candidates => candidates.ToList();

    public void RecordSetup() => Interlocked.Increment(ref setupCount);

    public void BeginExecution()
    {
        Interlocked.Increment(ref executionCount);
        var active = Interlocked.Increment(ref activeExecutions);
        var maximum = maximumActiveExecutions;
        while (active > maximum)
        {
            var observed = Interlocked.CompareExchange(ref maximumActiveExecutions, active, maximum);
            if (observed == maximum)
            {
                break;
            }

            maximum = observed;
        }
    }

    public void RecordCandidate(int candidate) => candidates.Enqueue(candidate);

    public void EndExecution() => Interlocked.Decrement(ref activeExecutions);
}

internal static class ExperimentTestSupport
{
    public static ExperimentRun<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, TAlgorithm, int> CreateRun<TAlgorithm>(
        IExperiment<int, TAlgorithm, PopulationState<int>, int> experiment,
        int seed = 42)
        where TAlgorithm : class, IAlgorithm<int, PopulationState<int>> =>
        experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(seed));

    public static PopulationState<int> CreateState(int candidate) => new()
    {
        Population = Population.From([EvaluatedCandidate.From(candidate, candidate)])
    };
}
