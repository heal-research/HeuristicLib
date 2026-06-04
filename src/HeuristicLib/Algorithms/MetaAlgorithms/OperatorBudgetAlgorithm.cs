using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public record OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance>
    : IAlgorithm<TG, TS, TP, TSearchState>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TSearchState : class, ISearchState
    where TOperator : IOperator<TObservedInstance>
    where TObservedInstance : class, IOperatorInstance
{
    public required IAlgorithm<TG, TS, TP, TSearchState> Algorithm { get; init; }
    public required TOperator ObservedOperator { get; init; }
    public required Func<TOperator, InvocationCounter, IOperator<TObservedInstance>> CountedOperatorFactory { get; init; }

    public int MaximumCount
    {
        get;
        init => field = value > 0
          ? value
          : throw new ArgumentOutOfRangeException(nameof(MaximumCount), "MaximumCount must be positive.");
    }

    public IEvaluator<TG, TS, TP> Evaluator => Algorithm.Evaluator;

    public IAlgorithmInstance<TG, TS, TP, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        var counter = new InvocationCounter();
        var countedOperator = CountedOperatorFactory(ObservedOperator, counter);
        var childRegistry = instanceRegistry.CreateChildRegistry();
        childRegistry.PreRegister(ObservedOperator, countedOperator);

        return new OperatorBudgetAlgorithmInstance<TG, TS, TP, TSearchState>(
            childRegistry.Resolve(Algorithm),
            counter,
            MaximumCount);
    }
}

public sealed class OperatorBudgetAlgorithmInstance<TG, TS, TP, TSearchState>
    : IAlgorithmInstance<TG, TS, TP, TSearchState>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmInstance<TG, TS, TP, TSearchState> algorithm;
    private readonly InvocationCounter counter;
    private readonly int maximumCalls;

    public OperatorBudgetAlgorithmInstance(
        IAlgorithmInstance<TG, TS, TP, TSearchState> algorithm,
        InvocationCounter counter,
        int maximumCalls)
    {
        this.algorithm = algorithm;
        this.counter = counter;
        this.maximumCalls = maximumCalls;
    }

    public async IAsyncEnumerable<TSearchState> RunStreamingAsync(
        TP problem,
        IRandomNumberGenerator random,
        TSearchState? initialState = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in algorithm.RunStreamingAsync(problem, random, initialState, ct))
        {
            yield return state;

            if (counter.CurrentCount >= maximumCalls)
            {
                yield break;
            }
        }
    }
}

public static class OperatorBudgetAlgorithmExtensions
{
    extension<TG, TS, TP, TSearchState, TObservedInstance>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
        where TObservedInstance : class, IOperatorInstance
    {
        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance> WithMaxOperatorCalls<TOperator>(
            TOperator observedOperator,
            int maximumCalls,
            Func<TOperator, InvocationCounter, IOperator<TObservedInstance>> countedOperatorFactory)
            where TOperator : IOperator<TObservedInstance>
        {
            return new OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance>
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumCount = maximumCalls,
                CountedOperatorFactory = countedOperatorFactory
            };
        }
    }

    extension<TG, TS, TP, TSearchState>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
    {
        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatorCalls(int maximumCalls)
        {
            return algorithm.WithMaxOperatorCalls(
                algorithm.Evaluator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountEvaluatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatedGenotypes(int maximumGenotypes)
        {
            return algorithm.WithMaxOperatorCalls(
                algorithm.Evaluator,
                maximumGenotypes,
                static (observedOperator, counter) => observedOperator.CountEvaluatedGenotypes(counter));
        }
    }
}
