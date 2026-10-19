using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Records the best candidate of every completed epoch and fits a curve through those bests.
/// </summary>
/// <remarks>
/// <para>
/// This analyzer holds its own data. It reads the evaluator's own observation and the problem's environment version, so
/// it neither subscribes to anything nor needs cleanup.
/// </para>
/// <para>
/// It is not a trace, because what it records per epoch is the best of an epoch that has ended, which is only known
/// once the next one begins. It is an accumulator with a hand-written fold: besides the per-epoch rows it maintains a
/// fitted model and derives a prediction from it, and it merges several evaluator boundaries. Those responsibilities
/// stay specific to this analyzer; the lock and the publication rule come from the base.
/// </para>
/// </remarks>
public sealed class BestBeforeChangePerformanceAnalyzer<TCandidate, TSearchSpace, TProblem> : AccumulatingAnalyzer
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IDynamicProblem<TCandidate, TSearchSpace>
{
    private readonly List<BestBeforeChangePerformanceEntry<TCandidate>> bestBeforeChange = [];
    private readonly Func<ObjectiveVector, double> objectiveValueSelector;
    private OnlineWeibullCurveModel predictionModel = new(double.NaN);
    private readonly TProblem problem;
    private (EvaluatedCandidate<TCandidate> Best, int Epoch)? currentEpochBest;
    private double objectiveValueSum;

    public BestBeforeChangePerformanceAnalyzer(TProblem problem,
                                               IReadOnlyList<IEvaluator<TCandidate>> evaluators,
                                               Func<ObjectiveVector, double>? objectiveValueSelector = null,
                                               int predictionEpochMultiplier = 10,
                                               bool includeCurrentEpoch = false)
    {
        if (evaluators.Count == 0)
            throw new ArgumentException("An analyzer needs at least one observation source.", nameof(evaluators));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(predictionEpochMultiplier);
        this.problem = problem;
        IncludeCurrentEpoch = includeCurrentEpoch;
        this.objectiveValueSelector = objectiveValueSelector ?? (static objectiveVector => objectiveVector[0]);
        PredictionEpochMultiplier = predictionEpochMultiplier;
        Advices = [.. evaluators.SelectMany(evaluator => Aspect.Observe<TCandidate, TSearchSpace, TProblem>(evaluator, ReadBatch).Advices)];
    }

    public int PredictionEpochMultiplier { get; }

    /// <summary>Gets whether the best observation of the current incomplete epoch is included.</summary>
    public bool IncludeCurrentEpoch { get; }

    /// <summary>
    /// The best candidate of every epoch that has ended. The epoch in progress appears once it does.
    /// </summary>
    public IReadOnlyList<BestBeforeChangePerformanceEntry<TCandidate>> BestBeforeChange
    {
        get { lock (Sync) { PublishCompletedEpoch(); return bestBeforeChange; } }
    }

    public double Performance
    {
        get { lock (Sync) { PublishCompletedEpoch(); return bestBeforeChange.Count == 0 ? double.NaN : objectiveValueSum / bestBeforeChange.Count; } }
    }

    public double Prediction
    {
        get { lock (Sync) { PublishCompletedEpoch(); return prediction; } }
    }
    private double prediction = double.NaN;

    private void ReadBatch(EvaluatorObservation<TCandidate, TSearchSpace, TProblem> observation)
    {
        // The environment only changes when the next batch begins, so it still names the one that scored this batch.
        var epoch = problem.CurrentEpoch;
        var comparer = problem.Objective.TotalOrderComparer;
        var batchBest = observation.Candidates
                                   .Zip(observation.ObjectiveVectors, (candidate, objectiveVector) => new EvaluatedCandidate<TCandidate>(candidate, objectiveVector))
                                   .MinBy(evaluated => evaluated.ObjectiveVector, comparer);
        if (batchBest is null)
            return;

        lock (Sync)
        {
            PublishCompletedEpoch();
            if (currentEpochBest is not { } current || current.Epoch != epoch)
            {
                currentEpochBest = (batchBest, epoch);
                if (IncludeCurrentEpoch)
                    Record(batchBest, epoch);
                return;
            }
            if (comparer.Compare(batchBest.ObjectiveVector, current.Best.ObjectiveVector) >= 0)
                return;
            currentEpochBest = (batchBest, epoch);
            if (IncludeCurrentEpoch)
            {
                var value = objectiveValueSelector(batchBest.ObjectiveVector);
                objectiveValueSum += value - bestBeforeChange[^1].ObjectiveValue;
                bestBeforeChange[^1] = new(batchBest.Candidate, batchBest.ObjectiveVector, value, epoch);
                predictionModel = new(double.NaN);
                foreach (var entry in bestBeforeChange)
                    predictionModel.AddObservation(entry.Epoch, entry.ObjectiveValue);
                prediction = predictionModel.Predict((epoch + 1) * PredictionEpochMultiplier);
            }
        }
    }

    private void PublishCompletedEpoch()
    {
        if (!IncludeCurrentEpoch && currentEpochBest is { } current && current.Epoch < problem.CurrentEpoch)
        {
            Record(current.Best, current.Epoch);
            currentEpochBest = null;
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

public readonly record struct BestBeforeChangePerformanceEntry<TCandidate>(
    TCandidate Candidate,
    ObjectiveVector ObjectiveVector,
    double ObjectiveValue,
    int Epoch);
