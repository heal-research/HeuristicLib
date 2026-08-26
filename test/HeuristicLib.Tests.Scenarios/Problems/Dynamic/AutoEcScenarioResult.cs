using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems.Dynamic;

using MetaOptimizationGenotype = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.Dynamic;

public sealed record AutoEcScenarioResult(
    string SchemaVersion,
    AutoEcScenarioInput Input,
    AutoEcScenarioOutput Output);

public sealed record AutoEcScenarioBatchManifest(
    string SchemaVersion,
    string BatchName,
    DateTimeOffset CreatedAt,
    int RunCount,
    IReadOnlyList<AutoEcScenarioBatchRun> Runs);

public sealed record AutoEcScenarioBatchRun(
    string ScenarioName,
    string ProblemInstance,
    int Repetition,
    int ProblemSeed,
    int RunSeed,
    string File);

public sealed record AutoEcScenarioInput(
    string ScenarioName,
    AutoEcProblemDescription Problem,
    AutoEcSeedPlan Seeds,
    AutoEcRacerParameters Racer,
    AutoEcBaseAlgorithmDescription BaseAlgorithm,
    IReadOnlyList<AutoEcParameterRange> HyperParameterSearchSpace);

public sealed record AutoEcProblemDescription(
    string Kind,
    string? Instance,
    IReadOnlyDictionary<string, string> Parameters);

public sealed record AutoEcSeedPlan(
    int ProblemSeed,
    int RunSeed);

public sealed record AutoEcRacerParameters(
    int NoRacers,
    int BurnInEpochs,
    double EarlyTerminationStrength,
    double HallOfFameStrength,
    int MinimumModelObservationCount,
    int ModelObservationInterval,
    double MetaMutationStrength,
    int TargetEpochChanges);

public sealed record AutoEcBaseAlgorithmDescription(
    string Kind,
    IReadOnlyDictionary<string, string> StaticParameters);

public sealed record AutoEcParameterRange(
    string Name,
    string Kind,
    double Minimum,
    double Maximum);

public sealed record AutoEcScenarioOutput(
    int ObservedEpoch,
    long ProblemEvaluationCount,
    int FinalPopulationSize,
    double BestBeforeChangePerformance,
    IReadOnlyList<AutoEcQualityCurvePoint> QualityCurve,
    IReadOnlyList<AutoEcBestBeforeChangePoint> BestBeforeChange,
    IReadOnlyList<AutoEcRacingQualityTracePoint> RacingQualityTrace,
    IReadOnlyList<AutoEcRacingRaceSummary> RacingRaces);

public sealed record AutoEcQualityCurvePoint(
    AutoEcEvaluationTiming Timing,
    IReadOnlyList<double> ObjectiveVector,
    string Candidate);

public sealed record AutoEcBestBeforeChangePoint(
    AutoEcEvaluationTiming Timing,
    IReadOnlyList<double> ObjectiveVector,
    double ObjectiveValue,
    string Candidate);

public sealed record AutoEcRacingQualityTracePoint(
    long Race,
    int Epoch,
    int ContenderIndex,
    bool IsIncumbent,
    string Origin,
    AutoEcMetaConfiguration Configuration,
    IReadOnlyList<double> ObjectiveVector,
    double ObjectiveValue,
    long ProblemEvaluationCount,
    int ContenderEvaluationCount,
    string Candidate);

public sealed record AutoEcRacingRaceSummary(
    long Race,
    int Epoch,
    string TerminationReason,
    int WinnerContenderIndex,
    AutoEcMetaConfiguration WinnerConfiguration,
    long ProblemEvaluationCount);

public sealed record AutoEcEvaluationTiming(
    long EpochCount,
    int Epoch,
    bool Valid);

public sealed record AutoEcMetaConfiguration(
    IReadOnlyList<double> RealValues,
    IReadOnlyList<int> IntegerValues)
{
    public string Display => $"[{string.Join(",", RealValues)}]|[{string.Join(",", IntegerValues)}]";
}

public static class AutoEcScenarioResultExport
{
    public static IReadOnlyList<double> Export(ObjectiveVector objectiveVector) => objectiveVector.ToArray();


    public static AutoEcMetaConfiguration Export(MetaOptimizationGenotype configuration) =>
        new(configuration.Part1.ToArray(), configuration.Part2.ToArray());
}
