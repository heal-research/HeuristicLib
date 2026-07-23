using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Execution;

public abstract class AlgorithmRun
{
    private readonly List<IAnalyzer> analyzers = [];
    private Dictionary<IAnalyzer, IAnalyzerRunState>? analyzerStates;

    public bool ExecutionStarted { get; private set; }

    protected AlgorithmRun(IReadOnlyList<IAnalyzer>? analyzers = null)
    {
        if (analyzers is not null)
        {
            this.analyzers.AddRange(analyzers);
        }
    }

    public AlgorithmRun WithAnalyzer(IAnalyzer analyzer)
    {
        EnsureNotStarted();
        analyzers.Add(analyzer);

        return this;
    }

    public AlgorithmRun WithAnalyzers(params IReadOnlyList<IAnalyzer> analyzers)
    {
        EnsureNotStarted();
        this.analyzers.AddRange(analyzers);

        return this;
    }

    protected void BeginExecution()
    {
        EnsureNotStarted();
        ExecutionStarted = true;
    }

    protected ExecutionInstanceRegistry CreateExecutionRegistry()
    {
        var observationPlan = new ObservationPlan();
        analyzerStates = new Dictionary<IAnalyzer, IAnalyzerRunState>(ReferenceEqualityComparer.Instance);

        foreach (var analyzer in analyzers)
        {
            var analyzerState = analyzer.CreateAnalyzerState();
            analyzerStates.Add(analyzer, analyzerState);
            analyzerState.RegisterObservations(observationPlan);
        }

        var registry = new ExecutionInstanceRegistry();
        observationPlan.Install(registry);
        return registry;
    }

    public TResult GetAnalyzerResult<TResult>(IAnalyzer<TResult> analyzer) where TResult : class
    {
        var states = GetAnalyzerStates();
        if (!states.TryGetValue(analyzer, out var state))
        {
            throw new KeyNotFoundException($"No analyzer found for analyzer {analyzer}");
        }

        if (state is IAnalyzerRunState<TResult> typedState)
        {
            return typedState.Result;
        }

        throw CreateAnalyzerResultTypeMismatchException<TResult>(analyzer, state);
    }

    public bool TryGetAnalyzerResult<TResult>(IAnalyzer<TResult> analyzer, [MaybeNullWhen(false)] out TResult result) where TResult : class
    {
        var states = GetAnalyzerStates();
        if (!states.TryGetValue(analyzer, out var state))
        {
            result = null;
            return false;
        }

        if (state is IAnalyzerRunState<TResult> typedState)
        {
            result = typedState.Result;
            return true;
        }

        throw CreateAnalyzerResultTypeMismatchException<TResult>(analyzer, state);
    }

    public TResult GetResult<TResult>(IAnalyzer<TResult> analyzer) where TResult : class => GetAnalyzerResult(analyzer);

    public bool TryGetResult<TResult>(IAnalyzer<TResult> analyzer, [MaybeNullWhen(false)] out TResult result) where TResult : class => TryGetAnalyzerResult(analyzer, out result);

    private Dictionary<IAnalyzer, IAnalyzerRunState> GetAnalyzerStates()
        => analyzerStates ?? throw new InvalidOperationException("Analyzer results are not available before the run starts.");

    private void EnsureNotStarted()
    {
        if (ExecutionStarted)
        {
            throw new InvalidOperationException("A run can only be configured and executed once. Create a new run for another execution.");
        }
    }

    private static InvalidOperationException CreateAnalyzerResultTypeMismatchException<TResult>(IAnalyzer<TResult> analyzer, IAnalyzerRunState state) where TResult : class =>
        new($"Analyzer {analyzer} created run state {state.GetType()} which does not implement {typeof(IAnalyzerRunState<TResult>)}.");
}

public sealed class AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> : AlgorithmRun
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; }

    public TProblem Problem { get; }

    public IRandomNumberGenerator Random { get; }

    public AlgorithmRun(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, TProblem problem, IRandomNumberGenerator random)
    {
        Algorithm = algorithm;
        Problem = problem;
        Random = random;
    }

    public new AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> WithAnalyzer(IAnalyzer analyzer)
    {
        base.WithAnalyzer(analyzer);
        return this;
    }

    public new AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> WithAnalyzers(params IReadOnlyList<IAnalyzer> analyzers)
    {
        base.WithAnalyzers(analyzers);
        return this;
    }

    public ExecutionStream<TSearchState> Stream(TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        BeginExecution();
        var algorithmInstance = CreateExecutionRegistry().Resolve(Algorithm);
        return new ExecutionStream<TSearchState>(StreamStates(algorithmInstance, initialState, cancellationToken), cancellationToken);
    }

    public async Task<TSearchState> CompleteAsync(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        await Stream(initialState, cancellationToken).LastAsync(cancellationToken);

    public TSearchState Complete(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        CompleteAsync(initialState, cancellationToken).GetAwaiter().GetResult();

    private async IAsyncEnumerable<TSearchState> StreamStates(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithmInstance, TSearchState? initialState, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var state in algorithmInstance.RunStreamingAsync(Problem, Random, initialState, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return state;
        }
    }
}
