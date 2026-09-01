using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
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
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public static class OperatorDurationBudgetExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator> WithMaxOperatorDuration<TOperator>(
            TOperator observedOperator,
            TimeSpan maximumDuration,
            Func<TOperator, ObservationDuration, TimeProvider, TOperator> measuredOperatorFactory)
            where TOperator : class, IOperator
        {
            return algorithm.WithMaxOperatorDuration(
                observedOperator,
                maximumDuration,
                TimeProvider.System,
                measuredOperatorFactory);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator> WithMaxOperatorDuration<TOperator>(
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

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IEvaluator<TCandidate, TSearchSpace, TProblem>> WithMaxEvaluatorDuration(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxEvaluatorDuration(evaluator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IEvaluator<TCandidate, TSearchSpace, TProblem>> WithMaxEvaluatorDuration(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
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

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICreator<TCandidate, TSearchSpace, TProblem>> WithMaxCreatorDuration(
            ICreator<TCandidate, TSearchSpace, TProblem> creator,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxCreatorDuration(creator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICreator<TCandidate, TSearchSpace, TProblem>> WithMaxCreatorDuration(
            ICreator<TCandidate, TSearchSpace, TProblem> creator,
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

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICrossover<TCandidate, TSearchSpace, TProblem>> WithMaxCrossoverDuration(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxCrossoverDuration(crossover, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICrossover<TCandidate, TSearchSpace, TProblem>> WithMaxCrossoverDuration(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
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

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IMutator<TCandidate>> WithMaxMutatorDuration(
            IMutator<TCandidate> mutator,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxMutatorDuration(mutator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IMutator<TCandidate>> WithMaxMutatorDuration(
            IMutator<TCandidate> mutator,
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

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IRefiner<TCandidate, TSearchSpace, TProblem>> WithMaxRefinerDuration(
            IRefiner<TCandidate, TSearchSpace, TProblem> refiner,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxRefinerDuration(refiner, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IRefiner<TCandidate, TSearchSpace, TProblem>> WithMaxRefinerDuration(
            IRefiner<TCandidate, TSearchSpace, TProblem> refiner,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.WithMaxOperatorDuration(
                refiner,
                maximumDuration,
                timeProvider,
                static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureRefinerDuration(duration, timeProvider));
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ISelector<TCandidate, TSearchSpace, TProblem>> WithMaxSelectorDuration(
            ISelector<TCandidate, TSearchSpace, TProblem> selector,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxSelectorDuration(selector, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ISelector<TCandidate, TSearchSpace, TProblem>> WithMaxSelectorDuration(
            ISelector<TCandidate, TSearchSpace, TProblem> selector,
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

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IReplacer<TCandidate, TSearchSpace, TProblem>> WithMaxReplacerDuration(
            IReplacer<TCandidate, TSearchSpace, TProblem> replacer,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxReplacerDuration(replacer, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IReplacer<TCandidate, TSearchSpace, TProblem>> WithMaxReplacerDuration(
            IReplacer<TCandidate, TSearchSpace, TProblem> replacer,
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

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> WithMaxInterceptorDuration(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxInterceptorDuration(interceptor, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> WithMaxInterceptorDuration(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
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

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> WithMaxTerminatorDuration(
            ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator,
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxTerminatorDuration(terminator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> WithMaxTerminatorDuration(
            ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator,
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
