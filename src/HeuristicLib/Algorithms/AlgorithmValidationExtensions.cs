using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// The same walk a run performs, so the two cannot drift. Worth calling when a configuration was assembled
/// dynamically, or when a meta-algorithm would otherwise reach a broken stage part way through.
/// </remarks>
public static class AlgorithmValidationExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <remarks>
        /// Reports two kinds of problem, and every occurrence of both, so one pass names every operator to change.
        /// An operator may declare an invariant the search space contradicts, or may not have been written for this
        /// run's search space, problem and search state at all.
        /// <para>
        /// Both are decided from what the configuration graph declares. Nothing is constructed and no registry is
        /// touched, so validating costs nothing that the run then repeats, and the answer covers children a run would
        /// only resolve once it is under way.
        /// </para>
        /// </remarks>
        public ValidationReport Validate<TProblem, TSearchSpace>(Problem<TProblem, TCandidate, TSearchSpace> problem)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            SearchConfigurationValidation.Validate(
                algorithm,
                problem.SearchSpace,
                ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>());

        /// <summary>
        /// Throws when this algorithm cannot be used over the problem, listing every reason the walk found rather
        /// than only the first.
        /// </summary>
        public void ValidateAndThrow<TProblem, TSearchSpace>(Problem<TProblem, TCandidate, TSearchSpace> problem)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.Validate(problem).ThrowIfInvalid();
    }
}
