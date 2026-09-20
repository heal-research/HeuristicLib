using HEAL.HeuristicLib.Encodings.RealVectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class ScaledTestFunction(double[] inputScaling, double outputScaling, ITestFunction inner)
    : MetaTestFunction(inner)
{
    protected readonly RealVector InputScaling = RealVector.Create(inputScaling);
    protected readonly double OutputScaling = outputScaling;

    public override double Evaluate(RealVector solution) => Inner.Evaluate(solution * InputScaling) * OutputScaling;
}

public class ScaledGradientTestFunction(double[] inputScaling, double outputScaling, IGradientTestFunction inner)
    : ScaledTestFunction(inputScaling, outputScaling, inner), IGradientTestFunction
{
    public RealVector EvaluateGradient(RealVector solution) =>
        inner.EvaluateGradient(solution * InputScaling) * InputScaling * OutputScaling;
}
