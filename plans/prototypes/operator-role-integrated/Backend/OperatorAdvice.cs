using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

public abstract class OperatorAdvice : Advice<IOperator>
{
    public abstract PreparedOperatorAdvice Prepare(IOperator source);
}

// Lets resolution skip other operation views without treating arbitrary Advice subclasses as supported.
internal interface ITypedOperatorAdvice;

public abstract class OperatorAdvice<TCandidate, TOperation> : Advice<IOperator<TCandidate, TOperation>>,
    ITypedOperatorAdvice where TOperation : IOperation<TOperation>
{
    public abstract PreparedOperatorAdvice<TCandidate, TOperation> Prepare(IOperator<TCandidate, TOperation> source);
}
