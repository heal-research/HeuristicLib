using HEAL.HeuristicLib.Encodings.RealVectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class ShiftedTestFunction(RealVector shiftVector, ITestFunction inner) : MetaTestFunction(inner)
{
    protected RealVector ShiftVector { get; } = shiftVector;

    public override double Evaluate(RealVector solution) => Inner.Evaluate(solution + ShiftVector);
}

public class ShiftedGradientTestFunction(RealVector shiftVector, IGradientTestFunction inner) : ShiftedTestFunction(shiftVector, inner), IGradientTestFunction
{
    protected IGradientTestFunction GradientInner { get; } = inner;

    public RealVector EvaluateGradient(RealVector solution) =>
        GradientInner.EvaluateGradient(solution + ShiftVector);
}
