using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Experiments;

/// <summary>Collects resources and aspects for already materialized experiment cases.</summary>
/// <remarks>Each trial snapshots its setup before the experiment run is returned. Later setup changes cannot affect it.</remarks>
public sealed class ExperimentRunSetup<TCandidate, TSearchState, TAlgorithm, TKey>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
{
    private readonly ImmutableArray<AlgorithmRunSetup<TCandidate, TSearchState>> trials;

    public ImmutableArray<ExperimentCase<TAlgorithm, TKey>> Cases { get; }

    internal ExperimentRunSetup(ImmutableArray<ExperimentCase<TAlgorithm, TKey>> cases)
    {
        Cases = cases;
        trials = [.. cases.Select(experimentCase => new AlgorithmRunSetup<TCandidate, TSearchState>(experimentCase.Algorithm))];
    }

    /// <summary>Adds one aspect per case and returns the typed case and aspect mappings in case order.</summary>
    public ImmutableArray<TrialAspect<ExperimentCase<TAlgorithm, TKey>, TAspect>> AddAspectPerTrial<TAspect>(Func<TAlgorithm, TAspect> createAspect)
        where TAspect : Aspect
    {
        ImmutableArray<TrialAspect<ExperimentCase<TAlgorithm, TKey>, TAspect>> aspects =
            [.. Cases.Select(experimentCase => TrialAspect.From(experimentCase, createAspect(experimentCase.Algorithm)))];
        for (var index = 0; index < trials.Length; index++)
            trials[index].AddAspect(aspects[index].Aspect);
        return aspects;
    }

    /// <summary>Supplies one resource per case after evaluating and validating every binding.</summary>
    /// <remarks>A callback or validation failure installs no bindings from this call. Supplied resources remain caller owned.</remarks>
    public ExperimentRunSetup<TCandidate, TSearchState, TAlgorithm, TKey> AddResourcePerTrial<TResource>(
        Func<TAlgorithm, (ResourceHandle<TResource> Handle, TResource Resource)> supply) where TResource : class
    {
        var bindings = Cases.Select(experimentCase => supply(experimentCase.Algorithm)).ToArray();
        for (var index = 0; index < trials.Length; index++)
            trials[index].CheckResourceSupply(bindings[index].Handle);
        for (var index = 0; index < trials.Length; index++)
            trials[index].AddResource(bindings[index].Handle, bindings[index].Resource);
        return this;
    }

    internal ResolutionScope BuildScope(int index) => trials[index].BuildScope();
}
