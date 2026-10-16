using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Terminators;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a terminator configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedTerminator<TCandidate> : WrappingTerminator<TCandidate>, INamedConfiguration
{
    public NamedTerminator(ITerminator<TCandidate> childTerminator, string name)
        : base(childTerminator)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutorFactory<ITerminationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>() =>
        child => child;
}

public static class NamedTerminator
{
    public static NamedTerminator<TCandidate> Create<TCandidate>(ITerminator<TCandidate> childTerminator, string name) => new(childTerminator, name);

    extension<TCandidate>(ITerminator<TCandidate> terminator)
    {
        public NamedTerminator<TCandidate> Named(string name) => Create(terminator, name);
    }
}
