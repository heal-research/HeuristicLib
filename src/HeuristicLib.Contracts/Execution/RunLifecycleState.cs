namespace HEAL.HeuristicLib.Execution;

/// <summary>Describes the lifecycle of one prepared and subsequently executed run.</summary>
public enum RunLifecycleState
{
    /// <summary>The run accepts analyzers and execution modules.</summary>
    Preparing,

    /// <summary>An execution entry point has started and the run's composition is frozen.</summary>
    Running,

    /// <summary>The consumer stopped between yielded root-algorithm states and the run can continue.</summary>
    Paused,

    /// <summary>The execution stream ended successfully.</summary>
    Completed,

    /// <summary>Cancellation ended execution.</summary>
    Canceled,

    /// <summary>Setup or execution failed.</summary>
    Failed,

    /// <summary>An execution stopped without preserving a continuation.</summary>
    Stopped
}
