using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.PythonInterop;

/// <summary>A completed run's analysis values exported to Python, retaining candidate references.</summary>
/// <param name="Graph">The genealogy in GraphViz form, or an empty string when genealogy tracking is disabled.</param>
/// <param name="ChildRanks">Average descendant rank rows, oldest first; empty when genealogy tracking is disabled.</param>
/// <param name="BestMedianWorst">Population quality summaries in iteration order.</param>
/// <param name="AllPopulations">Population snapshots in iteration order; empty when population tracking is disabled.</param>
/// <param name="QualityCurve">Retained running-best points, including evaluated-candidate counts; no elapsed-time axis is added.</param>
public record ExperimentResult<T>(
    string Graph,
    ValueArray<ValueArray<double>> ChildRanks,
    ValueArray<BestMedianWorstEntry<T>> BestMedianWorst,
    ValueArray<ValueArray<EvaluatedCandidate<T>>> AllPopulations,
    ValueArray<BestSoFarPoint<T>> QualityCurve);
