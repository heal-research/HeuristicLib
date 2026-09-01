using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public class MultiObjectiveTestFunctionProblem : RealVectorProblem
{
    public readonly IMultiObjectiveTestFunction TestFunction;

    public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction) : base(testFunction.Objective, GetEncoding(testFunction))
    {
        TestFunction = testFunction;
    }

    public MultiObjectiveTestFunctionProblem(IMultiObjectiveTestFunction testFunction, BoundedRealVectorSearchSpace searchSpace) : base(testFunction.Objective, searchSpace)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(searchSpace.Length, testFunction.Dimension);
        TestFunction = testFunction;
    }

    public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) => new(TestFunction.Evaluate(solution));

    private static BoundedRealVectorSearchSpace GetEncoding(IMultiObjectiveTestFunction testFunction) => new(testFunction.Dimension, testFunction.Min, testFunction.Max);
}
