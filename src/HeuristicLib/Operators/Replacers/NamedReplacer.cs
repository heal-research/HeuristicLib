using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Replacers;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a replacer configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedReplacer<TCandidate> : WrappingReplacer<TCandidate>, INamedConfiguration
{
    public NamedReplacer(IReplacer<TCandidate> childReplacer, string name)
        : base(childReplacer)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        child => child;
}

public static class NamedReplacer
{
    public static NamedReplacer<TCandidate> Create<TCandidate>(IReplacer<TCandidate> childReplacer, string name) => new(childReplacer, name);

    extension<TCandidate>(IReplacer<TCandidate> replacer)
    {
        public NamedReplacer<TCandidate> Named(string name) => Create(replacer, name);
    }
}
