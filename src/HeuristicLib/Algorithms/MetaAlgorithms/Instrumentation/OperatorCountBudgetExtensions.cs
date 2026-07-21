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

public static class OperatorCountBudgetExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState, TObservedInstance>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TObservedInstance : class, IOperatorInstance
    {
        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator, TObservedInstance> WithMaxCount<TOperator>(
            TOperator observedOperator,
            int maximumCount,
            Func<TOperator, ObservationCounter, IOperator<TObservedInstance>> countedOperatorFactory)
            where TOperator : IOperator<TObservedInstance>
        {
            return new OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator, TObservedInstance>
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
        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IEvaluator<TCandidate, TSearchSpace, TProblem>, IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> WithMaxEvaluatorCalls(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                evaluator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountEvaluatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IEvaluator<TCandidate, TSearchSpace, TProblem>, IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>> WithMaxEvaluatedCandidates(
            IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                evaluator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountEvaluatedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICreator<TCandidate, TSearchSpace, TProblem>, ICreatorInstance<TCandidate, TSearchSpace, TProblem>> WithMaxCreatorCalls(
            ICreator<TCandidate, TSearchSpace, TProblem> creator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                creator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCreatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICreator<TCandidate, TSearchSpace, TProblem>, ICreatorInstance<TCandidate, TSearchSpace, TProblem>> WithMaxCreatedCandidates(
            ICreator<TCandidate, TSearchSpace, TProblem> creator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                creator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCreatedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICrossover<TCandidate, TSearchSpace, TProblem>, ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> WithMaxCrossoverCalls(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                crossover,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCrossoverCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ICrossover<TCandidate, TSearchSpace, TProblem>, ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> WithMaxCrossedCandidates(
            ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                crossover,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountCrossedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IMutator<TCandidate, TSearchSpace, TProblem>, IMutatorInstance<TCandidate, TSearchSpace, TProblem>> WithMaxMutatorCalls(
            IMutator<TCandidate, TSearchSpace, TProblem> mutator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountMutatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IMutator<TCandidate, TSearchSpace, TProblem>, IMutatorInstance<TCandidate, TSearchSpace, TProblem>> WithMaxMutatedCandidates(
            IMutator<TCandidate, TSearchSpace, TProblem> mutator,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountMutatedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ISelector<TCandidate, TSearchSpace, TProblem>, ISelectorInstance<TCandidate, TSearchSpace, TProblem>> WithMaxSelectorCalls(
            ISelector<TCandidate, TSearchSpace, TProblem> selector,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                selector,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountSelectorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ISelector<TCandidate, TSearchSpace, TProblem>, ISelectorInstance<TCandidate, TSearchSpace, TProblem>> WithMaxSelectedCandidates(
            ISelector<TCandidate, TSearchSpace, TProblem> selector,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                selector,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountSelectedCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IReplacer<TCandidate, TSearchSpace, TProblem>, IReplacerInstance<TCandidate, TSearchSpace, TProblem>> WithMaxReplacerCalls(
            IReplacer<TCandidate, TSearchSpace, TProblem> replacer,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                replacer,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountReplacerCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IReplacer<TCandidate, TSearchSpace, TProblem>, IReplacerInstance<TCandidate, TSearchSpace, TProblem>> WithMaxReplacementCandidates(
            IReplacer<TCandidate, TSearchSpace, TProblem> replacer,
            int maximumCandidates)
        {
            return algorithm.WithMaxCount(
                replacer,
                maximumCandidates,
                static (observedOperator, counter) => observedOperator.CountReplacementCandidates(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> WithMaxInterceptorCalls(
            IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                interceptor,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountInterceptorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>, ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> WithMaxTerminatorCalls(
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
