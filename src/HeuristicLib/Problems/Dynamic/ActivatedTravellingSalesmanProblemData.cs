using HEAL.HeuristicLib.Problems.TravelingSalesman;
using MoreLinq;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public record ActivatedTravellingSalesmanProblemData(ITravelingSalesmanProblemData ProblemData, bool[] Activations) : IProblemData;
