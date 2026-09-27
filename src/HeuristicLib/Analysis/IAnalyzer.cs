using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Owns mutable analysis results and installs the observations used to produce them.
/// </summary>
/// <remarks>
/// An analyzer is an execution module and may install additional modules. Its registered wrappers sit outside
/// configuration wrappers, including those it registers directly. Reusing an analyzer across runs
/// intentionally combines its results. An analyzer that observes concurrent runs is responsible for synchronizing its
/// mutable state.
/// </remarks>
public interface IAnalyzer : IExecutionModule;
