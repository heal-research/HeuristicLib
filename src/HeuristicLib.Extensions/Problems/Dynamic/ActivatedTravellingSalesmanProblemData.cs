using HEAL.HeuristicLib.Problems.TravelingSalesman;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public record ActivatedTravellingSalesmanProblemData(ITravelingSalesmanProblemData ProblemData, bool[] Activations) : IProblemData;
