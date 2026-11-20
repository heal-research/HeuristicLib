using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A configuration node whose execution node is its reading for one run.</summary>
/// <remarks>
/// The reading is a run result: the root owns one per clock instance and run, obtained with <c>results.Result(clock)</c>.
/// Mixed clock lists use <c>IReadOnlyList&lt;IClock&gt;</c>. Equal clocks identify one trace axis.
/// A clock that needs placement also implements <see cref="ISubstitution"/>; the caller lists it after the analyzers
/// that read it.
/// </remarks>
public interface IClock : IConfigurationNode<IClockReading>;

/// <summary>A clock whose reading returns time of type <typeparamref name="TTime"/>.</summary>
/// <remarks>
/// Implement only the typed <c>CreateExecutionNode</c>; boxed construction delegates to it.
/// Typed reads such as <c>At</c> and <c>By</c> take <c>IClock&lt;TTime&gt;</c> and infer <c>TTime</c>.
/// <c>results.Result(clock)</c> infers its type for an <see cref="IClock"/>; for an <c>IClock&lt;TTime&gt;</c> it needs
/// the type argument <c>IClockReading&lt;TTime&gt;</c>, because this interface has two <c>IConfigurationNode</c> constructions.
/// </remarks>
public interface IClock<out TTime> : IClock, IConfigurationNode<IClockReading<TTime>>
{
    IClockReading IConfigurationNode<IClockReading>.CreateExecutionNode(ResolutionScope scope) =>
        ((IConfigurationNode<IClockReading<TTime>>)this).CreateExecutionNode(scope);
}
