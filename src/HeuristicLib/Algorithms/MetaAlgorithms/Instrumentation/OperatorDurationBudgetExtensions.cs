using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public static class OperatorDurationBudgetExtensions
{
    extension<TG, TS, TP, TSearchState, TObservedInstance>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
        where TObservedInstance : class, IOperatorInstance
    {
        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance> WithMaxOperatorDuration<TOperator>(
            TOperator observedOperator,
            TimeSpan maximumDuration,
            Func<TOperator, ObservationDuration, TimeProvider, IOperator<TObservedInstance>> measuredOperatorFactory)
            where TOperator : IOperator<TObservedInstance>
        {
            return algorithm.WithMaxOperatorDuration(
                observedOperator,
                maximumDuration,
                TimeProvider.System,
                measuredOperatorFactory);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance> WithMaxOperatorDuration<TOperator>(
            TOperator observedOperator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider,
            Func<TOperator, ObservationDuration, TimeProvider, IOperator<TObservedInstance>> measuredOperatorFactory)
            where TOperator : IOperator<TObservedInstance>
        {
            return new OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance>
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumDuration = maximumDuration,
                TimeProvider = timeProvider,
                MeasuredOperatorFactory = measuredOperatorFactory
            };
        }
    }

    extension<TG, TS, TP, TSearchState>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
    {
        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatorDuration(
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxEvaluatorDuration(maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatorDuration(
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxEvaluatorDuration(algorithm.Evaluator, maximumDuration, timeProvider);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatorDuration(
            IEvaluator<TG, TS, TP> evaluator,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxEvaluatorDuration(evaluator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatorDuration(
            IEvaluator<TG, TS, TP> evaluator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                evaluator,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureEvaluatorDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IMutator<TG, TS, TP>, IMutatorInstance<TG, TS, TP>> WithMaxMutatorDuration(
            IMutator<TG, TS, TP> mutator,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxMutatorDuration(mutator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IMutator<TG, TS, TP>, IMutatorInstance<TG, TS, TP>> WithMaxMutatorDuration(
            IMutator<TG, TS, TP> mutator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                mutator,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureMutatorDuration(duration, timeProvider));
        }
    }
}
