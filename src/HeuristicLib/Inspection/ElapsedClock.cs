using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Reads the time a run spends advancing its algorithm.</summary>
/// <remarks>Every elapsed clock equals every other. See <see cref="Clock.Elapsed"/> for what the time includes.</remarks>
public sealed record ElapsedClock : IClock<TimeSpan>
{
    public static IReadOnlyList<IParameter> Parameters { get; } = [];

    public IReadOnlyList<IParameter> GetParameters() => Parameters;

    /// <summary>Creates a reading of this run's elapsed time.</summary>
    public IClockReading<TimeSpan> CreateExecutionNode(ResolutionScope scope) => new Reading(scope.Results);

    private sealed class Reading(RunResults results) : IClockReading<TimeSpan>
    {
        public TimeSpan Read() => results.Elapsed;
    }
}
