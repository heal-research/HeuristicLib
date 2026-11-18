using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>The part of a configuration node that takes observations of one type.</summary>
/// <remarks>
/// An analyzer implements this interface for every observation type it accepts, and so does any other configuration
/// node that an <c>Observed</c> wrapper places, such as a clock that observes. Observed wrappers bind its recorder
/// during construction. Implementations must coordinate concurrent observations into the same result; a callback-only
/// receiver may publish no result.
/// </remarks>
public interface IObservationReceiver<in TObservation>
{
    /// <summary>Creates the callback that receives observations for one run.</summary>
    /// <param name="results">The run's result table; use it to find any result this receiver updates.</param>
    /// <returns>A callback invoked on the observation's thread; exceptions propagate into execution.</returns>
    Action<TObservation> CreateRecorder(RunResults results);
}
