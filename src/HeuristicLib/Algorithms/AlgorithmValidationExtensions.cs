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
        /// Reports two kinds of problem, which fail differently. Invariant violations are all reported, so one pass
        /// names every operator to change. Binding is answered by building the graph, so it stops at the first
        /// operator that refuses.
        /// <para>
        /// The instances it builds are discarded with their registry and never reach the run.
        /// </para>
        /// </remarks>
        public ValidationReport Validate<TProblem, TSearchSpace>(Problem<TProblem, TCandidate, TSearchSpace> problem)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate>
        {
            var diagnostics = SearchConfigurationValidation.Validate(algorithm, problem.SearchSpace).Diagnostics.ToBuilder();

            try
            {
                new ExecutionInstanceRegistry().Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);
            }
            catch (InvalidOperationException exception)
            {
                diagnostics.Add(new ValidationDiagnostic(algorithm.GetType().Name, exception.Message));
            }

            return new ValidationReport(diagnostics.ToImmutable());
        }

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
