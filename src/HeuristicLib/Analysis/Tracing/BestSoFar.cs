using System.Collections;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Keeps the best value seen so far, across calls, such as the best objective vector or the best evaluated candidate.
/// </summary>
/// <remarks>
/// Each value is ranked by the objective vector the selector reads from it. The running best is this object's own
/// state and is not thread-safe; create it in the trace's per-run state factory. The first objective directions it receives fix it: a later call with other
/// directions, or with objective vectors of another dimensionality, throws, because their best is not comparable to the
/// running one.
/// </remarks>
/// <param name="objectiveVector">
/// Reads the objective vector a value is ranked by, such as <c>candidate => candidate.ObjectiveVector</c>.
/// </param>
/// <param name="objectiveComparer">Orders objective vectors instead of the fixed directions' total order.</param>
public class BestSoFar<T>(Func<T, ObjectiveVector> objectiveVector, IComparer<ObjectiveVector>? objectiveComparer = null)
{
    private ObjectiveDirections? directions;
    private IComparer<ObjectiveVector>? order;
    private T? best;
    private ObjectiveVector? bestObjectiveVector;

    /// <summary>Takes the best of the values into the running best and returns the running best.</summary>
    /// <param name="values">Values to consider; an empty sequence returns the previous best if one exists.</param>
    /// <param name="objective">The objective directions, which must match those of the first call.</param>
    /// <returns>The first value attaining the best objective vector seen across all calls.</returns>
    /// <exception cref="InvalidOperationException">
    /// No value has been seen, the directions or vector dimensionality changed, or no total order is available.
    /// </exception>
    public T Update(IEnumerable<T> values, ObjectiveDirections objective)
    {
        var fixedOrder = Require(objective);
        foreach (var value in values)
        {
            var vector = objectiveVector(value);
            if (vector.Count != objective.Directions.Length)
                throw new InvalidOperationException(
                    $"A best so far is fixed to {objective.Directions.Length} objectives, but a call returned an objective vector of {vector.Count}.");
            if (bestObjectiveVector is null || fixedOrder.Compare(vector, bestObjectiveVector) < 0)
            {
                best = value;
                bestObjectiveVector = vector;
            }
        }

        return bestObjectiveVector is null
            ? throw new InvalidOperationException("There are no values, cannot determine the best so far.")
            : best!;
    }

    // The objective directions the running best was first ranked by fix the order of every later call.
    private IComparer<ObjectiveVector> Require(ObjectiveDirections objective)
    {
        if (directions is null)
        {
            order = objective.RequireTotalOrder(objectiveComparer);
            directions = objective;
        }
        else if (!ReferenceEquals(directions, objective) && !directions.Directions.SequenceEqual(objective.Directions))
            throw new InvalidOperationException(
                $"A best so far is fixed to the objective directions {directions} it first received, but a call has {objective}.");

        return order!;
    }
}

/// <summary>Keeps the best objective vector seen so far, across calls, ranking each vector by itself.</summary>
/// <remarks>The first objective directions it receives fix it, as for <see cref="BestSoFar{T}"/>.</remarks>
/// <param name="objectiveComparer">Orders objective vectors instead of the fixed directions' total order.</param>
public sealed class BestSoFar(IComparer<ObjectiveVector>? objectiveComparer = null)
    : BestSoFar<ObjectiveVector>(static vector => vector, objectiveComparer);

/// <summary>Records the running best candidate and counts the candidates evaluated at its selected evaluators.</summary>
/// <remarks>
/// Without a named evaluator, selects evaluators of this candidate type with no such evaluator among their declared
/// descendants, including evaluators inside refiners. Settings and delegates are not searched. Each run owns an
/// independent curve spanning all Cycle turns. Ranking uses each observation's problem, or the supplied comparer;
/// directions and dimensionality must stay fixed. By default only improvements are retained, keeping candidate references.
/// Clocks only read. The caller lists any clock that needs placement, or its counter's or duration's placement, after
/// every analyzer that reads it in one list, so its reading includes the call; listed before them, it reads before the call.
/// </remarks>
public sealed class BestSoFarAnalyzer<TCandidate> : Analyzer<BestSoFarCurve<TCandidate>>, IObservationReceiver<EvaluationObservation<TCandidate>>
{
    private readonly IOperator<Evaluation, TCandidate>? evaluator;
    private readonly ValueArray<IClock> clocks;
    private readonly TraceRetention retention;
    private readonly IComparer<ObjectiveVector>? objectiveComparer;

    internal BestSoFarAnalyzer(IOperator<Evaluation, TCandidate>? evaluator, IReadOnlyList<IClock>? clocks,
        TraceRetention? retention, IComparer<ObjectiveVector>? objectiveComparer)
    {
        this.evaluator = evaluator;
        this.clocks = [.. (clocks ?? []).Distinct()];
        this.retention = retention ?? TraceRetention.OnChange();
        this.objectiveComparer = objectiveComparer;
    }

    /// <summary>Counts the candidates the selected evaluators evaluated; each point reads it as <see cref="BestSoFarPoint{TCandidate}.Evaluations"/>.</summary>
    /// <remarks>
    /// Placing the analyzer with <c>CreateRun</c> or <c>Substitute</c> also places this counter. To place the analyzer
    /// explicitly, count inside the observer: <c>evaluator.Counted(best.Evaluations, OperatorCountMetric.Candidates).Observed(best)</c>.
    /// A terminator may read the same counter.
    /// </remarks>
    public Counter Evaluations { get; } = new();

