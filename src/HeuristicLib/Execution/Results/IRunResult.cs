namespace HEAL.HeuristicLib.Execution;

/// <summary>An execution node that holds what one run produces, such as the count of a counter.</summary>
/// <remarks>
/// A result defaults to the resolution tree's root scope. <see cref="ResolutionScope.Share"/> can narrow ordinary
/// resolution, as a budget does for a counter; <see cref="RunResults.Result{TResult}"/> always reads the root's result.
/// Analyzer recording uses that root result across all Cycle turns. Results remain readable after failure; each result
/// defines its own synchronization and live-read contract. There is no automatic disposal contract.
/// </remarks>
public interface IRunResult : IExecutionNode;
