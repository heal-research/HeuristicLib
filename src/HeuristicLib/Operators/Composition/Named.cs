using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Gives an operator configuration a name and forwards its executor and controls unchanged.</summary>
/// <remarks>The wrapper and child remain separate configurations; a name pointcut selects the wrapper.</remarks>
public sealed record Named<TOperation, TCandidate> : WrappingOperator<TOperation, TCandidate>, INamedConfiguration
    where TOperation : IOperationDescriptor<TOperation>
{
    public Named(IOperator<TOperation, TCandidate> childOperator, string name) : base(childOperator)
    {
        Name = name;
    }

    public string Name { get; init; }

    protected override OperatorExecutorFactory CreateOperatorFactory() => OperatorExecutorFactory.Forward(ChildOperator);
}

public static class Named
{
    public static Named<TOperation, TCandidate> Create<TOperation, TCandidate>(IOperator<TOperation, TCandidate> childOperator, string name)
        where TOperation : IOperationDescriptor<TOperation> => new(childOperator, name);
}

public static class NamedOperatorExtensions
{
    extension<TOperation, TCandidate>(IOperator<TOperation, TCandidate> source)
        where TOperation : IOperationDescriptor<TOperation>
    {
        public Named<TOperation, TCandidate> Named(string name) => new(source, name);
    }
}
