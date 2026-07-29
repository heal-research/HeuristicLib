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

    protected void AttachAnalyzer(IAnalyzer analyzer)
    {
        EnsureNotStarted();
        analyzers.Add(analyzer);
    }

    protected void AttachAnalyzers(IReadOnlyList<IAnalyzer> analyzers)
    {
        EnsureNotStarted();
        this.analyzers.AddRange(analyzers);
    }

    protected ExecutionInstanceRegistry StartExecution()
    {
        EnsureNotStarted();
        ExecutionStarted = true;

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

    public TResult GetResult<TResult>(IAnalyzer<TResult> analyzer) where TResult : class
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

        throw CreateResultTypeMismatchException(analyzer, state);
    }

    public bool TryGetResult<TResult>(IAnalyzer<TResult> analyzer, [MaybeNullWhen(false)] out TResult result) where TResult : class
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

        throw CreateResultTypeMismatchException(analyzer, state);
    }

    private Dictionary<IAnalyzer, IAnalyzerRunState> GetAnalyzerStates()
        => analyzerStates ?? throw new InvalidOperationException("Analyzer results are not available before the run starts.");

    private void EnsureNotStarted()
    {
        if (ExecutionStarted)
        {
            throw new InvalidOperationException("A run can only be configured and executed once. Create a new run for another execution.");
        }
    }

    private static InvalidOperationException CreateResultTypeMismatchException<TResult>(IAnalyzer<TResult> analyzer, IAnalyzerRunState state) where TResult : class =>
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

    public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> WithAnalyzer(IAnalyzer analyzer)
    {
        AttachAnalyzer(analyzer);
        return this;
    }

    public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> WithAnalyzer<TAnalyzer>(TAnalyzer analyzer, out TAnalyzer attachedAnalyzer)
        where TAnalyzer : IAnalyzer
    {
        attachedAnalyzer = analyzer;
        AttachAnalyzer(analyzer);
        return this;
    }

    public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> WithAnalyzers(params IReadOnlyList<IAnalyzer> analyzers)
    {
        AttachAnalyzers(analyzers);
        return this;
    }

    public ExecutionStream<TSearchState> Stream(TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        var algorithmInstance = StartExecution().Resolve(Algorithm);
        return new(StreamStates(algorithmInstance, initialState, cancellationToken), cancellationToken);
    }

    public async Task<TSearchState> CompleteAsync(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        await Stream(initialState, cancellationToken).LastAsync(cancellationToken);

    public TSearchState Complete(TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        CompleteAsync(initialState, cancellationToken).GetAwaiter().GetResult();

    private async IAsyncEnumerable<TSearchState> StreamStates(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithmInstance, TSearchState? initialState, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var state in algorithmInstance.RunStreamingAsync(Problem, Random, initialState, cancellationToken))
        {
            yield return state;
        }
    }
}
