using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

// This is an example problem that fully uses the standard search space of real vectors and only the standard operators.
public class TestFunctionProblem : RealVectorProblem<TestFunctionProblem>
{
    public readonly ITestFunction TestFunction;
    public TestFunctionProblem() : this(null!) { }

    public TestFunctionProblem(ITestFunction testFunction) : base(SingleObjective.Create(testFunction.Objective), GetEncoding(testFunction))
    {
        TestFunction = testFunction;
    }

    public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) => TestFunction.Evaluate(solution);

    private static BoundedRealVectorSearchSpace GetEncoding(ITestFunction testFunction) => new(testFunction.Dimension, testFunction.Min, testFunction.Max);
}
