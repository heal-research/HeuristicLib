using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IMultiObjectiveTestFunction
{
    int Dimension { get; }
    double Min { get; }
    double Max { get; }
    ObjectiveDirections Objective { get; }
    RealVector Evaluate(RealVector solution);
}
