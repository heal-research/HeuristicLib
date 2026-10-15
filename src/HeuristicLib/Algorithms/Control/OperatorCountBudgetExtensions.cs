using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Algorithms;

public static class OperatorCountBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public OperatorBudgetAlgorithm<TCandidate, TSearchState, TOperator> LimitedToCount<TOperator>(
            TOperator observedOperator,
            int maximumCount,
            IOperatorCounting<TOperator> counting)
            where TOperator : class, IOperator
        {
            return new()
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumCount = maximumCount,
                Counting = counting
            };
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IEvaluator<TCandidate>> LimitedToEvaluatorCalls(
            IEvaluator<TCandidate> evaluator,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                evaluator,
                maximumCalls,
                new EvaluatorCounting<TCandidate>(OperatorCountMetric.Calls));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IEvaluator<TCandidate>> LimitedToEvaluatedCandidates(
            IEvaluator<TCandidate> evaluator,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                evaluator,
                maximumCandidates,
                new EvaluatorCounting<TCandidate>(OperatorCountMetric.Candidates));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICreator<TCandidate>> LimitedToCreatorCalls(
            ICreator<TCandidate> creator,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                creator,
                maximumCalls,
                new CreatorCounting<TCandidate>(OperatorCountMetric.Calls));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICreator<TCandidate>> LimitedToCreatedCandidates(
            ICreator<TCandidate> creator,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                creator,
                maximumCandidates,
                new CreatorCounting<TCandidate>(OperatorCountMetric.Candidates));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICrossover<TCandidate>> LimitedToCrossoverCalls(
            ICrossover<TCandidate> crossover,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                crossover,
                maximumCalls,
                new CrossoverCounting<TCandidate>(OperatorCountMetric.Calls));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICrossover<TCandidate>> LimitedToCrossedCandidates(
            ICrossover<TCandidate> crossover,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                crossover,
                maximumCandidates,
                new CrossoverCounting<TCandidate>(OperatorCountMetric.Candidates));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IMutator<TCandidate>> LimitedToMutatorCalls(
            IMutator<TCandidate> mutator,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                mutator,
                maximumCalls,
                new MutatorCounting<TCandidate>(OperatorCountMetric.Calls));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IMutator<TCandidate>> LimitedToMutatedCandidates(
            IMutator<TCandidate> mutator,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                mutator,
                maximumCandidates,
                new MutatorCounting<TCandidate>(OperatorCountMetric.Candidates));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IRefiner<TCandidate>> LimitedToRefinerCalls(
            IRefiner<TCandidate> refiner,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                refiner,
                maximumCalls,
                new RefinerCounting<TCandidate>(OperatorCountMetric.Calls));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IRefiner<TCandidate>> LimitedToRefinedCandidates(
            IRefiner<TCandidate> refiner,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                refiner,
                maximumCandidates,
                new RefinerCounting<TCandidate>(OperatorCountMetric.Candidates));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ISelector<TCandidate>> LimitedToSelectorCalls(
            ISelector<TCandidate> selector,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                selector,
                maximumCalls,
                new SelectorCounting<TCandidate>(OperatorCountMetric.Calls));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ISelector<TCandidate>> LimitedToSelectedCandidates(
            ISelector<TCandidate> selector,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                selector,
                maximumCandidates,
                new SelectorCounting<TCandidate>(OperatorCountMetric.Candidates));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IReplacer<TCandidate>> LimitedToReplacerCalls(
            IReplacer<TCandidate> replacer,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                replacer,
                maximumCalls,
                new ReplacerCounting<TCandidate>(OperatorCountMetric.Calls));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IReplacer<TCandidate>> LimitedToReplacementCandidates(
            IReplacer<TCandidate> replacer,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                replacer,
                maximumCandidates,
                new ReplacerCounting<TCandidate>(OperatorCountMetric.Candidates));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IInterceptor<TCandidate>> LimitedToInterceptorCalls(
            IInterceptor<TCandidate> interceptor,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                interceptor,
                maximumCalls,
                new InterceptorCounting<TCandidate>());
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ITerminator<TCandidate>> LimitedToTerminatorCalls(
            ITerminator<TCandidate> terminator,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                terminator,
                maximumCalls,
                new TerminatorCounting<TCandidate>());
        }
    }
}
