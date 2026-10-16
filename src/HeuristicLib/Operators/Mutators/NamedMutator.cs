using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Mutators;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a mutator configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedMutator<TCandidate> : WrappingMutator<TCandidate>, INamedConfiguration
{
    public NamedMutator(IMutator<TCandidate> childMutator, string name)
        : base(childMutator)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutorFactory<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        child => child;
}

public static class NamedMutator
{
    public static NamedMutator<TCandidate> Create<TCandidate>(IMutator<TCandidate> childMutator, string name) => new(childMutator, name);

    extension<TCandidate>(IMutator<TCandidate> mutator)
    {
        public NamedMutator<TCandidate> Named(string name) => Create(mutator, name);
    }
}
