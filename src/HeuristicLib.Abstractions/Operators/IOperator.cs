using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator<out TExecutionInstance> : IExecutable<TExecutionInstance>
  where TExecutionInstance : IOperatorInstance
{
}

public interface IOperatorInstance : IExecutionInstance
{
}
