using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class AllPopulationsTracker<T> : AttachedAnalysis<T, PopulationAlgorithmState<T>>
  where T : class {
  public List<ISolution<T>[]> AllSolutions { get; } = [];

  public override void AfterInterception(PopulationAlgorithmState<T> currentAlgorithmState, PopulationAlgorithmState<T>? previousIterationResult, IEncoding<T> searchSpace, IProblem<T, IEncoding<T>> problem) {
    AllSolutions.Add(currentAlgorithmState.Population.ToArray());
  }
}
