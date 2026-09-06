namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// Decides when a dynamic problem's environment is due to advance.
/// </summary>
/// <remarks>
/// A schedule measures the epoch in progress and says when it is over. What it measures is its own business:
/// evaluations performed, time elapsed, or a signal from somewhere else entirely. Everything that follows from an
/// advance — applying the update, counting versions, telling anyone — belongs to the problem, so a schedule that counts
/// and one that watches a clock are interchangeable. The problem asks from one thread at a time.
/// </remarks>
public interface IEpochSchedule
{
    /// <summary>
    /// Hands over the epochs that have ended since this was last called, and starts measuring from what is left over.
    /// </summary>
    /// <remarks>
    /// More than one can end at a time: a schedule advancing every five minutes owes two epochs to an evaluation that
    /// took twelve, and one advancing every hundred evaluations owes two to a batch of two hundred and fifty. The
    /// schedule keeps the remainder, so the epochs it hands over never drift from what it measured. The problem asks at
    /// each boundary its update policy could act on, not only when work is reported, so a schedule that ends an epoch
    /// without any work happening is noticed at the next one.
    /// </remarks>
    int TakeDueEpochs();

    /// <summary>
    /// Reports one unit of work.
    /// </summary>
    void RecordProgress();

    /// <summary>
    /// Starts measuring a new epoch, discarding whatever the current one had accumulated.
    /// </summary>
    void Restart();
}
