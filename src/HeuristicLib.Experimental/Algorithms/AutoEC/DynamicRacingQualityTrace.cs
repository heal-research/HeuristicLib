using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

using MetaOptimizationGenotype = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;

namespace HEAL.HeuristicLib.Algorithms.AutoEC;

public interface IDynamicRacingQualityTraceSink<TCandidate>
{
    void Add(DynamicRacingQualityTracePoint<TCandidate> point);
    void AddRace(DynamicRacingRaceSummary race);
}

public sealed class DynamicRacingQualityTrace<TCandidate> : IDynamicRacingQualityTraceSink<TCandidate>
{
    private readonly List<DynamicRacingQualityTracePoint<TCandidate>> points = [];

    public IReadOnlyList<DynamicRacingQualityTracePoint<TCandidate>> Points => points;
    private readonly List<DynamicRacingRaceSummary> races = [];

    public IReadOnlyList<DynamicRacingRaceSummary> Races => races;

    public void Add(DynamicRacingQualityTracePoint<TCandidate> point) => points.Add(point);

    public void AddRace(DynamicRacingRaceSummary race) => races.Add(race);
}

public enum DynamicRacingConfigurationOrigin
{
    Incumbent,
    Mutation,
    HallOfFame
}

public enum DynamicRacingRaceTerminationReason
{
    EpochChange,
    EarlyTermination
}

public readonly record struct DynamicRacingQualityTracePoint<TCandidate>(
    long Race,
    int Epoch,
    int ContenderIndex,
    bool IsIncumbent,
    DynamicRacingConfigurationOrigin Origin,
    MetaOptimizationGenotype Configuration,
    TCandidate Candidate,
    ObjectiveVector ObjectiveVector,
    double ObjectiveValue,
    long ProblemEvaluationCount,
    int ContenderEvaluationCount);

public readonly record struct DynamicRacingRaceSummary(
    long Race,
    int Epoch,
    DynamicRacingRaceTerminationReason TerminationReason,
    int WinnerContenderIndex,
    MetaOptimizationGenotype WinnerConfiguration,
    long ProblemEvaluationCount);
