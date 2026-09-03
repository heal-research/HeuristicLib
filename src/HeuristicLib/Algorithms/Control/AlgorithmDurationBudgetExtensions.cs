namespace HEAL.HeuristicLib.Algorithms;

public static class AlgorithmDurationBudgetExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchState> WithMaxAlgorithmDuration(
            TimeSpan maximumDuration)
        {
            return algorithm.WithMaxAlgorithmDuration(maximumDuration, TimeProvider.System);
        }

        public AlgorithmDurationBudgetAlgorithm<TCandidate, TSearchState> WithMaxAlgorithmDuration(
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
