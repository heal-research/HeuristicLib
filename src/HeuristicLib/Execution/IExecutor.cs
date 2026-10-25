namespace HEAL.HeuristicLib.Execution;

/// <summary>A runtime object that performs operations, such as mutating candidates or running an algorithm.</summary>
/// <remarks>Operation-specific interfaces define its callable methods. One executor can serve many calls.</remarks>
public interface IExecutor;
