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
    /// <remarks>
    /// Compare this value with a previously recorded epoch to detect applied updates. Pending updates do not change it.
    /// </remarks>
    int CurrentEpoch { get; }
}
