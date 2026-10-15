using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Refiners;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a refiner configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedRefiner<TCandidate> : WrappingRefiner<TCandidate>, INamedConfiguration
{
    public NamedRefiner(IRefiner<TCandidate> childRefiner, string name)
        : base(childRefiner)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutionFactory<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        child => child;
}

public static class NamedRefiner
{
    public static NamedRefiner<TCandidate> Create<TCandidate>(IRefiner<TCandidate> childRefiner, string name) => new(childRefiner, name);

    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public NamedRefiner<TCandidate> Named(string name) => Create(refiner, name);
    }
}
