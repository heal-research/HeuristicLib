using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// A problem whose environment changes while the run searches it.
/// </summary>
public interface IDynamicProblem<TCandidate, out TSearchSpace> : IProblem<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    /// <summary>
    /// Decides when this problem's environment is due to advance.
    /// </summary>
    IEpochSchedule EpochSchedule { get; }

    /// <summary>
    /// The environment version currently in effect, which is the number of updates that have been applied.
    /// </summary>
    int CurrentEpoch { get; }

    /// <summary>
    /// Raised once an update has been applied, with the environment version now in effect.
    /// </summary>
    event EventHandler<int>? OnEpochChange;
}
