namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IEvaluationContext<in T>
{
    double? Evaluate(T input, out bool bounded, out double? bound);
    double? LowerBound(T input, out bool evaluated, out double? quality);
}
