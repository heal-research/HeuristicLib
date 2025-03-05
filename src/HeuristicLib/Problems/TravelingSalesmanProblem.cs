using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

//public record Tour(IReadOnlyList<int> Cities);

public class TravelingSalesmanProblem : ProblemBase<Permutation, ObjectiveValue> {
  private readonly double[,] distances;
  private readonly int numberOfCities;

  public TravelingSalesmanProblem(double[,] distances) {
    this.distances = distances;
    numberOfCities = distances.GetLength(0);
  }

  public override ObjectiveValue Evaluate(Permutation solution) {
    var tour = solution;
    double totalDistance = 0.0;
    for (int i = 0; i < tour.Count - 1; i++)
    {
      totalDistance += distances[tour[i], tour[i + 1]];
    }
    totalDistance += distances[tour[^1], tour[0]]; // Return to the starting city
    return (totalDistance, ObjectiveDirection.Minimize);
  }

  public override IEvaluator<Permutation, ObjectiveValue> CreateEvaluator() {
    return new Evaluator(this);
  }

  private sealed class Evaluator : EvaluatorBase<Permutation, ObjectiveValue> {
    private readonly TravelingSalesmanProblem problem;

    public Evaluator(TravelingSalesmanProblem problem) {
      this.problem = problem;
    }

    public override ObjectiveValue Evaluate(Permutation solution) {
      return problem.Evaluate(solution);
    }
  }
  
  public PermutationEncoding CreatePermutationEncoding() {
    return new PermutationEncoding(numberOfCities);
  }
  
  public GeneticAlgorithmSpec CreateGeneticAlgorithmDefaultConfig() {
    return new GeneticAlgorithmSpec(
      Crossover: numberOfCities > 5 ? new OrderCrossoverSpec() : null,
      Mutator: new SwapMutatorSpec()
    );
  }
}

public static class GeneticAlgorithmBuilderTravelingSalesmanProblemExtensions {
  public static GeneticAlgorithmBuilder<PermutationEncoding, Permutation> WithProblemEncoding
    (this GeneticAlgorithmBuilder<PermutationEncoding, Permutation> builder, TravelingSalesmanProblem problem)
  {
    builder.WithEvaluator(problem.CreateEvaluator());
    builder.WithEncoding(problem.CreatePermutationEncoding());
    builder.WithSpecs(problem.CreateGeneticAlgorithmDefaultConfig());
    return builder;
  }
}
