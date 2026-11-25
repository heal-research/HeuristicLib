using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Links configured base values to one runtime offset adaptation.</summary>
public sealed class OffsetAdaptation : Adaptation<AdaptedOffset>
{
    public override AdaptedOffset CreateExecutionNode(ResolutionScope scope) => new();
}

/// <summary>A runtime offset added to a configured base value.</summary>
public sealed class AdaptedOffset : IAdaptation
{
    public double Offset { get; set; }

    public double Apply(double baseValue) => baseValue + Offset;
}
