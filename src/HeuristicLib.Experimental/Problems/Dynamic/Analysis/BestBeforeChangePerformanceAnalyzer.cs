using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// Records the best candidate of every completed epoch and fits a curve through those bests.
/// </summary>
/// <remarks>
/// <para>
/// This analyzer holds its own data and is used for one run. It reads the evaluator's own observation and the problem's
/// environment version, so it neither subscribes to anything nor needs cleanup.
/// </para>
/// <para>
/// It is not a plain trace: besides the per-epoch series it maintains a fitted model and derives a prediction from it.
/// It also merges several evaluator boundaries. These prediction and observation responsibilities remain specific
/// to this analyzer.
/// </para>
/// </remarks>
public sealed class BestBeforeChangePerformanceAnalyzer<TCandidate, TSearchSpace, TProblem> : IAnalyzer
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
{
    private readonly Lock sync = new();
    private readonly List<BestBeforeChangePerformanceEntry<TCandidate>> bestBeforeChange = [];
    private readonly ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators;
    private readonly Func<ObjectiveVector, double> objectiveValueSelector;
    private readonly OnlineWeibullCurveModel predictionModel = new(double.NaN);
    private readonly TProblem problem;
    private (EvaluatedCandidate<TCandidate> Best, int Epoch)? currentEpochBest;
    private double objectiveValueSum;

    public BestBeforeChangePerformanceAnalyzer(TProblem problem,
                                               IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators,
                                               Func<ObjectiveVector, double>? objectiveValueSelector = null,
                                               int predictionEpochMultiplier = 10)
    {
        if (evaluators.Count == 0)
            throw new ArgumentException("An analyzer needs at least one observation source.", nameof(evaluators));

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(predictionEpochMultiplier);
        this.problem = problem;
        this.evaluators = [.. evaluators];
        this.objectiveValueSelector = objectiveValueSelector ?? (static objectiveVector => objectiveVector[0]);
        PredictionEpochMultiplier = predictionEpochMultiplier;
    }

    public int PredictionEpochMultiplier { get; }

    /// <summary>
    /// The best candidate of every epoch that has ended. The epoch in progress appears once it does.
    /// </summary>
    public IReadOnlyList<BestBeforeChangePerformanceEntry<TCandidate>> BestBeforeChange
    {
        get { lock (sync) return [.. bestBeforeChange]; }
    }

    public double Performance
    {
        get { lock (sync) return bestBeforeChange.Count == 0 ? double.NaN : objectiveValueSum / bestBeforeChange.Count; }
    }

    public double Prediction
    {
        get { lock (sync) return prediction; }
    }
    private double prediction = double.NaN;

    public void Install(ExecutionInstanceResolverBuilder builder)
    {
        foreach (var evaluator in evaluators)
            builder.Observe(evaluator, ReadBatch);
    }

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

public readonly record struct BestBeforeChangePerformanceEntry<TCandidate>(
    TCandidate Candidate,
    ObjectiveVector ObjectiveVector,
    double ObjectiveValue,
    int Epoch);
