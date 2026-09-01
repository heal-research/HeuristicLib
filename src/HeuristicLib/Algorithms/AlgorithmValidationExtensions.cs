using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// Pre-flight validation of an algorithm configuration against the problem it is about to run on.
/// </summary>
/// <remarks>
/// Validation is deliberately the same walk that a run performs, so the two cannot drift: it reaches every operator a
/// run would reach and asks the same compatibility question. Validate before starting a run when a configuration was
/// assembled dynamically, or when a meta-algorithm would otherwise reach a broken stage only part way through.
/// </remarks>
public static class AlgorithmValidationExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        /// <summary>
        /// Reports every operator in this algorithm that cannot be used over the problem's search space.
        /// </summary>
        public ValidationReport Validate(TProblem problem) =>
            SearchConfigurationValidation.Validate(algorithm, problem.SearchSpace);

        /// <summary>
        /// Throws when any operator in this algorithm cannot be used over the problem's search space, listing every
        /// reason rather than only the first.
        /// </summary>
        public void ValidateAndThrow(TProblem problem) => algorithm.Validate(problem).ThrowIfInvalid();
    }
}
