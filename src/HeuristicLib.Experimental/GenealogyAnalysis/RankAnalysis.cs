using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.GenealogyAnalysis;

public record RankAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> : Analyzer<TCandidate, TSearchSpace, TProblem, TSearchState, RankState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
    where TCandidate : notnull
{
    private readonly GenealogyAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> graphBuilder;
    private readonly IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor;

    public RankAnalysis(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm,
                        ICrossover<TCandidate, TSearchSpace, TProblem>? crossover = null,
                        IMutator<TCandidate, TSearchSpace, TProblem>? mutator = null,
                        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor = null,
                        IEqualityComparer<TCandidate>? equality = null) : base(Algorithm)
    {
        graphBuilder = new GenealogyAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm, crossover, mutator, interceptor, equality);
        this.interceptor = interceptor;
    }

    public override RankState<TCandidate> CreateInitialResult() => new(graphBuilder.CreateInitialResult());

    public override void RegisterObservations(ObservationPlan observations, RankState<TCandidate> result)
    {
        graphBuilder.RegisterObservations(observations, result.Graph); //tells sub-analyzer to record its findings into outer result
        if (interceptor is not null)
            observations.Observe(interceptor, (_, _, _, _, _) => RecordRanks(result));
    }

    private static void RecordRanks(RankState<TCandidate> state)
    {
        if (state.Graph.Nodes.Count < 2)
            return;

        var line = state.Graph.Nodes[^2].Values
                        .Where(x => x.Layer == 0)
                        .OrderBy(x => x.Rank)
                        .Select(node => node.GetAllDescendants().Where(x => x.Rank >= 0).Select(x => (double)x.Rank).DefaultIfEmpty(double.NaN).Average())
                        .ToList();
        if (line.Count > 0)
        {
            state.Ranks.Add(line);
        }
    }
}

public class RankState<TCandidate> where TCandidate : notnull
{
    public RankState(GenealogyGraph<TCandidate> graph)
    {
        Graph = graph;
    }

    public List<List<double>> Ranks { get; } = [];

    public GenealogyGraph<TCandidate> Graph { get; }

    public RankAnalysisResult<TCandidate> Result() => new(Graph, Ranks.Select(IReadOnlyList<double> (x) => x.ToArray()).ToArray());
}
