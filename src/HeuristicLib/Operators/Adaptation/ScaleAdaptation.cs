using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Links configured base values to one runtime scale adaptation.</summary>
public sealed class ScaleAdaptation : Adaptation<AdaptedScale>
{
    public override AdaptedScale CreateExecutionNode(ResolutionScope scope) => new();
}

/// <summary>A runtime factor applied to a configured base value.</summary>
public sealed class AdaptedScale : IAdaptation
{
    public double Factor { get; set; } = 1.0;

    public double Apply(double baseValue) => baseValue * Factor;
}
