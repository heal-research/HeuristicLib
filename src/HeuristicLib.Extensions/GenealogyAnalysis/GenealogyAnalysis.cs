using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.GenealogyAnalysis;

public record GenealogyAnalysis<T, TS, TP, TR> :
  Analyzer<T, TS, TP, TR, GenealogyGraph<T>>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TR : PopulationState<T>
  where T : notnull
{
  private readonly IEqualityComparer<T>? equality;
  private readonly bool saveSpace;

  public GenealogyAnalysis(IAlgorithm<T, TS, TP, TR> Algorithm,
                           ICrossover<T, TS, TP>? crossover = null,
                           IMutator<T, TS, TP>? mutator = null,
                           IInterceptor<T, TS, TP, TR>? interceptor = null,
                           IEqualityComparer<T>? equality = null,
                           bool saveSpace = false) : base(Algorithm)
  {
    this.equality = equality;
    this.saveSpace = saveSpace;
    Crossover = crossover;
    Mutator = mutator;
    Interceptor = interceptor;
  }

  private ICrossover<T, TS, TP>? Crossover { get; }
  private IMutator<T, TS, TP>? Mutator { get; }
  private IInterceptor<T, TS, TP, TR>? Interceptor { get; }

  public override void RegisterObservations(IObservationRegistry observationRegistry, GenealogyGraph<T> graph)
  {
    if (Crossover is not null) {
      observationRegistry.Add(Crossover, ((offspring, parents, _, _) => AfterCross(graph, offspring, parents)));
    }

    if (Mutator is not null) {
      observationRegistry.Add(Mutator, ((offspring, parent, _, _) => AfterMutate(graph, offspring, parent)));
    }

    if (Interceptor is not null) {
      observationRegistry.Add(Interceptor, ((currentState, _, _, _, problem) => AfterInterception(graph, currentState, problem)));
    }
  }

  public void AfterCross(GenealogyGraph<T> graph, IReadOnlyList<T> offspring, IReadOnlyList<IParents<T>> parents)
  {
    foreach (var (parents1, child) in parents.Zip(offspring)) {
      graph.AddConnection([parents1.Item1, parents1.Item2], child);
    }
  }

  public void AfterMutate(GenealogyGraph<T> graph, IReadOnlyList<T> offspring, IReadOnlyList<T> parent)
  {
    foreach (var (parents1, child) in parent.Zip(offspring)) {
      graph.AddConnection([parents1], child);
    }
  }

  public void AfterInterception(GenealogyGraph<T> graph, TR currentState, TP problem)
  {
    var ordered = currentState.Population.OrderBy(keySelector: x => x.ObjectiveVector, problem.Objective.TotalOrderComparer).ToArray();
    graph.SetAsNewGeneration(ordered.Select(x => x.Genotype), saveSpace);
  }

  public override GenealogyGraph<T> CreateInitialState() => new(equality ?? EqualityComparer<T>.Default);
}
