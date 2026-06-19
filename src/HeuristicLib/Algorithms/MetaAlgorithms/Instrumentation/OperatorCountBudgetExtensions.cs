using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public static class OperatorCountBudgetExtensions
{
    extension<TG, TS, TP, TSearchState, TObservedInstance>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
        where TObservedInstance : class, IOperatorInstance
    {
        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance> WithMaxCount<TOperator>(
            TOperator observedOperator,
            int maximumCount,
            Func<TOperator, ObservationCounter, IOperator<TObservedInstance>> countedOperatorFactory)
            where TOperator : IOperator<TObservedInstance>
        {
            return new OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance>
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumCount = maximumCount,
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
            return algorithm.WithMaxCount(
                algorithm.Evaluator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountEvaluatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatedGenotypes(int maximumGenotypes)
        {
            return algorithm.WithMaxCount(
                algorithm.Evaluator,
                maximumGenotypes,
                static (observedOperator, counter) => observedOperator.CountEvaluatedGenotypes(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IMutator<TG, TS, TP>, IMutatorInstance<TG, TS, TP>> WithMaxMutatorCalls(
            IMutator<TG, TS, TP> mutator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountMutatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IMutator<TG, TS, TP>, IMutatorInstance<TG, TS, TP>> WithMaxMutatedGenotypes(
            IMutator<TG, TS, TP> mutator,
            int maximumGenotypes)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumGenotypes,
                static (observedOperator, counter) => observedOperator.CountMutatedGenotypes(counter));
        }
    }
}
