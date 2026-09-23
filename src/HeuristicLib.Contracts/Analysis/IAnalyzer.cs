using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Owns mutable analysis results and installs the observations used to produce them.
/// </summary>
/// <remarks>
/// An analyzer may install zero, one or several execution modules. A run installs the analyzer itself as it would a
/// module, so a decoration the analyzer declares directly binds as a module's would. Reusing an analyzer across runs
/// intentionally combines its results. An analyzer that observes concurrent runs is responsible for synchronizing its
/// mutable state.
/// </remarks>
public interface IAnalyzer
{
    /// <summary>Installs this analyzer's observation machinery into a run before its execution graph is resolved.</summary>
    void Install(ResolutionScopeBuilder builder);
}
