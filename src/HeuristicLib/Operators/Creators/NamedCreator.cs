using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives a creator configuration a name and forwards its execution unchanged.</summary>
/// <remarks>The wrapper and its child are separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record NamedCreator<TCandidate> : WrappingCreator<TCandidate>, INamedConfiguration
{
    public NamedCreator(ICreator<TCandidate> childCreator, string name)
        : base(childCreator)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override WrapperExecutionFactory<ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
        child => child;
}

public static class NamedCreator
{
    public static NamedCreator<TCandidate> Create<TCandidate>(ICreator<TCandidate> childCreator, string name) => new(childCreator, name);

    extension<TCandidate>(ICreator<TCandidate> creator)
    {
        public NamedCreator<TCandidate> Named(string name) => Create(creator, name);
    }
}
