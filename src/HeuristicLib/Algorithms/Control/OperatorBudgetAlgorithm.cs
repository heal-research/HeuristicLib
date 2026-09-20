using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record OperatorBudgetAlgorithm<TCandidate, TSearchState, TOperator>
    : Algorithm<OperatorBudgetAlgorithm<TCandidate, TSearchState, TOperator>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TOperator : class, IOperator
{
    public required IAlgorithm<TCandidate, TSearchState> Algorithm { get; init; }
    public required TOperator ObservedOperator { get; init; }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits(Algorithm, ObservedOperator);

    /// <summary>
    /// Gets the factory that wraps the observed operator in a counting operator.
    /// </summary>
    /// <remarks>
    /// The factory receives whatever the surrounding execution already resolves for the observed operator, which may
    /// itself be a wrapper installed by an analyzer or by an enclosing budget, so that the decorations compose. The
    /// returned operator is expected to keep the observed operator's role, as every operator wrapper does.
    /// </remarks>
    public required Func<TOperator, ObservationCounter, TOperator> CountedOperatorFactory { get; init; }

    /// <summary>
    /// Gets the counted-operator budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public int MaximumCount { get; init; }

    public override OperatorBudgetAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceResolver resolver)
    {
        var counter = new ObservationCounter();
        var childResolver = resolver.CreateChildResolver(child =>
            child.Decorate(ObservedOperator, current => CountedOperatorFactory(current, counter)));

        return new(childResolver.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), counter, MaximumCount);
    }
}

public sealed class OperatorBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly ObservationCounter counter;
    private readonly int maximumCount;

    public OperatorBudgetAlgorithmInstance(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, ObservationCounter counter, int maximumCount)
    {
        this.algorithm = algorithm;
        this.counter = counter;
        this.maximumCount = maximumCount;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in algorithm.RunStreamingAsync(problem, random, initialState, ct))
        {
            yield return state;

            if (counter.CurrentCount >= maximumCount)
            {
                yield break;
            }
        }
    }
}
