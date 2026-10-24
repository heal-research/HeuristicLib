using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// Builds the genealogy graph of a run from crossover, mutation and generation boundaries.
/// </summary>
/// <remarks>
/// This analyzer holds its own data. It combines multiple kinds of boundary into a graph with domain-specific
/// queries. The graph is an accumulator, so read it once the run has finished.
/// </remarks>
public sealed class GenealogyAnalyzer<TCandidate, TSearchState> : AccumulatingAnalyzer
    where TSearchState : PopulationState<TCandidate>
    where TCandidate : notnull
{
    private readonly bool saveSpace;
    private readonly GenealogyGraph<TCandidate> graph;

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
        : this(new GenealogyGraph<TCandidate>(equality ?? EqualityComparer<TCandidate>.Default), crossovers, mutators, algorithms, saveSpace)
    {
    }

    /// <summary>
    /// Builds into a graph another analyzer owns, so that one analysis can reuse this observation logic while
    /// publishing a different result.
    /// </summary>
    internal GenealogyAnalyzer(GenealogyGraph<TCandidate> graph,
                               IReadOnlyList<IOperator<Crossover, TCandidate>>? crossovers,
                               IReadOnlyList<IOperator<Mutation, TCandidate>>? mutators,
                               IReadOnlyList<IAlgorithm<TCandidate, TSearchState>>? algorithms,
                               bool saveSpace)
    {
        if ((crossovers?.Count ?? 0) + (mutators?.Count ?? 0) + (algorithms?.Count ?? 0) == 0)
            throw new ArgumentException("A genealogy analysis needs at least one crossover, mutator or algorithm to observe.");

        this.graph = graph;
        this.saveSpace = saveSpace;
        Advices =
        [
            .. (crossovers ?? []).SelectMany(crossover => Aspect.Observe(
                crossover.Calls, observation => AfterCross(observation.Offspring, observation.Parents))),
            .. (mutators ?? []).SelectMany(mutator => Aspect.Observe(
                mutator.Calls, observation => AfterMutate(observation.Offspring, observation.Parents))),
            .. (algorithms ?? []).SelectMany(algorithm => Aspect.Observe(
                algorithm.Iterations, observation => CloseGeneration(observation.State, observation.Problem)))
        ];
    }

    /// <summary>The graph this analyzer accumulates.</summary>
    public GenealogyGraph<TCandidate> Graph => graph;

    public IComparer<ObjectiveVector>? ObjectiveComparer { get; init; }

    private void AfterCross(IReadOnlyList<TCandidate> offspring, IReadOnlyList<Parents<TCandidate>> parents)
    {
        foreach (var (parentPair, child) in parents.Zip(offspring))
            graph.AddConnection([parentPair.Parent1, parentPair.Parent2], child);
    }

    private void AfterMutate(IReadOnlyList<TCandidate> offspring, IReadOnlyList<TCandidate> parents)
    {
        foreach (var (parent, child) in parents.Zip(offspring))
            graph.AddConnection([parent], child);
    }

    private void CloseGeneration(TSearchState currentState, IProblem problem)
    {
        var ordered = currentState.Population
                                  .OrderBy(keySelector: x => x.ObjectiveVector, problem.Objective.RequireTotalOrder(ObjectiveComparer))
                                  .ToArray();
        graph.SetAsNewGeneration(ordered.Select(x => x.Candidate), saveSpace);
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
