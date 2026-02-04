using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Observers;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.GenealogyAnalysis;

public class GenealogyAnalysis<T>(IEqualityComparer<T>? equality = null, bool saveSpace = false) : ICrossoverObserver<T>, IMutatorObserver<T>, IInterceptorObserver<T, PopulationState<T>>
  where T : class
{
  public readonly GenealogyGraph<T> Graph = new(equality ?? EqualityComparer<T>.Default);

  public void AfterCrossover(IReadOnlyList<T> res, IReadOnlyList<IParents<T>> parents, IRandomNumberGenerator random, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
  {
    foreach (var (parents1, child) in parents.Zip(res)) {
      Graph.AddConnection([parents1.Item1, parents1.Item2], child);
    }
  }

  public void AfterMutation(IReadOnlyList<T> res, IReadOnlyList<T> parent, IRandomNumberGenerator random, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
  {
    foreach (var (parents1, child) in parent.Zip(res)) {
      Graph.AddConnection([parents1], child);
    }
  }

  public virtual void AfterInterception(PopulationState<T> currentAlgorithmState, PopulationState<T>? previousIterationResult, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
  {
    var ordered = currentAlgorithmState.Population.OrderBy(keySelector: x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    Graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), saveSpace);
  }

  public void BuildWrappers<TG, TS, TP, TR>(
    ICrossover<TG, TS, TP> crossover,
    IMutator<TG, TS, TP> mutator,
    IInterceptor<TG, TR, TS, TP> iteration,
    out ICrossover<TG, TS, TP> wrappedCrossover,
    out IMutator<TG, TS, TP> wrappedMutator,
    out IInterceptor<TG, TR, TS, TP> wrappedIteration)
    where TG : class, T
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TR : PopulationState<T>
  {
    wrappedIteration = this.WrapInterceptor(iteration);
    wrappedCrossover = this.WrapCrossover(crossover);
    wrappedMutator = this.WrapMutator(mutator);
  }
}

public class RankAnalysis<T>(IEqualityComparer<T>? equality = null) : GenealogyAnalysis<T>(equality) where T : class
{
  public List<List<double>> Ranks { get; } = [];

  public override void AfterInterception(PopulationState<T> currentAlgorithmState, PopulationState<T>? previousIterationResult, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
  {
    base.AfterInterception(currentAlgorithmState, previousIterationResult, searchSpace, problem);
    RecordRanks(Graph, Ranks);
  }

  private static void RecordRanks<TGenotype>(GenealogyGraph<TGenotype> graph, List<List<double>> ranks) where TGenotype : notnull
  {
    if (graph.Nodes.Count < 2) {
      return;
    }

    var line = graph.Nodes[^2].Values
                    .Where(x => x.Layer == 0)
                    .OrderBy(x => x.Rank)
                    .Select(node => node.GetAllDescendants().Where(x => x.Rank >= 0).AverageOrNaN(x => x.Rank))
                    .ToList();
    if (line.Count > 0) {
      ranks.Add(line);
    }
  }
}
