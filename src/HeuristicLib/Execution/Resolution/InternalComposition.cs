namespace HEAL.HeuristicLib.Execution;

/// <summary>Owns an internal composition's preparation and isolated dependencies for one owner.</summary>
internal sealed class InternalComposition
{
    private readonly ExecutionPreparation source;

    private InternalComposition(ExecutionPreparation source, IConfigurationNode configuration, ExecutionSharingScope owner)
    {
        this.source = source;
        Sharing = new ExecutionSharingScope(owner, internalSource: source.Source);
        Preparation = source.CreateWrapper(configuration, Sharing);
    }

    public ExecutionPreparation Preparation { get; }
    public ExecutionSharingScope Sharing { get; }

    public static ExecutionPreparation GetOrAdd(Dictionary<IConfigurationNode, InternalComposition> compositions,
        IConfigurationNode configuration, ExecutionPreparation source, ExecutionSharingScope owner)
    {
        if (ReferenceEquals(configuration, source.Source))
            throw new InvalidOperationException("An internal composition must wrap its source, not return it unchanged.");

        if (!compositions.TryGetValue(configuration, out var composition))
        {
            composition = new InternalComposition(source, configuration, owner);
            compositions.Add(configuration, composition);
        }
        else if (!ReferenceEquals(composition.source, source))
        {
            throw new InvalidOperationException("An internal composition can reference only one selected source.");
        }

        // Keep the isolated scope alive through the table entry; preparations hold scopes weakly.
        composition.Preparation.PrepareOnce();
        return composition.Preparation;
    }
}
