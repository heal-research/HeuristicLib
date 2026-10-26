using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Reports observations at a named point inside a configuration's executor.</summary>
public interface IObservationPointExecutor<in TObservation> : IExecutor
{
    void Report(TObservation observation);
}

/// <summary>A named child configuration that is also the source of the observations reported through its executor.</summary>
/// <remarks>
/// Declare a point with <c>ObservationPoint.Of&lt;TObservation&gt;(this, nameof(Point))</c> and resolve it like any other
/// child. Repeated access returns the same point for that configuration and name; a record copy gets its own point.
/// Observation advice wraps the point's executor, so clocks and traces follow ordinary advice ordering and identity.
/// An unobserved point still resolves to an executor that ignores reports.
/// </remarks>
public sealed class ObservationPoint<TObservation> : ObservationSource<TObservation>, IConfigurationNode<IObservationPointExecutor<TObservation>>, INamedConfiguration
{
    private static readonly ConditionalWeakTable<IConfigurationNode, Dictionary<string, ObservationPoint<TObservation>>> points = new();

    private ObservationPoint(string name)
    {
        Name = name;
    }

    public string Name { get; }

    internal override IConfigurationNode Configuration => this;

    internal static ObservationPoint<TObservation> Of(IConfigurationNode configuration, string name)
    {
        var named = points.GetValue(configuration, static _ => new(StringComparer.Ordinal));
        lock (named)
        {
            if (!named.TryGetValue(name, out var point))
                named.Add(name, point = new(name));
            return point;
        }
    }

    internal override Aspect Observe(Action<TObservation> callback) =>
        Aspect.Create(Pointcut.Reference(this), new ObservationPointAdvice<TObservation>(callback));

    public IObservationPointExecutor<TObservation> CreateExecutor(ResolutionScope scope) => Executor.Instance;

    private sealed class Executor : IObservationPointExecutor<TObservation>
    {
        internal static readonly Executor Instance = new();

        public void Report(TObservation observation)
        {
            // Unobserved points deliberately discard reports.
        }
    }
}

public static class ObservationPoint
{
    /// <summary>Returns the observation point of this configuration and name, without retaining the configuration globally.</summary>
    public static ObservationPoint<TObservation> Of<TObservation>(IConfigurationNode configuration, string name) =>
        ObservationPoint<TObservation>.Of(configuration, name);
}

file sealed class ObservationPointAdvice<TObservation>(Action<TObservation> observe)
    : Advice<ObservationPoint<TObservation>, IObservationPointExecutor<TObservation>>
{
    public override IObservationPointExecutor<TObservation> Wrap(IObservationPointExecutor<TObservation> executor, ObservationPoint<TObservation> source) =>
        new Executor(executor, observe);

    private sealed class Executor(IObservationPointExecutor<TObservation> child, Action<TObservation> observe)
        : IObservationPointExecutor<TObservation>, IExecutorControlProvider
    {
        public void Report(TObservation observation)
        {
            child.Report(observation);
            observe(observation);
        }

        public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(child);
    }
}
