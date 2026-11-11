using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>Gives an algorithm configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; selecting by name selects the wrapper.</remarks>
public sealed record NamedAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>, INamedConfiguration
    where TSearchState : class, ISearchState
{
    public NamedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, string name)
        : base(childAlgorithm)
    {
        Name = name;
    }

    public string Name { get; init; }

    public static Parameter<NamedAlgorithm<TCandidate, TSearchState>, string> NameParameter { get; } =
        new(nameof(Name), static named => named.Name, static (named, value) => named with { Name = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [NameParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        scope.Resolve(ChildAlgorithm);
}

public static class NamedAlgorithm
{
    public static NamedAlgorithm<TCandidate, TSearchState> Create<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> childAlgorithm, string name)
        where TSearchState : class, ISearchState => new(childAlgorithm, name);

    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public NamedAlgorithm<TCandidate, TSearchState> Named(string name) => Create(algorithm, name);
    }
}
