using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.PythonInterop;

public record ExperimentResult<T>(
    string Graph,
    IReadOnlyList<List<double>> ChildRanks,
    IReadOnlyList<BestMedianWorstEntry<T>> BestMedianWorst,
    IReadOnlyList<EvaluatedCandidate<T>[]> AllPopulations);
