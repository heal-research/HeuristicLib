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

public static class OperatorCountBudgetExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator> WithMaxCount<TOperator>(
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

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IEvaluator<TCandidate, TSearchSpace, TProblem>> WithMaxEvaluatorCalls(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                evaluator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountEvaluatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IEvaluator<TCandidate, TSearchSpace, TProblem>> WithMaxEvaluatedCandidates(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                evaluator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountEvaluatedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICreator<TCandidate, TSearchSpace, TProblem>> WithMaxCreatorCalls(
            ICreator<TCandidate, TSearchSpace, TProblem> creator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                creator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCreatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICreator<TCandidate, TSearchSpace, TProblem>> WithMaxCreatedCandidates(
            ICreator<TCandidate, TSearchSpace, TProblem> creator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                creator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCreatedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICrossover<TCandidate, TSearchSpace, TProblem>> WithMaxCrossoverCalls(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                crossover,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCrossoverCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICrossover<TCandidate, TSearchSpace, TProblem>> WithMaxCrossedCandidates(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                crossover,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCrossedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IMutator<TCandidate>> WithMaxMutatorCalls(
            IMutator<TCandidate> mutator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountMutatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IMutator<TCandidate>> WithMaxMutatedCandidates(
            IMutator<TCandidate> mutator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountMutatedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IRefiner<TCandidate, TSearchSpace, TProblem>> WithMaxRefinerCalls(
            IRefiner<TCandidate, TSearchSpace, TProblem> refiner,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                refiner,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountRefinerCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IRefiner<TCandidate, TSearchSpace, TProblem>> WithMaxRefinedCandidates(
            IRefiner<TCandidate, TSearchSpace, TProblem> refiner,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                refiner,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountRefinedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ISelector<TCandidate, TSearchSpace, TProblem>> WithMaxSelectorCalls(
            ISelector<TCandidate, TSearchSpace, TProblem> selector,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                selector,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountSelectorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ISelector<TCandidate, TSearchSpace, TProblem>> WithMaxSelectedCandidates(
            ISelector<TCandidate, TSearchSpace, TProblem> selector,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                selector,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountSelectedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IReplacer<TCandidate, TSearchSpace, TProblem>> WithMaxReplacerCalls(
            IReplacer<TCandidate, TSearchSpace, TProblem> replacer,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                replacer,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountReplacerCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IReplacer<TCandidate, TSearchSpace, TProblem>> WithMaxReplacementCandidates(
            IReplacer<TCandidate, TSearchSpace, TProblem> replacer,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                replacer,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountReplacementCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> WithMaxInterceptorCalls(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                interceptor,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountInterceptorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> WithMaxTerminatorCalls(
            ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                terminator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountTerminatorCalls(counter));
        }
    }
}
