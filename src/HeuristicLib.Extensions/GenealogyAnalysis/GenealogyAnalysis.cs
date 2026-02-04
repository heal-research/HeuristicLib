using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analyzers;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.MetaOperators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.GenealogyAnalysis;

public static class GenealogyAnalysis
{
  public static GenealogyAnalysis<TG> BuildOperatorWrappers<TG, TS, TP, TR>(
    ICreator<TG, TS, TP> creator,
    ICrossover<TG, TS, TP> crossover,
    IMutator<TG, TS, TP> mutator,
    IIterationObserver<TG, TS, TP, TR> iteration,
    out ICreator<TG, TS, TP> wrappedCreator,
    out ICrossover<TG, TS, TP> wrappedCrossover,
    out IMutator<TG, TS, TP> wrappedMutator,
    out IIterationObserver<TG, TS, TP, TR> wrappedIteration,
    IEqualityComparer<TG>? equality = null, bool saveSpace = false
  )  
    where TG : class
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
    where TR : PopulationAlgorithmState<TG>
  {
    var graph = new GenealogyGraph<TG>(equality ?? EqualityComparer<TG>.Default);
    
    var analysis = new GenealogyAnalysis<TG>(equality, saveSpace);
    
    
    wrappedCreator = creator.AddObservation(analysis);
    wrappedCreator = creator.AddObservation(analysis);
    wrappedCreator = creator.AddObservation(analysis);
    wrappedCreator = creator.AddObservation(analysis);
    
    
    
    wrappedCreator = analysis.AttachTo(creator);
    wrappedCrossover = analysis.AttachTo(crossover);
    wrappedMutator = analysis.AttachTo(mutator);
    
    wrappedMutator = mutator.ObserveWith((parents, children) => {
      for (int i = 0; i < parents.Count; i++) {
        graph.AddConnection([parents[i]], children[i]);
      }
    }).AsMutator();
    
    // toDo: others
    
    wrappedIteration = analysis.AttachTo(iteration);
    return graph;
  }
  
  
  public static GenealogyAnalysis<TGenotype> Create<TGenotype, TE, TP, TRes>(IAlgorithmBuilder<TGenotype, TE, TP, TRes> prototype, IEqualityComparer<TGenotype>? equality = null, bool saveSpace = false)
    where TE : class, ISearchSpace<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TRes : PopulationAlgorithmState<TGenotype>
    where TGenotype : class
  {
    var t = new GenealogyAnalysis<TGenotype>(equality, saveSpace);
    t.AttachTo(prototype);

    return t;
  }
}

public class GenealogyAnalysis<T>(IEqualityComparer<T>? equality = null, bool saveSpace = false) : AttachedAnalysis<T, PopulationAlgorithmState<T>>
  , IMutatorObserver<>T, ICrossoverObserver<T>
  where T : class
{
  public readonly GenealogyGraph<T> Graph = new(equality ?? EqualityComparer<T>.Default);

  public override void AfterCrossover(IReadOnlyList<T> res, IReadOnlyList<IParents<T>> parents, IRandomNumberGenerator random, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
  {
    foreach (var (parents1, child) in parents.Zip(res)) {
      Graph.AddConnection([parents1.Item1, parents1.Item2], child);
    }
  }

  public override void AfterMutation(IReadOnlyList<T> res, IReadOnlyList<T> parent, IRandomNumberGenerator random, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
  {
    foreach (var (parents1, child) in parent.Zip(res)) {
      Graph.AddConnection([parents1], child);
    }
  }

  public override void AfterInterception(PopulationAlgorithmState<T> currentAlgorithmState, PopulationAlgorithmState<T>? previousIterationResult, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
  {
    var ordered = currentAlgorithmState.Population.OrderBy(keySelector: x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    Graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), saveSpace);
  }
}

public class RankAnalysis<T>(IEqualityComparer<T>? equality = null) : GenealogyAnalysis<T>(equality) where T : class
{
  public List<List<double>> Ranks { get; } = [];

  public override void AfterInterception(PopulationAlgorithmState<T> currentAlgorithmState, PopulationAlgorithmState<T>? previousIterationResult, ISearchSpace<T> searchSpace, IProblem<T, ISearchSpace<T>> problem)
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
