using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;

namespace HEAL.HeuristicLib.Algorithms;

public static class OperatorDurationBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, TOperator> LimitedToOperatorDuration<TOperator>(
            TOperator observedOperator,
            TimeSpan maximumDuration,
            Func<TOperator, ObservationDuration, TimeProvider, TOperator> measuredOperatorFactory)
            where TOperator : class, IOperator
        {
            return algorithm.LimitedToOperatorDuration(
                observedOperator,
                maximumDuration,
                TimeProvider.System,
                measuredOperatorFactory);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, TOperator> LimitedToOperatorDuration<TOperator>(
            TOperator observedOperator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider,
            Func<TOperator, ObservationDuration, TimeProvider, TOperator> measuredOperatorFactory)
            where TOperator : class, IOperator
        {
            return new()
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumDuration = maximumDuration,
                TimeProvider = timeProvider,
                MeasuredOperatorFactory = measuredOperatorFactory
            };
        }
    }

    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IEvaluator<TCandidate>> LimitedToEvaluatorDuration(
            IEvaluator<TCandidate> evaluator,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToEvaluatorDuration(evaluator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IEvaluator<TCandidate>> LimitedToEvaluatorDuration(
            IEvaluator<TCandidate> evaluator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                evaluator,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, ICreator<TCandidate>> LimitedToCreatorDuration(
            ICreator<TCandidate> creator,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToCreatorDuration(creator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, ICreator<TCandidate>> LimitedToCreatorDuration(
            ICreator<TCandidate> creator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                creator,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, ICrossover<TCandidate>> LimitedToCrossoverDuration(
            ICrossover<TCandidate> crossover,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToCrossoverDuration(crossover, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, ICrossover<TCandidate>> LimitedToCrossoverDuration(
            ICrossover<TCandidate> crossover,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                crossover,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IMutator<TCandidate>> LimitedToMutatorDuration(
            IMutator<TCandidate> mutator,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToMutatorDuration(mutator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IMutator<TCandidate>> LimitedToMutatorDuration(
            IMutator<TCandidate> mutator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                mutator,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IRefiner<TCandidate>> LimitedToRefinerDuration(
            IRefiner<TCandidate> refiner,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToRefinerDuration(refiner, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IRefiner<TCandidate>> LimitedToRefinerDuration(
            IRefiner<TCandidate> refiner,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                refiner,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, ISelector<TCandidate>> LimitedToSelectorDuration(
            ISelector<TCandidate> selector,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToSelectorDuration(selector, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, ISelector<TCandidate>> LimitedToSelectorDuration(
            ISelector<TCandidate> selector,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                selector,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IReplacer<TCandidate>> LimitedToReplacerDuration(
            IReplacer<TCandidate> replacer,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToReplacerDuration(replacer, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IReplacer<TCandidate>> LimitedToReplacerDuration(
            IReplacer<TCandidate> replacer,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                replacer,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IInterceptor<TCandidate>> LimitedToInterceptorDuration(
            IInterceptor<TCandidate> interceptor,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToInterceptorDuration(interceptor, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IInterceptor<TCandidate>> LimitedToInterceptorDuration(
            IInterceptor<TCandidate> interceptor,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                interceptor,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, ITerminator<TCandidate>> LimitedToTerminatorDuration(
            ITerminator<TCandidate> terminator,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToTerminatorDuration(terminator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, ITerminator<TCandidate>> LimitedToTerminatorDuration(
            ITerminator<TCandidate> terminator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                terminator,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureDuration(duration, timeProvider));
        }
    }
}
