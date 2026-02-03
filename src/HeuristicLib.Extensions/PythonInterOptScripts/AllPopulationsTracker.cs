using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class AllPopulationsTracker<T> : AttachedAnalysis<T, PopulationAlgorithmState<T>>
  where T : class
{
  public List<ISolution<T>[]> AllSolutions { get; } = [];

  public override void AfterInterception(PopulationAlgorithmState<T> currentAlgorithmState, PopulationAlgorithmState<T>? previousIterationResult, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem) => AllSolutions.Add(currentAlgorithmState.Population.ToArray());
}
