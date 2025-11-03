using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.TravelingSalesman;

// This is an example problem that uses a permutation search spaces to get access to the standard operators, but also offers custom TSP-specific operators.
public class TravelingSalesmanProblem(ITravelingSalesmanProblemData problemData) : PermutationProblem(SingleObjective.Minimize, GetEncoding(problemData)) /*, IDeterministicProblem<Permutation>*/ {
  //public int? Seed { get; init; }

  public ITravelingSalesmanProblemData ProblemData { get; } = problemData;

  public TravelingSalesmanProblem() : this(null!) { }

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random) {
    var tour = solution;
    double totalDistance = 0.0;
    for (int i = 0; i < tour.Count - 1; i++) {
      totalDistance += ProblemData.GetDistance(tour[i], tour[i + 1]);
    }

    totalDistance += ProblemData.GetDistance(tour[^1], tour[0]); // Return to the starting city

    return totalDistance;
  }

  private static PermutationEncoding GetEncoding(ITravelingSalesmanProblemData problemData) {
    return new PermutationEncoding(problemData.NumberOfCities);
  }

  // public IEvaluator<Permutation> CreateEvaluator() {
  //   return new DeterministicProblemEvaluator<Permutation>(this);
  // }

  // public override Tour Decode(Permutation genotype) => new Tour(genotype);

  #region Default Instance
  public static TravelingSalesmanProblem CreateDefault() {
    var problemData = new TravelingSalesmanCoordinatesData(DefaultProblemCoordinates);
    return new TravelingSalesmanProblem(problemData);
  }

  private static readonly double[,] DefaultProblemCoordinates = new double[,] { { 100, 100 }, { 100, 200 }, { 100, 300 }, { 100, 400 }, { 200, 100 }, { 200, 200 }, { 200, 300 }, { 200, 400 }, { 300, 100 }, { 300, 200 }, { 300, 300 }, { 300, 400 }, { 400, 100 }, { 400, 200 }, { 400, 300 }, { 400, 400 } };
  #endregion
}

// public class TspEvaluator : IEvaluator<Permutation>
// {
//   
// }

// public static class TravelingSalesmanProblemEncodingExtensions {
//   public static OptimizableProblem<Tour, Permutation, PermutationSearchSpace> EncodeAsPermutationSearchSpace(this TravelingSalesmanProblem problem) {
//     var searchSpace = new PermutationSearchSpace(problem.ProblemData.NumberOfCities);
//
//     return new OptimizableProblem<Tour, Permutation, PermutationSearchSpace>(
//       problem,
//       searchSpace,
//       new PermutationDecoder()
//     );
//   }
//   
//   private class PermutationDecoder : IDecoder<Permutation, Tour> {
//     public Tour Decode(Permutation genotype) => new Tour(genotype);
//   }
// }

// public static class GeneticAlgorithmBuilderTravelingSalesmanProblemExtensions {
//   public static GeneticAlgorithmBuilder<Permutation, PermutationSearchSpace> UsingProblem(this GeneticAlgorithmBuilder<Permutation, PermutationSearchSpace> builder, TravelingSalesmanProblem problem) {
//     builder.WithEvaluator(problem.CreateEvaluator());
//     builder.WithGoal(problem.Goal);
//     builder.WithSearchSpace(problem.CreatePermutationSearchSpace());
//     builder.WithGeneticAlgorithmSpec(problem.CreateGASpec());
//     return builder;
//   }
// }

// public interface IProblemInstanceProvider<out TProblemInstance> {
//   TProblemInstance Load();
// }
