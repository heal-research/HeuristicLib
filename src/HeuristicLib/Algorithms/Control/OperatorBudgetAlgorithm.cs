using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
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
    /// Gets the strategy creating advice that counts the observed operator's work into the supplied counter.
    /// </summary>
    /// <remarks>
    /// The advice applies only inside this budget's algorithm, so enclosing budgets and observations compose. If the
    /// algorithm does not reference the observed operator, nothing is counted and the budget never stops.
    /// </remarks>
    public required IOperatorCounting<TOperator> Counting { get; init; }

    /// <summary>
    /// Gets the counted-operator budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public int MaximumCount { get; init; }

    public override ExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
    {
        var counter = new CountAccumulator();
        var counting = Aspect.Create(Pointcut.Reference(ObservedOperator), Counting.CreateAdvice(counter));
        return scope =>
        {
            var childScope = scope.With(counting);
            return new OperatorBudgetAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(childScope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), counter, MaximumCount);
        };
    }
}

public sealed class OperatorBudgetAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly CountAccumulator counter;
    private readonly int maximumCount;

    public OperatorBudgetAlgorithmExecution(IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, CountAccumulator counter, int maximumCount)
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
