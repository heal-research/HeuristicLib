using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Instrumentation;

public interface IOperatorDurationMeasuring<TOperator> where TOperator : class, IOperator
{
    Advice<TOperator> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider);
}
