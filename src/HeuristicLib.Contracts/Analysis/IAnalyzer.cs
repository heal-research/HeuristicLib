using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// A hook that collects information about one run.
/// </summary>
/// <remarks>
/// An analyzer holds the data it collects and is used for one run. It is installed before the execution graph is
/// materialized and publishes typed reads that stay safe to call during and after execution.
/// </remarks>
public interface IAnalyzer : IExecutionHook;
