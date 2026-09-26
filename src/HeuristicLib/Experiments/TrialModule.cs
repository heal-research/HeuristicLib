using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Experiments;

/// <summary>
/// Creates one module per experiment trial, bound to boundaries selected from that trial's algorithm.
/// </summary>
/// <remarks>
/// The factory receives each trial's algorithm and creates its module before the experiment starts.
/// The experiment retains the module for typed lookup alongside its trial.
/// </remarks>
public static class TrialModule
{
    public static TrialModule<TAlgorithm, TModule> Create<TAlgorithm, TModule>(
        Func<TAlgorithm, TModule> moduleFactory)
        where TModule : IExecutionModule => new(moduleFactory);
}

/// <summary>
/// A trial module for one algorithm type, which is what an experiment run accepts.
/// </summary>
public abstract class TrialModule<TAlgorithm>
{
    private protected TrialModule()
    {
    }

    /// <summary>
    /// Creates the module for one trial's algorithm.
    /// </summary>
    internal abstract IExecutionModule CreateFor(TAlgorithm algorithm);
}

public sealed class TrialModule<TAlgorithm, TModule> : TrialModule<TAlgorithm>
    where TModule : IExecutionModule
{
    internal Func<TAlgorithm, TModule> ModuleFactory { get; }

    internal TrialModule(Func<TAlgorithm, TModule> moduleFactory)
    {
        ModuleFactory = moduleFactory;
    }

    internal override IExecutionModule CreateFor(TAlgorithm algorithm) => ModuleFactory(algorithm);
}

/// <summary>
/// One trial and the module attached to it.
/// </summary>
public sealed record TrialAttachment<TTrial, TModule>(TTrial Trial, TModule Module)
    where TModule : IExecutionModule;

public static class TrialAttachment
{
    public static TrialAttachment<TTrial, TModule> From<TTrial, TModule>(TTrial trial, TModule module)
        where TModule : IExecutionModule => new(trial, module);
}
