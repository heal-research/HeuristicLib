using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator;

public interface IOperator<out TExecutionInstance> : IOperator, IExecutable<TExecutionInstance>
  where TExecutionInstance : IOperatorInstance;

public interface IOperatorInstance : IExecutionInstance;
