using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives an operator configuration a name and forwards its executor and controls unchanged.</summary>
/// <remarks>The wrapper and child remain separate configurations; selecting by name selects the wrapper.</remarks>
public sealed record Named<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>, INamedConfiguration
    where TOperation : IOperation<TOperation>
{
    public Named(IOperator<TOperation, TCandidate> childOperator, string name) : base(childOperator)
    {
        Name = name;
    }

    public string Name { get; init; }

    public static Parameter<Named<TOperation, TCandidate>, string> NameParameter { get; } =
        new(nameof(Name), static named => named.Name, static (named, value) => named with { Name = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [NameParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        scope.ResolveOperator(ChildOperator);
}

public static class Named
{
    public static Named<TOperation, TCandidate> Create<TOperation, TCandidate>(IOperator<TOperation, TCandidate> childOperator, string name)
        where TOperation : IOperation<TOperation> => new(childOperator, name);
}

public static class NamedOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
    {
        public Named<TOperation, TCandidate> Named(string name) => new(source, name);
    }
}
