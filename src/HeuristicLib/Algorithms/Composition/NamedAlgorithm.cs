using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Algorithms;

/// <summary>Gives an algorithm configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>, INamedConfiguration
    where TSearchState : class, ISearchState
{
    public NamedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, string name)
        : base(childAlgorithm)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        child => child;
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
