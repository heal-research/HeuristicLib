using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public static class AlgorithmDurationBudgetExtensions
{
    extension<TG, TS, TP, TSearchState>(IAlgorithm<TG, TS, TP, TSearchState> algorithm)
        where TS : class, ISearchSpace<TG>
        where TP : class, IProblem<TG, TS>
        where TSearchState : class, ISearchState
    {
        public AlgorithmDurationBudgetAlgorithm<TG, TS, TP, TSearchState> WithMaxAlgorithmDuration(
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxAlgorithmDuration(maximumDuration, TimeProvider.System);
        }

        public AlgorithmDurationBudgetAlgorithm<TG, TS, TP, TSearchState> WithMaxAlgorithmDuration(
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return new AlgorithmDurationBudgetAlgorithm<TG, TS, TP, TSearchState>
            {
                Algorithm = algorithm,
                MaximumDuration = maximumDuration,
                TimeProvider = timeProvider
            };
        }
    }
}
