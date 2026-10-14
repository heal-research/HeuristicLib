namespace HEAL.HeuristicLib.Execution;

/// <remarks>Prepared once per target and owning state; receives each newly constructed inner execution directly.</remarks>
public delegate TExecution Advice<TExecution>(TExecution inner)
    where TExecution : class, IExecutionNode;
