using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record IterativeAlgorithm<TSelf, TCandidate, TSearchState>
    : Algorithm<TSelf, TCandidate, TSearchState>, IIterativeAlgorithm<TCandidate>
    where TSelf : IterativeAlgorithm<TSelf, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public IInterceptor<TCandidate>? Interceptor { get; init; }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Interceptor);

    /// <summary>Prepares persistent execution data and returns the contextual execution factory.</summary>
    public override ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>() => CreateIterationFactory<TRunSearchSpace, TRunProblem>();

    /// <summary>Prepares persistent execution data and returns a factory accepting the construction scope.</summary>
    /// <remarks>Allocate shared state here; resolve the optional interceptor and other children and construct nodes in the returned factory.</remarks>
    protected abstract ExecutionFactory<IterativeAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateIterationFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

/// <remarks>
/// Derive from this base only when the algorithm reads its search space or problem itself; naming them costs two type
/// arguments on every mention of the configuration. An algorithm that just hands them to its operators belongs on
/// <see cref="IterativeAlgorithm{TSelf, TCandidate, TSearchState}"/>.
/// <para>
/// A run this algorithm was not written for is reported when the execution graph is built.
/// </para>
/// </remarks>
public abstract record IterativeAlgorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    : IterativeAlgorithm<TSelf, TCandidate, TSearchState>
    where TSelf : IterativeAlgorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    /// <summary>Prepares persistent execution data and returns a factory accepting the construction scope.</summary>
    /// <remarks>Allocate shared state here; resolve the optional interceptor and other children and construct nodes in the returned factory.</remarks>
    protected abstract ExecutionFactory<IterativeAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateIterationFactory();

    /// <summary>Prepares persistent execution data and returns the contextual execution factory.</summary>
    public ExecutionFactory<IterativeAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateExecutionFactory() => CreateIterationFactory();

    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution)
        && execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace))
        && execution.Problem.IsAssignableTo(typeof(TProblem));

    public sealed override ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
    {
        if (!typeof(TRunSearchSpace).IsAssignableTo(typeof(TSearchSpace)) || !typeof(TRunProblem).IsAssignableTo(typeof(TProblem)))
        {
            throw ExecutionSignature.Mismatch(
                this,
                ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState)),
                ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem)));
        }

        return (ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>>)CreateExecutionFactory();
    }

    protected sealed override ExecutionFactory<IterativeAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateIterationFactory<TRunSearchSpace, TRunProblem>() =>
        throw new NotSupportedException("A bound algorithm prepares its factory through its own preparation method.");
}

public abstract class IterativeAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor;

    protected IterativeAlgorithmExecution(IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor)
    {
        this.interceptor = interceptor;
    }

    protected abstract TSearchState ExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random);

    protected virtual bool TryExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random, [NotNullWhen(true)] out TSearchState? nextState)
    {
        nextState = ExecuteStep(previousState, problem, random);
        return true;
    }

    protected virtual bool HasCompleted(int yieldedStateCount, TSearchState? previousState, TProblem problem) => false;

    protected virtual bool IsTerminalState(TSearchState state, int yieldedStateCount, TSearchState? previousState, TProblem problem) => false;

    public sealed override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var previousState = initialState;

        foreach (var yieldedStateCount in Enumerable.InfiniteSequence(0, 1))
        {
            if (HasCompleted(yieldedStateCount, previousState, problem))
            {
                yield break;
            }

            ct.ThrowIfCancellationRequested();
            var iterationRandom = random.Fork(yieldedStateCount);
            if (!TryExecuteStep(previousState, problem, iterationRandom, out var newState))
            {
                yield break;
            }

            if (interceptor is not null)
            {
                newState = interceptor.Transform(newState, previousState, iterationRandom, problem.SearchSpace, problem);
            }

            var isTerminalState = IsTerminalState(newState, yieldedStateCount + 1, previousState, problem);
            yield return newState;

            if (isTerminalState)
            {
                yield break;
            }

            await Task.Yield();
            previousState = newState;
        }
    }
}
