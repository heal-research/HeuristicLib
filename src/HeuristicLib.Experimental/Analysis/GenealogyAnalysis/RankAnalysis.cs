using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// Reads what this analysis collected, without naming its anchor types.
/// </summary>
public interface IRankAnalysis<TCandidate> : IExecutionHook
    where TCandidate : notnull
{
    RankState<TCandidate> State { get; }
}

/// <summary>
/// Records, per generation, the average rank of the descendants of each candidate in the generation before it.
/// </summary>
/// <remarks>
/// This analyzer composes a genealogy analysis: it installs that analysis into a graph it owns and adds one observation
/// of its own. That composition is the case a replacement authoring model still has to cover.
/// </remarks>
public sealed class RankAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> : IRankAnalysis<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
    where TCandidate : notnull
{
    private readonly GenealogyAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> graphBuilder;
    private readonly ImmutableArray<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> algorithms;

    /// <param name="algorithms">
    /// Algorithms whose yielded populations close a generation, which is also where ranks are read. Without one the
    /// graph is still built, but no ranks are recorded.
    /// </param>
    public RankAnalysis(IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>>? crossovers = null,
                        IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>>? mutators = null,
                        IReadOnlyList<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>>? algorithms = null,
                        IEqualityComparer<TCandidate>? equality = null)
    {
        State = new RankState<TCandidate>(new GenealogyGraph<TCandidate>(equality ?? EqualityComparer<TCandidate>.Default));
        graphBuilder = new GenealogyAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState>(
            State.Graph, crossovers, mutators, algorithms, saveSpace: false);
        this.algorithms = [.. algorithms ?? []];
    }

    public RankState<TCandidate> State { get; }

    public void Install(ExecutionInstanceResolverBuilder builder)
    {
        // Installed first, so the graph already contains this generation when the ranks over it are read.
        graphBuilder.Install(builder);

        foreach (var algorithm in algorithms)
            builder.Observe(Anchor.At(algorithm), _ => RecordRanks(State));
    }

    private static void RecordRanks(RankState<TCandidate> state)
    {
        if (state.Graph.Nodes.Count < 2)
            return;

        var line = state.Graph.Nodes[^2].Values
                        .Where(x => x.Layer == 0)
                        .OrderBy(x => x.Rank)
                        .Select(node =>
                            node.GetAllDescendants().Where(x => x.Rank >= 0).Select(x => (double)x.Rank)
                                .DefaultIfEmpty(double.NaN).Average())
                        .ToList();
        if (line.Count > 0)
        {
            state.Ranks.Add(line);
        }
    }

    private sealed class Recorder(RankState<TCandidate> state)
        : IObservationRecorder<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>
    {
        public void Record(InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState> observation) =>
            RecordRanks(state);
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

    public RankAnalysisResult<TCandidate> Result() =>
        new(Graph, Ranks.Select(IReadOnlyList<double> (x) => x.ToArray()).ToArray());
}

public static class RankAnalysisTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a rank analysis over the given anchors.
        /// </summary>
        public static RankAnalysis<T, TS, TP, TR> Rank<T, TS, TP, TR>(
            ICrossover<T, TS, TP>? crossover = null,
            IMutator<T, TS, TP>? mutator = null,
            IAlgorithm<T, TS, TP, TR>? algorithm = null,
            IEqualityComparer<T>? equality = null)
            where T : notnull
            where TS : class, ISearchSpace<T>
            where TP : class, IProblem<T, TS>
            where TR : PopulationState<T> =>
            new(
                crossover is null ? null : (IReadOnlyList<ICrossover<T, TS, TP>>)[crossover],
                mutator is null ? null : (IReadOnlyList<IMutator<T, TS, TP>>)[mutator],
                algorithm is null ? null : (IReadOnlyList<IAlgorithm<T, TS, TP, TR>>)[algorithm],
                equality);
    }
}
