using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

public abstract class OperatorAdvice : Advice<IOperator>
{
    public abstract OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source);
}

// Lets resolution skip other operation views without treating arbitrary Advice subclasses as supported.
internal interface ITypedOperatorAdvice;

public abstract class OperatorAdvice<TOperation, TCandidate> : Advice<IOperator<TOperation, TCandidate>>,
    ITypedOperatorAdvice where TOperation : IOperationDescriptor<TOperation>
{
    public abstract OperatorAdviceWrapperFactory<TOperation, TCandidate> CreateWrapperFactory(IOperator<TOperation, TCandidate> source);
}
