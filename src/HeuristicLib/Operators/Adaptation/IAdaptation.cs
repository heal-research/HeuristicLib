using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Applies a runtime adaptation to a configured base value.</summary>
public interface IAdaptation : IExecutionNode
{
    double Apply(double baseValue);
}
