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
public sealed class GenealogyAnalyzer<TCandidate, TSearchState> : Instrument<GenealogyGraph<TCandidate>>,
    IObservationInstrument<CrossoverObservation<TCandidate>>,
    IObservationInstrument<MutationObservation<TCandidate>>,
    IObservationInstrument<AlgorithmObservation<TCandidate, TSearchState>>
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

    protected override IReadOnlyList<Substitution> CreateSubstitutions() => Recording(this, this, this);

    // The substitutions of everything this analysis observes, each recording into the given instrument.
    internal IReadOnlyList<Substitution> Recording(
        IObservationInstrument<CrossoverObservation<TCandidate>> crossoverInstrument,
        IObservationInstrument<MutationObservation<TCandidate>> mutationInstrument,
        IObservationInstrument<AlgorithmObservation<TCandidate, TSearchState>> iterationInstrument) =>
    [
        .. crossovers.Select(crossover => crossover.Invocations.RecordingInto(crossoverInstrument)),
        .. mutators.Select(mutator => mutator.Invocations.RecordingInto(mutationInstrument)),
        .. algorithms.Select(algorithm => algorithm.Iterations.RecordingInto(iterationInstrument))
    ];

    Action<CrossoverObservation<TCandidate>> IObservationInstrument<CrossoverObservation<TCandidate>>.CreateRecorder(RunResults results) =>
        RecordCrossovers(results.Result(this));

    Action<MutationObservation<TCandidate>> IObservationInstrument<MutationObservation<TCandidate>>.CreateRecorder(RunResults results) =>
        RecordMutations(results.Result(this));

    Action<AlgorithmObservation<TCandidate, TSearchState>> IObservationInstrument<AlgorithmObservation<TCandidate, TSearchState>>.CreateRecorder(RunResults results) =>
        RecordGenerations(results.Result(this));

    internal static Action<CrossoverObservation<TCandidate>> RecordCrossovers(GenealogyGraph<TCandidate> graph) => observation =>
    {
        foreach (var (parents, child) in observation.Parents.Zip(observation.Offspring))
            graph.AddConnection([parents.Parent1, parents.Parent2], child);
    };

    internal static Action<MutationObservation<TCandidate>> RecordMutations(GenealogyGraph<TCandidate> graph) => observation =>
    {
        foreach (var (parent, child) in observation.Parents.Zip(observation.Offspring))
            graph.AddConnection([parent], child);
    };

    internal Action<AlgorithmObservation<TCandidate, TSearchState>> RecordGenerations(GenealogyGraph<TCandidate> graph) => observation =>
    {
        var ordered = observation.State.Population.OrderBy(x => x.ObjectiveVector,
            observation.Problem.Objective.RequireTotalOrder(ObjectiveComparer));
        graph.SetAsNewGeneration(ordered.Select(x => x.Candidate), saveSpace);
    };

    protected override GenealogyGraph<TCandidate> CreateResult(RunResults results) => new(equality);

    protected override Aspect CreateAspect(GenealogyGraph<TCandidate> result, RunResults results) => Observe(result);

    internal Aspect Observe(GenealogyGraph<TCandidate> graph) =>
        new Collection(graph, crossovers, mutators, algorithms, RecordGenerations(graph));

    public IComparer<ObjectiveVector>? ObjectiveComparer { get; init; }

    private sealed class Collection : Aspect
    {
        public Collection(GenealogyGraph<TCandidate> graph,
            IReadOnlyList<IOperator<Crossover, TCandidate>> crossovers,
            IReadOnlyList<IOperator<Mutation, TCandidate>> mutators,
            IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms,
            Action<AlgorithmObservation<TCandidate, TSearchState>> recordGenerations)
        {
            Advices =
            [
                .. crossovers.SelectMany(crossover => Aspect.Observe(crossover.Invocations, RecordCrossovers(graph))),
                .. mutators.SelectMany(mutator => Aspect.Observe(mutator.Invocations, RecordMutations(graph))),
                .. algorithms.SelectMany(algorithm => Aspect.Observe(algorithm.Iterations, recordGenerations))
            ];
        }
    }
}

public static class GenealogyAnalysisTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a genealogy analyzer over the given observation sources.
        /// </summary>
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
