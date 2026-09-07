using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// The work one environment received.
/// </summary>
/// <param name="Epoch">The environment version the evaluations were scored against.</param>
/// <param name="Evaluations">How many candidates were scored against it.</param>
/// <param name="Stale">
/// How many of those ran after the schedule had already called for the next environment, which is the update policy's
/// lag expressed in wasted evaluations.
/// </param>
public readonly record struct EpochWork(int Epoch, long Evaluations, long Stale);

public static class EpochWorkTrace
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a trace of the work an evaluator does, recorded against both the evaluation count and the
        /// environment version.
        /// </summary>
        /// <remarks>
        /// Read it with <see cref="EpochWorkTrace.PerEpoch"/>. The two clocks are what carry the information: the
        /// evaluation count says how much work happened, and the environment version says what scored it.
        /// </remarks>
        public static TraceAnalyzer<int> TraceEpochWork<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate, TSearchSpace, TProblem> at,
            Clock<long> evaluations,
            EpochClock<TCandidate, TSearchSpace> epoch)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : DynamicProblem<TCandidate, TSearchSpace> =>
            Analyzer.Trace(
                observation => new[] { observation.ObjectiveVectors.Count },
                Aggregate.Single<int>(),
                Anchor.At(at),
                evaluations,
                epoch);
    }

    /// <summary>
    /// Reads a trace created by <c>Analyzer.TraceEpochWork</c> as the work each environment received.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An evaluation is stale when the schedule had already called for the next environment but the update policy had
    /// not applied it yet. Both halves are on the trace already: the evaluation count says which nominal epoch an
    /// evaluation belongs to, and the environment version says which one actually scored it. Where the first runs ahead
    /// of the second, the evaluations in between are stale.
    /// </para>
    /// <para>
    /// This assumes the schedule paces epochs by evaluations and that every evaluated candidate reached the problem, so
    /// it does not describe a run whose evaluator answers from a cache. The environment in progress when the run ends
    /// is reported like any other, but nothing has closed it, so its counts are only what happened so far.
    /// </para>
    /// </remarks>
    /// <param name="evaluationsPerEpoch">
    /// The evaluations one epoch is paced by, from <see cref="EvaluationCountSchedule.EvaluationsPerEpoch"/>.
    /// </param>
    public static IReadOnlyList<EpochWork> PerEpoch<TCandidate, TSearchSpace>(
        TraceAnalyzer<int> trace,
        Clock<long> evaluations,
        EpochClock<TCandidate, TSearchSpace> epoch,
        int evaluationsPerEpoch)
        where TSearchSpace : class, ISearchSpace<TCandidate>
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(evaluationsPerEpoch);

        var work = new Dictionary<int, (long Evaluations, long Stale)>();
        var previous = 0L;
        foreach (var entry in trace.Snapshot())
        {
            var reached = entry.At(evaluations);
            var version = entry.At(epoch);
            var performed = reached - previous;

            // Of the evaluations this firing performed, the stale ones are those the schedule had already counted into
            // a later epoch than the one in effect.
            var stale = 0L;
            for (var evaluation = previous + 1; evaluation <= reached; evaluation++)
            {
                if ((evaluation - 1) / evaluationsPerEpoch > version)
                    stale++;
            }

            var current = work.GetValueOrDefault(version);
            work[version] = (current.Evaluations + performed, current.Stale + stale);
            previous = reached;
        }

        return [.. work.OrderBy(entry => entry.Key).Select(entry => new EpochWork(entry.Key, entry.Value.Evaluations, entry.Value.Stale))];
    }
}
