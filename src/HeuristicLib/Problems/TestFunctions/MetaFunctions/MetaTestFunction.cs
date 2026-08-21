using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public abstract class MetaTestFunction : ITestFunction
{
    protected readonly ITestFunction Inner;
    protected MetaTestFunction(ITestFunction inner)
    {
        Inner = inner;
    }

    public virtual int Dimension => Inner.Dimension;
    public double Min => Inner.Min;
    public double Max => Inner.Max;
    public ObjectiveDirection Objective => Inner.Objective;

    public abstract double Evaluate(RealVector solution);
}
