using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public static class AlgorithmDurationBudgetExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> WithMaxAlgorithmDuration(
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxAlgorithmDuration(maximumDuration, TimeProvider.System);
        }

        public AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> WithMaxAlgorithmDuration(
            TimeSpan maximumDuration,
            TimeProvider timeProvider)
        {
            return new()
            {
                Algorithm = algorithm,
                MaximumDuration = maximumDuration,
                TimeProvider = timeProvider
            };
        }
    }
}
