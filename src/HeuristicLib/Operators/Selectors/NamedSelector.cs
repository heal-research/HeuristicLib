using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Selectors;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a selector configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedSelector<TCandidate> : WrappingSelector<TCandidate>, INamedConfiguration
{
    public NamedSelector(ISelector<TCandidate> childSelector, string name)
        : base(childSelector)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutionFactory<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        child => child;
}

public static class NamedSelector
{
    public static NamedSelector<TCandidate> Create<TCandidate>(ISelector<TCandidate> childSelector, string name) => new(childSelector, name);

    extension<TCandidate>(ISelector<TCandidate> selector)
    {
        public NamedSelector<TCandidate> Named(string name) => Create(selector, name);
    }
}
