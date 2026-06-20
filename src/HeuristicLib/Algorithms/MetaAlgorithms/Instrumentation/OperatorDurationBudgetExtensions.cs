using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
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

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, ICreator<TG, TS, TP>, ICreatorInstance<TG, TS, TP>> WithMaxCreatorDuration(
            ICreator<TG, TS, TP> creator,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxCreatorDuration(creator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, ICreator<TG, TS, TP>, ICreatorInstance<TG, TS, TP>> WithMaxCreatorDuration(
            ICreator<TG, TS, TP> creator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                creator,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureCreatorDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, ICrossover<TG, TS, TP>, ICrossoverInstance<TG, TS, TP>> WithMaxCrossoverDuration(
            ICrossover<TG, TS, TP> crossover,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxCrossoverDuration(crossover, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, ICrossover<TG, TS, TP>, ICrossoverInstance<TG, TS, TP>> WithMaxCrossoverDuration(
            ICrossover<TG, TS, TP> crossover,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                crossover,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureCrossoverDuration(duration, timeProvider));
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

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, ISelector<TG, TS, TP>, ISelectorInstance<TG, TS, TP>> WithMaxSelectorDuration(
            ISelector<TG, TS, TP> selector,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxSelectorDuration(selector, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, ISelector<TG, TS, TP>, ISelectorInstance<TG, TS, TP>> WithMaxSelectorDuration(
            ISelector<TG, TS, TP> selector,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                selector,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureSelectorDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IReplacer<TG, TS, TP>, IReplacerInstance<TG, TS, TP>> WithMaxReplacerDuration(
            IReplacer<TG, TS, TP> replacer,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxReplacerDuration(replacer, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IReplacer<TG, TS, TP>, IReplacerInstance<TG, TS, TP>> WithMaxReplacerDuration(
            IReplacer<TG, TS, TP> replacer,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                replacer,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureReplacerDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IInterceptor<TG, TS, TP, TSearchState>, IInterceptorInstance<TG, TS, TP, TSearchState>> WithMaxInterceptorDuration(
            IInterceptor<TG, TS, TP, TSearchState> interceptor,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxInterceptorDuration(interceptor, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, IInterceptor<TG, TS, TP, TSearchState>, IInterceptorInstance<TG, TS, TP, TSearchState>> WithMaxInterceptorDuration(
            IInterceptor<TG, TS, TP, TSearchState> interceptor,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                interceptor,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureInterceptorDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, ITerminator<TG, TS, TP, TSearchState>, ITerminatorInstance<TG, TS, TP, TSearchState>> WithMaxTerminatorDuration(
            ITerminator<TG, TS, TP, TSearchState> terminator,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxTerminatorDuration(terminator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TG, TS, TP, TSearchState, ITerminator<TG, TS, TP, TSearchState>, ITerminatorInstance<TG, TS, TP, TSearchState>> WithMaxTerminatorDuration(
            ITerminator<TG, TS, TP, TSearchState> terminator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                terminator,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureTerminatorDuration(duration, timeProvider));
        }
    }
}
