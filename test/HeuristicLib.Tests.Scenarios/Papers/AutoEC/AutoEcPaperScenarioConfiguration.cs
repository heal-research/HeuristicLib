using HEAL.HeuristicLib.Problems.Dynamic;

namespace HEAL.HeuristicLib.Tests.Scenarios.Papers.AutoEC;

public partial class AutoEcPaperScenarioTests
{
    private const int PrototypeRepetitions = 10;
    private const int PrototypeTargetEpochChanges = 300;
    private const int PrototypeEpochLength = 500;
    private static readonly string[] PrototypeTspFiles =
    [
        "burma14.tsp", "bayg29.tsp", "bays29.tsp", "att48.tsp", "berlin52.tsp", "kroA100.tsp", "kroB150.tsp",
        "pr299.tsp"
    ];
    private static readonly int[] PrototypeRacerVariants = [1, 2];

    private const int MatrixScaffoldRepetitions = 10;
    private const int MatrixScaffoldTargetEpochChanges = 500;
    private const int MatrixScaffoldEpochLength = 500;
    private static readonly int[] MatrixScaffoldRacerVariants = [1, 2];
    private static readonly string[] MatrixScaffoldTspFiles =
    [
        "burma14.tsp", "bayg29.tsp", "att48.tsp", "kroA100.tsp", "kroB150.tsp", "pr299.tsp"
    ];
    private static readonly string[] MatrixScaffoldQapFiles =
    [
        "chr12a.dat", "had12.dat", "nug12.dat", "chr15a.dat", "dre15.dat", "chr18a.dat", "nug20.dat",
        "tai20a.dat", "nug30.dat", "tai40a.dat", "sko56.dat"
    ];
    private static readonly MovingPeaksScaffoldConfig[] MatrixScaffoldMovingPeaksConfigs =
    [
        new("scenario2-static", MovingPeaksDefaults.Scenario2_Static()),
        new("scenario2-canonical", MovingPeaksDefaults.Scenario2()),
        new("scenario2-low-shift", MovingPeaksDefaults.Scenario2() with { ShiftSeverity = 0.25 }),
        new("scenario2-high-shift", MovingPeaksDefaults.Scenario2() with { ShiftSeverity = 2.0 }),
        new("scenario2-many-peaks", MovingPeaksDefaults.Scenario2() with { NumberOfPeaks = 20 }),
        new("scenario2-10d", MovingPeaksDefaults.Scenario2() with { Dimension = 10 })
    ];
    private static readonly AutoEcChildAlgorithmKind[] MatrixScaffoldAlgorithmVariants =
    [
        AutoEcChildAlgorithmKind.GeneticAlgorithm,
        AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm,
        AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm
    ];

    private sealed record PrototypeRunSpec(string TspFile, int InstanceIndex, int Repetition, int NoRacers);

    private sealed record MatrixScaffoldRunSpec(
        string ProblemKey,
        int ProblemIndex,
        string InstanceKey,
        int Repetition,
        int NoRacers,
        AutoEcChildAlgorithmKind AlgorithmKind,
        string? TspFile = null,
        string? QapFile = null,
        MovingPeaksParameters? MovingPeaksParameters = null);

    private sealed record MovingPeaksScaffoldConfig(string Key, MovingPeaksParameters Parameters);

    private enum AutoEcChildAlgorithmKind
    {
        GeneticAlgorithm,
        HypermutationGeneticAlgorithm,
        ChangeTriggeredRandomImmigrantGeneticAlgorithm
    }

    private static string FormatAlgorithmKind(AutoEcChildAlgorithmKind algorithmKind) =>
        algorithmKind switch
        {
            AutoEcChildAlgorithmKind.HypermutationGeneticAlgorithm => "hypermutation-ga",
            AutoEcChildAlgorithmKind.ChangeTriggeredRandomImmigrantGeneticAlgorithm => "change-triggered-riga",
            _ => "ga"
        };
}
