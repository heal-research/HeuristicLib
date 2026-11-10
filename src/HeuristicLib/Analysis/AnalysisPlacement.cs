using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Attaches analyzers to a configuration by placing the wrappers they need.</summary>
/// <remarks>
/// <para>
/// One call is one <c>Substitute</c> with the same arguments, under the name of its most common use. Every source and
/// clock is matched by reference against the configuration the call received, so list everything in one call and make it
/// before other rewrites, which copy what they change. A clock shared by several traces of one call is placed once and
/// closer to the node than what reads it. Separate calls each place what they list.
/// </para>
/// <para>
/// The result is a new configuration and the receiver is unchanged, so create the run from the result. Unlike
/// <c>Substitute</c>, it is typed by the interface, because an analyzer of the receiver itself replaces it with a wrapper.
/// </para>
/// </remarks>
public static class AnalysisPlacement
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Attaches the analyzers, such as traces and counters, wherever this algorithm holds what they observe.</summary>
        /// <exception cref="InvalidOperationException">This algorithm does not hold something an analyzer observes.</exception>
        public IAlgorithm<TCandidate, TSearchState> AnalyzedBy(params IReadOnlyList<ISubstitutionBundle> analyzers) =>
            algorithm.Substitute(analyzers);
    }

    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> @operator)
        where TOperation : IOperation<TOperation>
    {
        /// <summary>Attaches the analyzers, such as traces and counters, wherever this operator holds what they observe.</summary>
        /// <exception cref="InvalidOperationException">This operator does not hold something an analyzer observes.</exception>
        public IOperator<TOperation, TCandidate> AnalyzedBy(params IReadOnlyList<ISubstitutionBundle> analyzers) =>
            @operator.Substitute(analyzers);
    }
}
