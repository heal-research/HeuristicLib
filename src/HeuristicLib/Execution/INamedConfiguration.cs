namespace HEAL.HeuristicLib.Execution;

/// <summary>A configuration carrying a descriptive name for pointcut matching.</summary>
/// <remarks>
/// Consumer-defined configurations may implement this contract directly. Names need not be unique and do not change
/// configuration reference identity, execution sharing or caller-path selection.
/// </remarks>
public interface INamedConfiguration : IConfigurationNode
{
    string Name { get; }
}
