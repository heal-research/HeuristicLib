namespace HEAL.HeuristicLib.Execution;

/// <summary>An execution node that holds what one run produces, such as the count of a counter.</summary>
/// <remarks>
/// A configuration whose executor is a run result is an ordinary configuration node, built once for the whole run: its
/// executor lives in the run's root scope, unless a nearer scope shares the configuration, as a budget does to restart its
/// count each time it is built. <c>run.Result(configuration)</c> reads the root scope's result, also after a failure.
/// </remarks>
public interface IRunResult : IExecutor;
