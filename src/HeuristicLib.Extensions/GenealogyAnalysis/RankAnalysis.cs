using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.GenealogyAnalysis;

public record RankAnalysis<T, TS, TP, TR> : Analyzer<T, TS, TP, TR, RankState<T>>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TR : PopulationState<T>
  where T : notnull
{
  private readonly GenealogyAnalysis<T, TS, TP, TR> graphBuilder;
  private readonly IInterceptor<T, TS, TP, TR>? interceptor;

  public RankAnalysis(IAlgorithm<T, TS, TP, TR> Algorithm,
                      ICrossover<T, TS, TP>? crossover = null,
                      IMutator<T, TS, TP>? mutator = null,
                      IInterceptor<T, TS, TP, TR>? interceptor = null,
                      IEqualityComparer<T>? equality = null) : base(Algorithm)
  {
    graphBuilder = new GenealogyAnalysis<T, TS, TP, TR>(Algorithm, crossover, mutator, interceptor, equality);
    this.interceptor = interceptor;
  }

  public override RankState<T> CreateInitialState() => new(graphBuilder.CreateInitialState());

  public override void RegisterObservations(ObservationPlan observations, RankState<T> state)
  {
    graphBuilder.RegisterObservations(observations, state.Graph); //tells sub-analyzer to record its findings into outer state
    if (interceptor is not null)
      observations.Observe(interceptor, (_, _, _, _, _) => RecordRanks(state));
  }

  private static void RecordRanks(RankState<T> state)
  {
    if (state.Graph.Nodes.Count < 2) return;

    var line = state.Graph.Nodes[^2].Values
                    .Where(x => x.Layer == 0)
                    .OrderBy(x => x.Rank)
                    .Select(node => node.GetAllDescendants().Where(x => x.Rank >= 0).Select(x => (double)x.Rank).DefaultIfEmpty(double.NaN).Average())
                    .ToList();
    if (line.Count > 0) {
      state.Ranks.Add(line);
    }
  }
}

public class RankState<T> where T : notnull
{
  public RankState(GenealogyGraph<T> graph)
  {
    Graph = graph;
  }

  public List<List<double>> Ranks { get; } = [];

  public GenealogyGraph<T> Graph { get; }

  public RankAnalysisResult<T> Result => new(Graph, Ranks.Select(x => (IReadOnlyList<double>)x.ToArray()).ToArray());
}
