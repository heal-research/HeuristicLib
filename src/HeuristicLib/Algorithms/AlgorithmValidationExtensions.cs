using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>
/// Pre-flight validation of an algorithm configuration against the problem it is about to run on.
/// </summary>
/// <remarks>
/// Validation is deliberately the same walk that a run performs, so the two cannot drift: it reaches every operator a
/// run would reach and asks the same compatibility questions. Validate before starting a run when a configuration was
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
        /// Reports every operator in this algorithm that cannot be used over the problem's search space, and whether
        /// the configuration can be built for this run at all.
        /// </summary>
        /// <remarks>
        /// Two questions, because they fail differently. <em>Invariants</em> are what an operator declares about the
        /// candidates it produces, checked against the search space; every violation is reported, so one pass names
        /// every operator a user has to change. <em>Binding</em> is whether each operator can produce an execution
        /// instance for this run's candidate, search space and problem, and it is answered the only way it can be —
        /// by building the graph. That is where problem compatibility is decided: an operator written for one problem
        /// is refused over another, because resolution is typed at the problem.
        /// <para>
        /// Building stops at the first operator that refuses, so the binding half reports one failure where the
        /// invariant half reports all of them. The instances it builds are discarded with their registry and never
        /// reach the run.
        /// </para>
        /// </remarks>
        public ValidationReport Validate(TProblem problem)
        {
            var diagnostics = SearchConfigurationValidation.Validate(algorithm, problem.SearchSpace).Diagnostics.ToBuilder();

            try
            {
                new ExecutionInstanceRegistry().Resolve(algorithm);
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
        public void ValidateAndThrow(TProblem problem) => algorithm.Validate(problem).ThrowIfInvalid();
    }
}
