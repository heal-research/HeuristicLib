using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

//public record Tour(IReadOnlyList<int> Cities);

public class TravelingSalesmanProblem : ProblemBase<Permutation, ObjectiveValue>
{
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
    return new TSPEvaluator(this);
  }

  private class TSPEvaluator : EvaluatorBase<Permutation, ObjectiveValue> {
    private readonly TravelingSalesmanProblem problem;

    public TSPEvaluator(TravelingSalesmanProblem problem) {
      this.problem = problem;
    }

    public override ObjectiveValue Evaluate(Permutation solution) {
      return problem.Evaluate(solution);
    }
  }
  
  public PermutationEncoding CreatePermutationEncoding() {
    return new PermutationEncoding(numberOfCities);
  }
  
  public GeneticAlgorithmSpec CreateGeneticAlgorithmConfig() {
    return new GeneticAlgorithmSpec(
      Crossover: new OrderCrossoverSpec(),
      Mutator: new SwapMutatorSpec()
    );
  }

  // public TspPermutationEncodingBundle CreatePermutationEncodingBundle()
  // {
  //   var encoding = new PermutationEncoding(numberOfCities);
  //   
  //   return new TspPermutationEncodingBundle(
  //     encoding,
  //     RandomPermutationCreator.FromEncoding(encoding),
  //     new OrderCrossover()
  //   );
  // }
}
//
// public class TspPermutationEncodingBundle : IEncodingBundle<Permutation, PermutationEncoding>, ICreatorProvider<Permutation>, ICrossoverProvider<Permutation> {
//   public TspPermutationEncodingBundle(PermutationEncoding encoding, Func<CreatorParameters, ICreator<Permutation>> creatorFactory, ICrossover<Permutation> crossover) {
//     Encoding = encoding;
//     CreatorFactory = creatorFactory;
//     Crossover = crossover;
//   }
//
//   public PermutationEncoding Encoding { get; }
//
//   public Func<CreatorParameters, ICreator<Permutation>> CreatorFactory { get; }
//   public ICrossover<Permutation> Crossover { get; }
// }
