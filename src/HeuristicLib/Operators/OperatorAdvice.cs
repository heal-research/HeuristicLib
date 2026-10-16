using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

public abstract class OperatorAdvice : Advice<IOperator>
{
    public abstract OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source);
}

// Lets resolution skip other operation views without treating arbitrary Advice subclasses as supported.
internal interface ITypedOperatorAdvice;

public abstract class OperatorAdvice<TCandidate, TOperation> : Advice<IOperator<TCandidate, TOperation>>,
    ITypedOperatorAdvice where TOperation : IOperationDescriptor<TOperation>
{
    public abstract OperatorAdviceWrapperFactory<TCandidate, TOperation> CreateWrapperFactory(IOperator<TCandidate, TOperation> source);
}
