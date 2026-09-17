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

public static class OperatorCountBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public OperatorBudgetAlgorithm<TCandidate, TSearchState, TOperator> LimitedToCount<TOperator>(
            TOperator observedOperator,
            int maximumCount,
            Func<TOperator, ObservationCounter, TOperator> countedOperatorFactory)
            where TOperator : class, IOperator
        {
            return new()
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumCount = maximumCount,
                CountedOperatorFactory = countedOperatorFactory
            };
        }
    }

    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IEvaluator<TCandidate>> LimitedToEvaluatorCalls(
            IEvaluator<TCandidate> evaluator,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                evaluator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IEvaluator<TCandidate>> LimitedToEvaluatedCandidates(
            IEvaluator<TCandidate> evaluator,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                evaluator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICreator<TCandidate>> LimitedToCreatorCalls(
            ICreator<TCandidate> creator,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                creator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICreator<TCandidate>> LimitedToCreatedCandidates(
            ICreator<TCandidate> creator,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                creator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICrossover<TCandidate>> LimitedToCrossoverCalls(
            ICrossover<TCandidate> crossover,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                crossover,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICrossover<TCandidate>> LimitedToCrossedCandidates(
            ICrossover<TCandidate> crossover,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                crossover,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IMutator<TCandidate>> LimitedToMutatorCalls(
            IMutator<TCandidate> mutator,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                mutator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IMutator<TCandidate>> LimitedToMutatedCandidates(
            IMutator<TCandidate> mutator,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                mutator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IRefiner<TCandidate>> LimitedToRefinerCalls(
            IRefiner<TCandidate> refiner,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                refiner,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IRefiner<TCandidate>> LimitedToRefinedCandidates(
            IRefiner<TCandidate> refiner,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                refiner,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ISelector<TCandidate>> LimitedToSelectorCalls(
            ISelector<TCandidate> selector,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                selector,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ISelector<TCandidate>> LimitedToSelectedCandidates(
            ISelector<TCandidate> selector,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                selector,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IReplacer<TCandidate>> LimitedToReplacerCalls(
            IReplacer<TCandidate> replacer,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                replacer,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IReplacer<TCandidate>> LimitedToReplacementCandidates(
            IReplacer<TCandidate> replacer,
            int maximumCandidates)
        {
            return algorithm.LimitedToCount(
                replacer,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IInterceptor<TCandidate>> LimitedToInterceptorCalls(
            IInterceptor<TCandidate> interceptor,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                interceptor,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ITerminator<TCandidate>> LimitedToTerminatorCalls(
            ITerminator<TCandidate> terminator,
            int maximumCalls)
        {
            return algorithm.LimitedToCount(
                terminator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }
    }
}
