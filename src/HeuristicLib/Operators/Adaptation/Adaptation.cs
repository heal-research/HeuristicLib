using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>A reference-equal link to a runtime adaptation.</summary>
/// <remarks>
/// An adaptation is a link: every place holding the same instance applies the same runtime value within a resolution
/// scope. Independent instances identify independent adaptations, even when they have the same form.
/// Share this configuration explicitly when its runtime value must survive child scopes.
/// </remarks>
public abstract class Adaptation<TAdapted> : IConfigurationNode<TAdapted>
    where TAdapted : class, IAdaptation
{
    public abstract TAdapted CreateExecutionNode(ResolutionScope scope);
}
