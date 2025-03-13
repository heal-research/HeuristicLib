using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

//public record Tour(IReadOnlyList<int> Cities);

public class TravelingSalesmanProblem : ProblemBase<Permutation, Fitness, Goal>
  //,IProvider<PermutationEncoding>
  //, IProvider<GeneticAlgorithmSpec>
{
  private readonly double[,] distances;
  private readonly int numberOfCities;

#pragma warning disable S2368
  public TravelingSalesmanProblem(double[,] distances) : base(Goal.Minimize) {
#pragma warning restore S2368
    this.distances = distances;
    numberOfCities = distances.GetLength(0);
  }

  public override Fitness Evaluate(Permutation solution) {
    var tour = solution;
    double totalDistance = 0.0;
    for (int i = 0; i < tour.Count - 1; i++) {
      totalDistance += distances[tour[i], tour[i + 1]];
    }
    totalDistance += distances[tour[^1], tour[0]];// Return to the starting city

    return totalDistance;
  }

  public override IEvaluator<Permutation, Fitness> CreateEvaluator() {
    return new Evaluator(this);
  }

  private sealed class Evaluator : EvaluatorBase<Permutation, Fitness> {
    private readonly TravelingSalesmanProblem problem;

    public Evaluator(TravelingSalesmanProblem problem) {
      this.problem = problem;
    }

    public override Fitness Evaluate(Permutation solution) {
      return problem.Evaluate(solution);
    }
  }

  public PermutationEncoding CreatePermutationEncoding() {
    return new PermutationEncoding(numberOfCities);
  }

  public GeneticAlgorithmSpec CreateGASpec() {
    return new GeneticAlgorithmSpec {
      Creator = new RandomPermutationCreatorSpec(), 
      Crossover = numberOfCities > 3 ? new OrderCrossoverSpec() : null, 
      Mutator = new InversionMutatorSpec(), 
      MutationRate = 0.10
    };
  }

  // public GeneticAlgorithmConfig<Permutation, PermutationEncoding> CreateGeneticAlgorithmConfig() {
  //   return new GeneticAlgorithmConfig<Permutation, PermutationEncoding>() {
  //     
  //   }
  // }
  
  
  // EvolutionStrategySpec IAlgorithmConfigProvider<EvolutionStrategySpec, RealVectorEncoding>.CreateDefaultConfig(RealVectorEncoding encoding) {
  //   return new EvolutionStrategySpec {
  //     PopulationSize = 100,
  //     Children = 500,
  //     Strategy = EvolutionStrategyType.Plus,
  //     Creator = new UniformRealVectorCreatorSpec(),
  //     Mutator = new SwapMutatorSpec(),
  //     InitialMutationStrength = 0.1,
  //   };
  // }
  // public void ProvideTo(IConsumer<PermutationEncoding> consumer) {
  //   var encoding = CreatePermutationEncoding();
  //   consumer.Consume(encoding);
  // }
  //
  // public void ProvideTo(IConsumer<GeneticAlgorithmSpec> consumer) {
  //   var gaSpec = CreateGASpec();
  //   consumer.Consume(gaSpec);
  // }
}

public static class GeneticAlgorithmBuilderTravelingSalesmanProblemExtensions {
  public static GeneticAlgorithmBuilder<Permutation, PermutationEncoding> UsingProblem
    (this GeneticAlgorithmBuilder<Permutation, PermutationEncoding> builder, TravelingSalesmanProblem problem)
  {
    builder.WithEvaluator(problem.CreateEvaluator());
    builder.WithGoal(problem.Goal);
    builder.WithEncoding(problem.CreatePermutationEncoding());
    builder.WithGeneticAlgorithmSpec(problem.CreateGASpec());
    return builder;
  }
}
