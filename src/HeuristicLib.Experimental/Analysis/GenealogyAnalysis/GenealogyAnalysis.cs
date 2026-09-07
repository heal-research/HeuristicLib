using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// Builds the genealogy graph of a run from crossover, mutation and generation boundaries.
/// </summary>
/// <remarks>
/// This analyzer holds its own data and is used for one run. Its graph accumulates across firings rather than being
/// aggregated within one, which is the case a trace-based replacement still has to cover.
/// </remarks>
public sealed class GenealogyAnalysis<TCandidate, TSearchSpace, TProblem, TSearchState> : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
    where TCandidate : notnull
{
    private readonly bool saveSpace;
    private readonly ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> crossovers;
    private readonly ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> mutators;
    private readonly ImmutableArray<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> algorithms;

    /// <param name="crossovers">Crossovers whose offspring become graph edges from two parents.</param>
    /// <param name="mutators">Mutators whose offspring become graph edges from one parent.</param>
    /// <param name="algorithms">
    /// Algorithms whose yielded populations close a generation. Without one the graph still records descent, it just
    /// has no generational structure.
    /// </param>
    public GenealogyAnalysis(IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>>? crossovers = null,
                             IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>>? mutators = null,
                             IReadOnlyList<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>>? algorithms = null,
                             IEqualityComparer<TCandidate>? equality = null,
                             bool saveSpace = false)
        : this(new GenealogyGraph<TCandidate>(equality ?? EqualityComparer<TCandidate>.Default), crossovers, mutators, algorithms, saveSpace)
    {
    }

    /// <summary>
    /// Builds into a graph another analyzer owns, so that one analysis can reuse this observation logic while
    /// publishing a different result.
    /// </summary>
    internal GenealogyAnalysis(GenealogyGraph<TCandidate> graph,
                               IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>>? crossovers,
                               IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>>? mutators,
                               IReadOnlyList<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>>? algorithms,
                               bool saveSpace)
    {
        if ((crossovers?.Count ?? 0) + (mutators?.Count ?? 0) + (algorithms?.Count ?? 0) == 0)
            throw new ArgumentException("A genealogy analysis needs at least one crossover, mutator or algorithm to observe.");

        Graph = graph;
        this.crossovers = [.. crossovers ?? []];
        this.mutators = [.. mutators ?? []];
        this.algorithms = [.. algorithms ?? []];
        this.saveSpace = saveSpace;
    }

    public GenealogyGraph<TCandidate> Graph { get; }

    public void Install(ExecutionInstanceResolverBuilder builder)
    {
        foreach (var crossover in crossovers)
            builder.Observe(Anchor.At(crossover), observation => AfterCross(observation.Offspring, observation.Parents));

        foreach (var mutator in mutators)
            builder.Observe(Anchor.At(mutator), observation => AfterMutate(observation.Offspring, observation.Parents));

        foreach (var algorithm in algorithms)
            builder.Observe(Anchor.At(algorithm), observation => CloseGeneration(observation.State, observation.Problem));
    }

    private void AfterCross(IReadOnlyList<TCandidate> offspring, IReadOnlyList<Parents<TCandidate>> parents)
    {
        foreach (var (parentPair, child) in parents.Zip(offspring))
            Graph.AddConnection([parentPair.Parent1, parentPair.Parent2], child);
    }

    private void AfterMutate(IReadOnlyList<TCandidate> offspring, IReadOnlyList<TCandidate> parents)
    {
        foreach (var (parent, child) in parents.Zip(offspring))
            Graph.AddConnection([parent], child);
    }

    private void CloseGeneration(TSearchState currentState, TProblem problem)
    {
        var ordered = currentState.Population
                                  .OrderBy(keySelector: x => x.ObjectiveVector, problem.Objective.ToTotalOrderComparer())
                                  .ToArray();
        Graph.SetAsNewGeneration(ordered.Select(x => x.Candidate), saveSpace);
    }
}

public static class GenealogyAnalysisTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a genealogy analysis over the given anchors.
        /// </summary>
        public static GenealogyAnalysis<T, TS, TP, TR> Genealogy<T, TS, TP, TR>(
            ICrossover<T, TS, TP>? crossover = null,
            IMutator<T, TS, TP>? mutator = null,
            IAlgorithm<T, TS, TP, TR>? algorithm = null,
            IEqualityComparer<T>? equality = null,
            bool saveSpace = false)
            where T : notnull
            where TS : class, ISearchSpace<T>
            where TP : class, IProblem<T, TS>
            where TR : PopulationState<T> =>
            new(
                crossover is null ? null : (IReadOnlyList<ICrossover<T, TS, TP>>)[crossover],
                mutator is null ? null : (IReadOnlyList<IMutator<T, TS, TP>>)[mutator],
                algorithm is null ? null : (IReadOnlyList<IAlgorithm<T, TS, TP, TR>>)[algorithm],
                equality,
                saveSpace);
    }
}
