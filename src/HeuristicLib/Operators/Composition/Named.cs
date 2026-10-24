using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives an operator configuration a name and forwards its executor and controls unchanged.</summary>
/// <remarks>The wrapper and child remain separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record Named<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>, INamedConfiguration
    where TOperation : IOperation<TOperation>
{
    public Named(IOperator<TOperation, TCandidate> childOperator, string name) : base(childOperator)
    {
        Name = name;
    }

    public string Name { get; init; }

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