    // The observer first, so the counter sits closer to the evaluator and a point includes its own call.
    protected override IReadOnlyList<ISubstitution> CreateSubstitutions()
    {
        IReadOnlyList<ISubstitution> placements = evaluator is null
            ?
            [
                Substitution.Of<IOperator<Evaluation, TCandidate>>(Selects, node => node.Observed(this)),
                Substitution.Of<IOperator<Evaluation, TCandidate>>(Selects, node => node.Counted(Evaluations, OperatorCountMetric.Candidates))
            ]
            : [evaluator.Invocations.RecordingInto(this), evaluator.Invocations.CountingCandidates(Evaluations)];
        return placements;
    }

    private static bool Selects(IOperator<Evaluation, TCandidate> evaluator)
    {
        var seen = new HashSet<IConfigurationNode>(ReferenceEqualityComparer.Instance) { evaluator };
        var pending = new Stack<IConfigurationNode>();
        AddChildren(evaluator);
        while (pending.TryPop(out var node))
        {
            if (!seen.Add(node))
                continue;
            if (node is IOperator<Evaluation, TCandidate>)
                return false;
            AddChildren(node);
        }
        return true;

        void AddChildren(IConfigurationNode node)
        {
            foreach (var parameter in node.GetParameters())
                foreach (var child in parameter.GetChildren(node))
                    pending.Push(child);
        }
    }

    /// <inheritdoc/>
    public override BestSoFarCurve<TCandidate> CreateExecutionNode(ResolutionScope scope)
    {
        var evaluations = scope.Results.Result(Evaluations);
        return new(evaluations, [.. clocks],
            [.. clocks.Select(selected => scope.Results.Result(selected))],
            retention.CreateDecider<EvaluatedCandidate<TCandidate>>(), objectiveComparer);
    }

    /// <inheritdoc/>
    public Action<EvaluationObservation<TCandidate>> CreateRecorder(RunResults results) => results.Result(this).Record;
}

/// <summary>One run's retained running bests. Enumeration reads a snapshot and is safe during collection.</summary>
/// <remarks>Collection and live reads are synchronized. Points retain candidate references, without copying candidates.</remarks>
public sealed class BestSoFarCurve<TCandidate> : IRunResult, IEnumerable<BestSoFarPoint<TCandidate>>
{
    private readonly Lock sync = new();
    private readonly List<BestSoFarPoint<TCandidate>> points = [];
    private readonly BestSoFar<EvaluatedCandidate<TCandidate>> best;
    private readonly RetentionDecider<EvaluatedCandidate<TCandidate>> retention;
    private readonly CountAccumulator evaluations;
    private readonly IClock[] columns;
    private readonly IClockReading[] readings;

    internal BestSoFarCurve(CountAccumulator evaluations, IClock[] columns, IClockReading[] readings,
        RetentionDecider<EvaluatedCandidate<TCandidate>> retention, IComparer<ObjectiveVector>? objectiveComparer)
    {
        this.evaluations = evaluations;
        this.columns = columns;
        this.readings = readings;
        this.retention = retention;
        best = new(static candidate => candidate.ObjectiveVector, objectiveComparer);
    }

    public int Count { get { lock (sync) return points.Count; } }
    /// <summary>The latest retained point, or <see langword="null"/> before any point is retained.</summary>
    public BestSoFarPoint<TCandidate>? Latest { get { lock (sync) return points.Count == 0 ? null : points[^1]; } }

    /// <summary>Enumerates a stable copy of the retained points in collection order.</summary>
    public IEnumerator<BestSoFarPoint<TCandidate>> GetEnumerator()
    {
        lock (sync)
            return ((IEnumerable<BestSoFarPoint<TCandidate>>)points.ToArray()).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Record(EvaluationObservation<TCandidate> observation)
    {
        // A call that evaluated nothing changes no best, and before the first candidate there is no best to record.
        if (observation.Candidates.Count == 0)
            return;
        lock (sync)
        {
            var candidate = best.Update(observation.Candidates.ToEvaluated(observation.ObjectiveVectors), observation.Problem.Objective);
            var decision = retention.Decide(candidate);
            if (decision == RetentionDecision.Skip)
                return;
            var point = new BestSoFarPoint<TCandidate>(evaluations.CurrentCount, candidate.ObjectiveVector, candidate.Candidate, Moment.Read(columns, readings));
            switch (decision)
            {
                case RetentionDecision.Append:
                    points.Add(point);
                    break;
                case RetentionDecision.ReplaceLatest when points.Count == 0:
                    points.Add(point);
                    break;
                case RetentionDecision.ReplaceLatest:
                    points[^1] = point;
                    break;
                default:
                    throw new InvalidOperationException($"The retention returned the unknown decision '{decision}'.");
            }
        }
    }
}

/// <summary>A retained running best and its clock readings at collection time.</summary>
public readonly record struct BestSoFarPoint<TCandidate>
{
    private readonly Moment moment;

    internal BestSoFarPoint(long evaluations, ObjectiveVector value, TCandidate candidate, Moment moment)
    {
        Evaluations = evaluations;
        Value = value;
        Candidate = candidate;
        this.moment = moment;
    }

    /// <summary>The selected evaluators' candidate count when this point was recorded, including its observed batch.</summary>
    public long Evaluations { get; }
    public ObjectiveVector Value { get; }
    public TCandidate Candidate { get; }
    /// <summary>Returns the recorded reading of an additional clock supplied to the analyzer.</summary>
    /// <param name="clock">A clock equal to one supplied when defining the analyzer.</param>
    /// <exception cref="InvalidOperationException">The analyzer does not record this clock.</exception>
    public TTime At<TTime>(IClock<TTime> clock) => moment.At(clock);
}
