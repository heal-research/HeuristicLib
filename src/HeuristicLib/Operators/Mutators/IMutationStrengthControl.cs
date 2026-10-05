namespace HEAL.HeuristicLib.Operators.Mutators;

/// <summary>Controls a mutator's execution-scoped strength without invoking mutation.</summary>
/// <remarks>
/// Algorithms project this optional control from the selected source's raw binding and invoke mutation through
/// its wrapped execution. Explicit configured wrappers expose their own control deliberately.
/// </remarks>
public interface IMutationStrengthControl
{
    /// <summary>Gets or sets the execution-scoped strength without changing the reusable configuration.</summary>
    double CurrentMutationStrength { get; set; }
}
