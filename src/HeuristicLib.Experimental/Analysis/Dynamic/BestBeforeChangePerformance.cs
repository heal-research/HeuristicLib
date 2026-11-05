using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>The completed epochs and fitted prediction collected during one run.</summary>
/// <remarks>Read the live accumulator after execution has stopped. The epoch in progress is not published.</remarks>
public sealed class BestBeforeChangePerformance<TCandidate>
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
    /// The best candidate of every epoch that has ended. The epoch in progress appears once it does.
    /// </summary>
    public IReadOnlyList<BestBeforeChangePerformanceEntry<TCandidate>> BestBeforeChange => bestBeforeChange;

    public double Performance
    {
        get { lock (sync) return bestBeforeChange.Count == 0 ? double.NaN : objectiveValueSum / bestBeforeChange.Count; }
    }

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
