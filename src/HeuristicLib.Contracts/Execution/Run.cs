using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Execution;

public abstract class Run
{
    private readonly Lazy<ExecutionInstanceRegistry> rootRegistry;
    protected ExecutionInstanceRegistry RootRegistry => rootRegistry.Value;

    private readonly ObservationPlan observationPlan = new();

    private readonly Dictionary<IAnalyzer, IAnalyzerRunState> analyzerStates;

    protected Run(IReadOnlyList<IAnalyzer>? analyzers = null)
    {
        analyzerStates = new Dictionary<IAnalyzer, IAnalyzerRunState>(ReferenceEqualityComparer.Instance);

        foreach (var analyzer in analyzers ?? [])
        {
            var analyzerState = analyzer.CreateAnalyzerState();
            analyzerStates.Add(analyzer, analyzerState);
            analyzerState.RegisterObservations(observationPlan);
        }

        rootRegistry = new Lazy<ExecutionInstanceRegistry>(() =>
        {
            var registry = new ExecutionInstanceRegistry(this, parentRegistry: null);
            InstallObservations(registry, observationPlan);
            return registry;
        });

        _ = RootRegistry;
    }

    private static void InstallObservations(ExecutionInstanceRegistry registry, ObservationPlan observationPlan)
    {
        observationPlan.Install(registry);
    }

    public ExecutionInstanceRegistry CreateNewRegistry()
    {
        var registry = new ExecutionInstanceRegistry(this, parentRegistry: null);
        InstallObservations(registry, observationPlan);
        return registry;
    }

    public ExecutionInstanceRegistry CreateChildRegistry()
    {
        return RootRegistry.CreateChildRegistry();
    }

    public TResult GetAnalyzerResult<TResult>(IAnalyzer<TResult> analyzer)
      where TResult : class
    {
        if (!analyzerStates.TryGetValue(analyzer, out var state))
        {
            throw new KeyNotFoundException($"No analyzer found for analyzer {analyzer}");
        }

        if (state is IAnalyzerRunState<TResult> typedState)
        {
            return typedState.Result;
        }

        throw CreateAnalyzerResultTypeMismatchException<TResult>(analyzer, state);
    }

    public bool TryGetAnalyzerResult<TResult>(IAnalyzer<TResult> analyzer, [MaybeNullWhen(false)] out TResult result)
      where TResult : class
    {
        if (!analyzerStates.TryGetValue(analyzer, out var state))
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

    public TResult GetResult<TResult>(IAnalyzer<TResult> analyzer)
        where TResult : class
        => GetAnalyzerResult(analyzer);

    public bool TryGetResult<TResult>(IAnalyzer<TResult> analyzer, [MaybeNullWhen(false)] out TResult result)
        where TResult : class
        => TryGetAnalyzerResult(analyzer, out result);

    private static InvalidOperationException CreateAnalyzerResultTypeMismatchException<TResult>(IAnalyzer<TResult> analyzer, IAnalyzerRunState state)
        where TResult : class
        => new($"Analyzer {analyzer} created run state {state.GetType()} which does not implement {typeof(IAnalyzerRunState<TResult>)}.");
}

public class Run<TCandidate, TSearchSpace, TProblem, TSearchState> : Run
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private int executionStarted;

    public IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; }

    public TProblem Problem { get; }

    public Run(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, TProblem problem, IReadOnlyList<IAnalyzer>? analyzers = null)
        : base(analyzers)
    {
        Algorithm = algorithm;
        Problem = problem;
    }

    public async IAsyncEnumerable<TSearchState> StreamAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref executionStarted, 1) != 0)
        {
            throw new InvalidOperationException("A run can only be executed once. Create a new run for another execution.");
        }

        await foreach (var state in RootRegistry.Resolve(Algorithm).RunStreamingAsync(Problem, random, initialState, cancellationToken))
        {
            yield return state;
        }
    }

    public async Task<TSearchState> CompleteAsync(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        return await StreamAsync(random, initialState, cancellationToken).LastAsync(cancellationToken);
    }

    public IEnumerable<TSearchState> Stream(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        return StreamAsync(random, initialState, cancellationToken).ToBlockingEnumerable(cancellationToken);
    }

    public TSearchState Complete(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        return CompleteAsync(random, initialState, cancellationToken).GetAwaiter().GetResult();
    }
}
