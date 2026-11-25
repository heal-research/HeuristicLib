using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments.TestSupport;

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
    protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
    {
        Probe?.RecordSetup();
        if (FailDuringSetup)
        {
            throw new InvalidOperationException($"Setup failed for {Value}.");
        }

        return new Executor(Value, Probe, DelayMilliseconds, UseRandomValue, FailDuringExecution, YieldState, HoldAfterYieldMilliseconds);
    }

    private sealed class Executor(int value, ExecutionProbe? probe, int delayMilliseconds, bool useRandomValue, bool failDuringExecution, bool yieldState, int holdAfterYieldMilliseconds)
        : AlgorithmExecutor<PopulationState<int>>
    {
        public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
            IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
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

internal sealed record DisposalTrackingAlgorithm(Func<ValueTask> Dispose, Func<CancellationToken, ValueTask<bool>>? MoveAfterFirstYield = null)
    : Algorithm<DisposalTrackingAlgorithm, int, PopulationState<int>>
{
    protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) => new Executor(Dispose, MoveAfterFirstYield);

    private sealed class Executor(Func<ValueTask> dispose, Func<CancellationToken, ValueTask<bool>>? moveAfterFirstYield)
        : AlgorithmExecutor<PopulationState<int>>, IAsyncEnumerable<PopulationState<int>>, IAsyncEnumerator<PopulationState<int>>
    {
        private bool yielded;
        private CancellationToken executionCancellation;

        public PopulationState<int> Current => ExperimentTestSupport.CreateState(1);

        public override IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
            IRandomNumberGenerator random, PopulationState<int>? initialState = null, CancellationToken ct = default)
        {
            executionCancellation = ct;
            return this;
        }

        public IAsyncEnumerator<PopulationState<int>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;

        public ValueTask<bool> MoveNextAsync()
        {
            if (yielded)
                return moveAfterFirstYield?.Invoke(executionCancellation) ?? ValueTask.FromResult(false);

            yielded = true;
            return ValueTask.FromResult(true);
        }

        public ValueTask DisposeAsync() => dispose();
    }
}

internal sealed class ExecutionProbe
{
    private int activeExecutions;
    private int maximumActiveExecutions;
    private int setupCount;
    private int executionCount;
    private readonly ConcurrentQueue<int> candidates = new();
    private readonly SemaphoreSlim executionsBegun = new(0);

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

        executionsBegun.Release();
    }

    /// <summary>Completes once the given number of executions have begun, counting those that began earlier.</summary>
    public async Task WaitForExecutionsAsync(int count, CancellationToken cancellationToken)
    {
        for (var execution = 0; execution < count; execution++)
        {
            await executionsBegun.WaitAsync(cancellationToken);
        }
    }

    public void RecordCandidate(int candidate) => candidates.Enqueue(candidate);

    public void EndExecution() => Interlocked.Decrement(ref activeExecutions);
}

internal static class ExperimentTestSupport
{
    public static ExperimentRun<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, TAlgorithm> CreateRun<TAlgorithm>(
        Experiment<int, TAlgorithm, PopulationState<int>> experiment,
        int seed = 42)
        where TAlgorithm : class, IAlgorithm<int, PopulationState<int>> =>
        experiment.CreateRun(MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(seed));

    public static GeneticAlgorithm<RealVector> CreateGeneticAlgorithm()
    {
        return new()
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            MaximumGenerations = 2
        };
    }

    public static PopulationState<int> CreateState(int candidate) => new()
    {
        Population = Population.From([EvaluatedCandidate.From(candidate, candidate)])
    };
}
