using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

public static class OperatorDurationBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, TOperator> LimitedToOperatorDuration<TOperator>(
            TOperator observedOperator,
            TimeSpan maximumDuration,
            IOperatorDurationMeasuring<TOperator> durationMeasuring)
            where TOperator : class, IOperator
        {
            return algorithm.LimitedToOperatorDuration(
                observedOperator,
                maximumDuration,
                TimeProvider.System,
                durationMeasuring);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, TOperator> LimitedToOperatorDuration<TOperator>(
            TOperator observedOperator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider,
            IOperatorDurationMeasuring<TOperator> durationMeasuring)
            where TOperator : class, IOperator
        {
            return new()
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumDuration = maximumDuration,
                TimeProvider = timeProvider,
                DurationMeasuring = durationMeasuring
            };
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IOperator<Evaluation, TCandidate>> LimitedToEvaluatorDuration(
            IOperator<Evaluation, TCandidate> evaluator,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToEvaluatorDuration(evaluator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IOperator<Evaluation, TCandidate>> LimitedToEvaluatorDuration(
            IOperator<Evaluation, TCandidate> evaluator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                evaluator,
                maximumDuration,
                timeProvider,
                new OperatorDurationMeasuring<Evaluation, TCandidate>());
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
                new CreatorDurationMeasuring<TCandidate>());
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
                new CrossoverDurationMeasuring<TCandidate>());
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IOperator<Mutation, TCandidate>> LimitedToMutatorDuration(
            IOperator<Mutation, TCandidate> mutator,
            TimeSpan maximumDuration)
        {
            return algorithm.LimitedToMutatorDuration(mutator, maximumDuration, TimeProvider.System);
        }

        public OperatorDurationBudgetAlgorithm<TCandidate, TSearchState, IOperator<Mutation, TCandidate>> LimitedToMutatorDuration(
            IOperator<Mutation, TCandidate> mutator,
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return algorithm.LimitedToOperatorDuration(
                mutator,
                maximumDuration,
                timeProvider,
                new OperatorDurationMeasuring<Mutation, TCandidate>());
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
                new RefinerDurationMeasuring<TCandidate>());
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
                new SelectorDurationMeasuring<TCandidate>());
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
                new ReplacerDurationMeasuring<TCandidate>());
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
                new InterceptorDurationMeasuring<TCandidate>());
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
                new TerminatorDurationMeasuring<TCandidate>());
        }
    }
}
