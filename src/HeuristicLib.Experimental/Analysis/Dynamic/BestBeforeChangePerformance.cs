using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>The completed epochs and fitted prediction collected during one run.</summary>
/// <remarks>
/// Read the live accumulator after execution has stopped. An epoch is published only when a nonempty batch from a
/// different epoch is observed; the last observed epoch is not published when execution ends.
/// </remarks>
public sealed class BestBeforeChangePerformance<TCandidate> : IRunResult
{
    private readonly Lock sync = new();
    private readonly List<BestBeforeChangePerformanceEntry<TCandidate>> bestBeforeChange = [];
    private readonly Func<ObjectiveVector, double> objectiveValueSelector;
    private readonly OnlineWeibullCurveModel predictionModel = new(double.NaN);
    private (EvaluatedCandidate<TCandidate> Best, int Epoch)? currentEpochBest;
    private double objectiveValueSum;

    internal BestBeforeChangePerformance(Func<ObjectiveVector, double> objectiveValueSelector, int predictionEpochMultiplier)
    {
        this.objectiveValueSelector = objectiveValueSelector;
        PredictionEpochMultiplier = predictionEpochMultiplier;
    }

    private int PredictionEpochMultiplier { get; }

    /// <summary>
    /// The best candidate of each published epoch, in observation order; this is a live view.
    /// </summary>
    public IReadOnlyList<BestBeforeChangePerformanceEntry<TCandidate>> BestBeforeChange => bestBeforeChange;

    /// <summary>The mean selected objective value across published epochs, or <see cref="double.NaN"/> before any is published.</summary>
    public double Performance
    {
        get { lock (sync) return bestBeforeChange.Count == 0 ? double.NaN : objectiveValueSum / bestBeforeChange.Count; }
    }

    /// <summary>The fitted performance at the configured horizon, or <see cref="double.NaN"/> before any epoch is published.</summary>
    public double Prediction
    {
        get { lock (sync) return prediction; }
    }
    private double prediction = double.NaN;

    internal void ReadBatch(EvaluationObservation<TCandidate> observation, int epoch, IComparer<ObjectiveVector> comparer)
    {
        var batchBest = observation.Candidates
                                   .Zip(observation.ObjectiveVectors, (candidate, objectiveVector) => new EvaluatedCandidate<TCandidate>(candidate, objectiveVector))
                                   .MinBy(evaluated => evaluated.ObjectiveVector, comparer);
        if (batchBest is null)
            return;

        lock (sync)
        {
            if (currentEpochBest is not { } current)
            {
                currentEpochBest = (batchBest, epoch);
                return;
            }

            if (current.Epoch != epoch)
            {
                Record(current.Best, current.Epoch);
                currentEpochBest = (batchBest, epoch);
                return;
            }

            if (comparer.Compare(batchBest.ObjectiveVector, current.Best.ObjectiveVector) < 0)
                currentEpochBest = (batchBest, epoch);
        }
    }

    private void Record(EvaluatedCandidate<TCandidate> best, int epoch)
    {
        var objectiveValue = objectiveValueSelector(best.ObjectiveVector);
        bestBeforeChange.Add(new BestBeforeChangePerformanceEntry<TCandidate>(best.Candidate, best.ObjectiveVector, objectiveValue, epoch));
        objectiveValueSum += objectiveValue;

        predictionModel.AddObservation(epoch, objectiveValue);
        prediction = predictionModel.Predict((epoch + 1) * PredictionEpochMultiplier);
    }
}
