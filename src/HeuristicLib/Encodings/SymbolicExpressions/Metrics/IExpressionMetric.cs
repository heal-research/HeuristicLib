using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public interface IExpressionMetric
{
    ObjectiveDirection Direction { get; }

    double Evaluate(ExpressionTree expression);
}
