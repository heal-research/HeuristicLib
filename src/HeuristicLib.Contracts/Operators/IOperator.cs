using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// An operator configuration. Every operator resolves to an execution instance, so the roles constrain against this
/// rather than against <see cref="IExecutionInstanceResolvable"/>: what a caller means is "an operator", and
/// resolvability comes with it.
/// </summary>
public interface IOperator : IExecutionInstanceResolvable;

public interface IOperatorInstance : IExecutionInstance;
