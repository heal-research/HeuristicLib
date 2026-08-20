using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public interface IExpressionMetric
{
    ObjectiveDirection Direction { get; }

    double Evaluate(ExpressionTree expression);
}
