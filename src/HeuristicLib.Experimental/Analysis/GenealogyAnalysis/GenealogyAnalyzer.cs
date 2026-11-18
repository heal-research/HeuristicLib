using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// Builds the genealogy graph of a run from crossover, mutation and generation boundaries.
/// </summary>
/// <remarks>
/// Each run creates its own graph. It combines multiple kinds of boundary into a graph with domain-specific
/// queries. The graph is an accumulator, so read it once the run has finished.
/// </remarks>
public sealed class GenealogyAnalyzer<TCandidate, TSearchState> : Analyzer<GenealogyGraph<TCandidate>>,
    IObservationReceiver<CrossoverObservation<TCandidate>>,
    IObservationReceiver<MutationObservation<TCandidate>>,
    IObservationReceiver<AlgorithmObservation<TCandidate, TSearchState>>
    where TSearchState : PopulationState<TCandidate>
    where TCandidate : notnull
{
    private readonly bool saveSpace;
    private readonly ValueArray<IOperator<Crossover, TCandidate>> crossovers;
    private readonly ValueArray<IOperator<Mutation, TCandidate>> mutators;
    private readonly ValueArray<IAlgorithm<TCandidate, TSearchState>> algorithms;
    private readonly IEqualityComparer<TCandidate> equality;

    /// <param name="crossovers">Crossovers whose offspring become graph edges from two parents.</param>
    /// <param name="mutators">Mutators whose offspring become graph edges from one parent.</param>
    /// <param name="algorithms">
    /// Algorithms whose yielded populations close a generation. Without one the graph still records descent, it just
    /// has no generational structure.
    /// </param>
    /// <param name="equality">
    /// Decides which candidates are the same graph node. Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="saveSpace">
    /// Keeps parent links for the latest generation only, discarding older generations as each new one closes.
    /// </param>
    /// <exception cref="ArgumentException">No crossover, mutator or algorithm is supplied.</exception>
    public GenealogyAnalyzer(IReadOnlyList<IOperator<Crossover, TCandidate>>? crossovers = null,
                             IReadOnlyList<IOperator<Mutation, TCandidate>>? mutators = null,
                             IReadOnlyList<IAlgorithm<TCandidate, TSearchState>>? algorithms = null,
                             IEqualityComparer<TCandidate>? equality = null,
                             bool saveSpace = false)
    {
        if ((crossovers?.Count ?? 0) + (mutators?.Count ?? 0) + (algorithms?.Count ?? 0) == 0)
            throw new ArgumentException("A genealogy analysis needs at least one crossover, mutator or algorithm to observe.");
        this.crossovers = (crossovers ?? []).ToValueArray();
        this.mutators = (mutators ?? []).ToValueArray();
        this.algorithms = (algorithms ?? []).ToValueArray();
        this.equality = equality ?? EqualityComparer<TCandidate>.Default;
        this.saveSpace = saveSpace;
    }

    protected override IReadOnlyList<ISubstitution> CreateSubstitutions() =>
    [
        .. crossovers.Select(crossover => crossover.Invocations.RecordingInto(this)),
        .. mutators.Select(mutator => mutator.Invocations.RecordingInto(this)),
        .. algorithms.Select(algorithm => algorithm.Iterations.RecordingInto(this))
    ];

    Action<CrossoverObservation<TCandidate>> IObservationReceiver<CrossoverObservation<TCandidate>>.CreateRecorder(RunResults results) =>
        RecordCrossovers(results.Result(this));

    Action<MutationObservation<TCandidate>> IObservationReceiver<MutationObservation<TCandidate>>.CreateRecorder(RunResults results) =>
        RecordMutations(results.Result(this));

    Action<AlgorithmObservation<TCandidate, TSearchState>> IObservationReceiver<AlgorithmObservation<TCandidate, TSearchState>>.CreateRecorder(RunResults results) =>
        RecordGenerations(results.Result(this));

    private static Action<CrossoverObservation<TCandidate>> RecordCrossovers(GenealogyGraph<TCandidate> graph) => observation =>
    {
        foreach (var (parents, child) in observation.Parents.Zip(observation.Offspring))
            graph.AddConnection([parents.Parent1, parents.Parent2], child);
    };

    private static Action<MutationObservation<TCandidate>> RecordMutations(GenealogyGraph<TCandidate> graph) => observation =>
    {
        foreach (var (parent, child) in observation.Parents.Zip(observation.Offspring))
            graph.AddConnection([parent], child);
    };

    private Action<AlgorithmObservation<TCandidate, TSearchState>> RecordGenerations(GenealogyGraph<TCandidate> graph) => observation =>
    {
        var ordered = observation.State.Population.OrderBy(x => x.ObjectiveVector,
            observation.Problem.Objective.RequireTotalOrder(ObjectiveComparer));
        graph.SetAsNewGeneration(ordered.Select(x => x.Candidate), saveSpace);
    };

    /// <inheritdoc/>
    public override GenealogyGraph<TCandidate> CreateExecutionNode(ResolutionScope scope) => new(equality);

    /// <summary>Orders each observed population; when omitted, its problem's objective must supply a total order.</summary>
    public IComparer<ObjectiveVector>? ObjectiveComparer { get; init; }
}

public static class GenealogyAnalysisTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a genealogy analyzer over the given observation sources.
        /// </summary>
        /// <param name="crossover">An optional crossover to observe, matched by reference.</param>
        /// <param name="mutator">An optional mutator to observe, matched by reference.</param>
        /// <param name="algorithm">An optional algorithm whose yielded populations close generations.</param>
        /// <param name="equality">Candidate identity in the graph; omitted means default equality.</param>
        /// <param name="saveSpace">Discards older generation contents as new generations close.</param>
        /// <param name="objectiveComparer">Orders each generation; omitted means the problem must supply a total order.</param>
        /// <exception cref="ArgumentException">No crossover, mutator or algorithm is supplied.</exception>
        public static GenealogyAnalyzer<T, TR> Genealogy<T, TR>(
            IOperator<Crossover, T>? crossover = null,
            IOperator<Mutation, T>? mutator = null,
            IAlgorithm<T, TR>? algorithm = null,
            IEqualityComparer<T>? equality = null,
            bool saveSpace = false,
            IComparer<ObjectiveVector>? objectiveComparer = null)
            where T : notnull
            where TR : PopulationState<T> =>
            new(
                crossover is null ? null : (IReadOnlyList<IOperator<Crossover, T>>)[crossover],
                mutator is null ? null : (IReadOnlyList<IOperator<Mutation, T>>)[mutator],
                algorithm is null ? null : (IReadOnlyList<IAlgorithm<T, TR>>)[algorithm],
                equality,
                saveSpace)
            { ObjectiveComparer = objectiveComparer };
    }
}
