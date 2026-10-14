using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Instrumentation;

public interface IOperatorCounting<TOperator> where TOperator : class, IOperator
{
    Advice<TOperator> CreateAdvice(CountAccumulator counter);
}
