using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// Builds the genealogy graph of a run from crossover, mutation and generation boundaries.
/// </summary>
/// <remarks>
/// This analyzer holds its own data and is used for one run. It combines multiple kinds of boundary
/// into a graph with domain-specific queries.
/// </remarks>
public sealed class GenealogyAnalyzer<TCandidate, TSearchSpace, TProblem, TSearchState> : IAnalyzer
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
    public GenealogyAnalyzer(IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>>? crossovers = null,
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
    internal GenealogyAnalyzer(GenealogyGraph<TCandidate> graph,
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

    public IComparer<ObjectiveVector>? ObjectiveComparer { get; init; }

    public void Install(ExecutionInstanceResolverBuilder builder)
    {
        foreach (var crossover in crossovers)
            builder.Observe(crossover, observation => AfterCross(observation.Offspring, observation.Parents));

        foreach (var mutator in mutators)
            builder.Observe(mutator, observation => AfterMutate(observation.Offspring, observation.Parents));

        foreach (var algorithm in algorithms)
            builder.Observe(algorithm, observation => CloseGeneration(observation.State, observation.Problem));
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
                                  .OrderBy(keySelector: x => x.ObjectiveVector, problem.Objective.RequireTotalOrder(ObjectiveComparer))
                                  .ToArray();
        Graph.SetAsNewGeneration(ordered.Select(x => x.Candidate), saveSpace);
    }
}

public static class GenealogyAnalysisTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a genealogy analyzer over the given observation sources.
        /// </summary>
        public static GenealogyAnalyzer<T, TS, TP, TR> Genealogy<T, TS, TP, TR>(
            ICrossover<T, TS, TP>? crossover = null,
            IMutator<T, TS, TP>? mutator = null,
            IAlgorithm<T, TS, TP, TR>? algorithm = null,
            IEqualityComparer<T>? equality = null,
            bool saveSpace = false,
            IComparer<ObjectiveVector>? objectiveComparer = null)
            where T : notnull
            where TS : class, ISearchSpace<T>
            where TP : class, IProblem<T, TS>
            where TR : PopulationState<T> =>
            new(
                crossover is null ? null : (IReadOnlyList<ICrossover<T, TS, TP>>)[crossover],
                mutator is null ? null : (IReadOnlyList<IMutator<T, TS, TP>>)[mutator],
                algorithm is null ? null : (IReadOnlyList<IAlgorithm<T, TS, TP, TR>>)[algorithm],
                equality,
                saveSpace)
            { ObjectiveComparer = objectiveComparer };
    }
}
