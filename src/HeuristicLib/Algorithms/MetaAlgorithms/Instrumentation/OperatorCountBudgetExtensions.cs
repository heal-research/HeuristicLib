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
    extension<TG, TS, TP, TSearchState, TObservedInstance>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
        where TObservedInstance : class, IOperatorInstance
    {
        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance> WithMaxCount<TOperator>(
            TOperator observedOperator,
            int maximumCount,
            Func<TOperator, ObservationCounter, IOperator<TObservedInstance>> countedOperatorFactory)
            where TOperator : IOperator<TObservedInstance>
        {
            return new OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, TOperator, TObservedInstance>
            {
                Algorithm = algorithm,
                ObservedOperator = observedOperator,
                MaximumCount = maximumCount,
                CountedOperatorFactory = countedOperatorFactory
            };
        }
    }

    extension<TG, TS, TP, TSearchState>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
    {
        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatorCalls(int maximumCalls)
        {
            return algorithm.WithMaxCount(
                algorithm.Evaluator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountEvaluatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>> WithMaxEvaluatedGenotypes(int maximumGenotypes)
        {
            return algorithm.WithMaxCount(
                algorithm.Evaluator,
                maximumGenotypes,
                static (observedOperator, counter) => observedOperator.CountEvaluatedGenotypes(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, ICreator<TG, TS, TP>, ICreatorInstance<TG, TS, TP>> WithMaxCreatorCalls(
            ICreator<TG, TS, TP> creator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                creator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCreatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, ICreator<TG, TS, TP>, ICreatorInstance<TG, TS, TP>> WithMaxCreatedGenotypes(
            ICreator<TG, TS, TP> creator,
            int maximumGenotypes)
        {
            return algorithm.WithMaxCount(
                creator,
                maximumGenotypes,
                static (observedOperator, counter) => observedOperator.CountCreatedGenotypes(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, ICrossover<TG, TS, TP>, ICrossoverInstance<TG, TS, TP>> WithMaxCrossoverCalls(
            ICrossover<TG, TS, TP> crossover,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                crossover,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountCrossoverCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, ICrossover<TG, TS, TP>, ICrossoverInstance<TG, TS, TP>> WithMaxCrossedGenotypes(
            ICrossover<TG, TS, TP> crossover,
            int maximumGenotypes)
        {
            return algorithm.WithMaxCount(
                crossover,
                maximumGenotypes,
                static (observedOperator, counter) => observedOperator.CountCrossedGenotypes(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IMutator<TG, TS, TP>, IMutatorInstance<TG, TS, TP>> WithMaxMutatorCalls(
            IMutator<TG, TS, TP> mutator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountMutatorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IMutator<TG, TS, TP>, IMutatorInstance<TG, TS, TP>> WithMaxMutatedGenotypes(
            IMutator<TG, TS, TP> mutator,
            int maximumGenotypes)
        {
            return algorithm.WithMaxCount(
                mutator,
                maximumGenotypes,
                static (observedOperator, counter) => observedOperator.CountMutatedGenotypes(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, ISelector<TG, TS, TP>, ISelectorInstance<TG, TS, TP>> WithMaxSelectorCalls(
            ISelector<TG, TS, TP> selector,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                selector,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountSelectorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, ISelector<TG, TS, TP>, ISelectorInstance<TG, TS, TP>> WithMaxSelectedSolutions(
            ISelector<TG, TS, TP> selector,
            int maximumSolutions)
        {
            return algorithm.WithMaxCount(
                selector,
                maximumSolutions,
                static (observedOperator, counter) => observedOperator.CountSelectedSolutions(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IReplacer<TG, TS, TP>, IReplacerInstance<TG, TS, TP>> WithMaxReplacerCalls(
            IReplacer<TG, TS, TP> replacer,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                replacer,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountReplacerCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IReplacer<TG, TS, TP>, IReplacerInstance<TG, TS, TP>> WithMaxReplacementSolutions(
            IReplacer<TG, TS, TP> replacer,
            int maximumSolutions)
        {
            return algorithm.WithMaxCount(
                replacer,
                maximumSolutions,
                static (observedOperator, counter) => observedOperator.CountReplacementSolutions(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, IInterceptor<TG, TS, TP, TSearchState>, IInterceptorInstance<TG, TS, TP, TSearchState>> WithMaxInterceptorCalls(
            IInterceptor<TG, TS, TP, TSearchState> interceptor,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                interceptor,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountInterceptorCalls(counter));
        }

        public OperatorBudgetAlgorithm<TG, TS, TP, TSearchState, ITerminator<TG, TS, TP, TSearchState>, ITerminatorInstance<TG, TS, TP, TSearchState>> WithMaxTerminatorCalls(
            ITerminator<TG, TS, TP, TSearchState> terminator,
            int maximumCalls)
        {
            return algorithm.WithMaxCount(
                terminator,
                maximumCalls,
                static (observedOperator, counter) => observedOperator.CountTerminatorCalls(counter));
        }
    }
}
