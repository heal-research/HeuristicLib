using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class AllPopulationsTracker<T> : AttachedAnalysis<T, PopulationIterationResult<T>>
  where T : class {
  public List<ISolution<T>[]> AllSolutions { get; } = [];

  public override void AfterInterception(PopulationIterationResult<T> currentIterationResult, PopulationIterationResult<T>? previousIterationResult, IEncoding<T> encoding, IProblem<T, IEncoding<T>> problem) {
    AllSolutions.Add(currentIterationResult.Population.ToArray());
  }
}
