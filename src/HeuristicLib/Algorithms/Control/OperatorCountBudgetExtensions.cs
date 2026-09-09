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
        public OperatorBudgetAlgorithm<TCandidate, TSearchState, TOperator> WithMaxCount<TOperator>(
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
        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IEvaluator<TCandidate>> WithMaxEvaluatorCalls(
            IEvaluator<TCandidate> evaluator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                evaluator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IEvaluator<TCandidate>> WithMaxEvaluatedCandidates(
            IEvaluator<TCandidate> evaluator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                evaluator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICreator<TCandidate>> WithMaxCreatorCalls(
            ICreator<TCandidate> creator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                creator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICreator<TCandidate>> WithMaxCreatedCandidates(
            ICreator<TCandidate> creator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                creator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICrossover<TCandidate>> WithMaxCrossoverCalls(
            ICrossover<TCandidate> crossover,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                crossover,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ICrossover<TCandidate>> WithMaxCrossedCandidates(
            ICrossover<TCandidate> crossover,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                crossover,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IMutator<TCandidate>> WithMaxMutatorCalls(
            IMutator<TCandidate> mutator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IMutator<TCandidate>> WithMaxMutatedCandidates(
            IMutator<TCandidate> mutator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IRefiner<TCandidate>> WithMaxRefinerCalls(
            IRefiner<TCandidate> refiner,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                refiner,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IRefiner<TCandidate>> WithMaxRefinedCandidates(
            IRefiner<TCandidate> refiner,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                refiner,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ISelector<TCandidate>> WithMaxSelectorCalls(
            ISelector<TCandidate> selector,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                selector,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ISelector<TCandidate>> WithMaxSelectedCandidates(
            ISelector<TCandidate> selector,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                selector,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IReplacer<TCandidate>> WithMaxReplacerCalls(
            IReplacer<TCandidate> replacer,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                replacer,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IReplacer<TCandidate>> WithMaxReplacementCandidates(
            IReplacer<TCandidate> replacer,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                replacer,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, IInterceptor<TCandidate>> WithMaxInterceptorCalls(
            IInterceptor<TCandidate> interceptor,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                interceptor,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchState, ITerminator<TCandidate>> WithMaxTerminatorCalls(
            ITerminator<TCandidate> terminator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                terminator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCalls(counter));
        }
    }
}
