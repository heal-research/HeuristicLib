namespace HEAL.HeuristicLib.Execution;

/// <summary>A configuration carrying a descriptive name, which a substitution can select it by.</summary>
/// <remarks>
/// Consumer-defined configurations may implement this contract directly. Names need not be unique and do not change
/// configuration reference identity, execution sharing or caller-path selection.
/// </remarks>
public interface INamedConfiguration : IConfigurationNode
{
    string Name { get; }
}
