using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface ITestFunction
{
    int Dimension { get; }
    double Min { get; }
    double Max { get; }
    ObjectiveDirection Objective { get; }
    double Evaluate(RealVector solution);
}
