using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public record ExperimentResult<T>(
  string Graph,
  List<List<double>> ChildRanks,
  List<BestMedianWorstEntry<T>> BestMedianWorst,
  List<ISolution<T>[]> AllPopulations)
  where T : class;
