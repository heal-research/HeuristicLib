using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Analyzers;

public record BestMedianWorstEntry<T>(ISolution<T> Best, ISolution<T> Median, ISolution<T> Worst);

public class BestMedianWorstAnalysis<TGenotype> : AttachedAnalysis<TGenotype, PopulationAlgorithmState<TGenotype>> where TGenotype : class
{
  public readonly List<BestMedianWorstEntry<TGenotype>> BestISolutions = [];

  public override void AfterInterception(PopulationAlgorithmState<TGenotype> currentAlgorithmState, PopulationAlgorithmState<TGenotype>? previousIterationResult, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    var comp = problem.Objective.TotalOrderComparer is NoTotalOrderComparer ? new LexicographicComparer(problem.Objective.Directions) : problem.Objective.TotalOrderComparer;
    var ordered = currentAlgorithmState.Population.OrderBy(keySelector: x => x.ObjectiveVector, comp).ToArray();
    if (ordered.Length == 0) {
      BestISolutions.Add(null!);

      return;
    }

    BestISolutions.Add(new BestMedianWorstEntry<TGenotype>(ordered[0], ordered[ordered.Length / 2], ordered[^1]));
  }
}

public static class BestMedianWorstAnalysis
{
  public static BestMedianWorstAnalysis<TGenotype> Analyze<TGenotype, TE, TP, TR, TAlg, TBuildSpec>(
    IAlgorithmBuilder<TGenotype, TE, TP, TR, TAlg, TBuildSpec> ga)
    where TE : class, ISearchSpace<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TR : PopulationAlgorithmState<TGenotype>
    where TGenotype : class
    where TAlg : class, IAlgorithm<TGenotype, TE, TP, TR>
    where TBuildSpec : IBuildSpec
  {
    var r = new BestMedianWorstAnalysis<TGenotype>();
    ga.AddAttachment(r);

    return r;
  }
}
