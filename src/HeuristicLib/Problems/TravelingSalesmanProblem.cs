using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.EvolutionStrategy;
using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
using HEAL.HeuristicLib.Configuration;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;


public class TravelingSalesmanProblem : ProblemBase<Permutation, Fitness, Goal> {
  public ITravelingSalesmanProblemData ProblemData { get; }

  public TravelingSalesmanProblem(ITravelingSalesmanProblemData problemData) : base(Goal.Minimize) {
    ProblemData = problemData;
  }

  public override Fitness Evaluate(Permutation solution) {
    var tour = solution;
    double totalDistance = 0.0;
    for (int i = 0; i < tour.Count - 1; i++) {
      totalDistance += ProblemData.GetDistance(tour[i], tour[i + 1]);
    }
    totalDistance += ProblemData.GetDistance(tour[^1], tour[0]); // Return to the starting city
    return totalDistance;
  }

  public bool IsValidSolution(Permutation solution) {
    return solution.Count == ProblemData.NumberOfCities;
  }

  public override IEvaluator<Permutation, Fitness> CreateEvaluator() {
    return Evaluator.Create<Permutation, Fitness>(Evaluate);
  }

  public static TravelingSalesmanProblem CreateDefault() {
    var problemData = new TravelingSalesmanCoordinatesData(DefaultProblemData);
    return new TravelingSalesmanProblem(problemData);
  }
  private static readonly double[,] DefaultProblemData = new double[,] {
    { 100, 100 }, { 100, 200 }, { 100, 300 }, { 100, 400 },
    { 200, 100 }, { 200, 200 }, { 200, 300 }, { 200, 400 },
    { 300, 100 }, { 300, 200 }, { 300, 300 }, { 300, 400 },
    { 400, 100 }, { 400, 200 }, { 400, 300 }, { 400, 400 }
  };
  
  public PermutationEncoding CreatePermutationEncoding() {
    return new PermutationEncoding(ProblemData.NumberOfCities);
  }

  public GeneticAlgorithmSpec<Permutation, PermutationEncoding> CreateGASpec() {
    return new GeneticAlgorithmSpec<Permutation, PermutationEncoding> {
      Creator = new RandomPermutationCreatorSpec(), 
      Crossover = ProblemData.NumberOfCities > 3 ? new OrderCrossoverSpec() : null, 
      Mutator = new InversionMutatorSpec(), 
      MutationRate = 0.10
    };
  }
}

public static class GeneticAlgorithmBuilderTravelingSalesmanProblemExtensions {
  public static GeneticAlgorithmBuilder<Permutation, PermutationEncoding> UsingProblem(this GeneticAlgorithmBuilder<Permutation, PermutationEncoding> builder, TravelingSalesmanProblem problem) {
    builder.WithEvaluator(problem.CreateEvaluator());
    builder.WithGoal(problem.Goal);
    builder.WithEncoding(problem.CreatePermutationEncoding());
    builder.WithGeneticAlgorithmSpec(problem.CreateGASpec());
    return builder;
  }
}


// public class Tour {
//   public IReadOnlyList<int> Cities { get; }
//   public Tour(IEnumerable<int> cities) {
//     Cities = cities.ToList();
//   }
// }

public interface ITravelingSalesmanProblemData {
  int NumberOfCities { get; }
  double GetDistance(int fromCity, int toCity);
}

public class TravelingSalesmanProblemInstanceInformation {
  public required string Name { get; init; }
  public string? Description { get; init; }
  public string? Publication { get; init; }
  public double? BestKnownQuality { get; init; }
  public Permutation? BestKnownSolution { get; init; }
}


public abstract class TravelingSalesmanProblemData : ITravelingSalesmanProblemData {
  public int NumberOfCities { get; }
  public abstract double GetDistance(int fromCity, int toCity);
  
  public TravelingSalesmanProblemInstanceInformation? InstanceInformation { get; }
  
  protected TravelingSalesmanProblemData(int numberOfCities, TravelingSalesmanProblemInstanceInformation? instanceInformation = null) {
    NumberOfCities = numberOfCities;
    InstanceInformation = instanceInformation;
  }
}

public class TravelingSalesmanDistanceMatrixProblemData : TravelingSalesmanProblemData {
  private readonly double[,] distances;
  public IReadOnlyList<IReadOnlyList<double>> Distances => CopyToArray(distances);
  private static IReadOnlyList<IReadOnlyList<double>> CopyToArray(double[,] array) {
    double[][] result = new double[array.GetLength(0)][];
    for (int i = 0; i < array.GetLength(0); i++) {
      result[i] = new double[array.GetLength(1)];
      for (int j = 0; j < array.GetLength(1); j++) {
        result[i][j] = array[i, j];
      }
    }
    return result;
  }
  
  public TravelingSalesmanDistanceMatrixProblemData(double[,] distances, TravelingSalesmanProblemInstanceInformation? instanceInformation = null)
    : base(distances.GetLength(0), instanceInformation)
  {
    if (distances.GetLength(0) != distances.GetLength(1)) throw new ArgumentException("The distance matrix must be square.");
    if (distances.GetLength(0) < 1) throw new ArgumentException("The distance matrix must have at least one city.");
    this.distances = (double[,])distances.Clone(); // clone distances to prevent modification
  }

  public override double GetDistance(int fromCity, int toCity) {
    return distances[fromCity, toCity];
  }
}

public class TravelingSalesmanCoordinatesData : TravelingSalesmanProblemData {
  public IReadOnlyList<(double X, double Y)> Coordinates { get; }
  public DistanceMetric DistanceMetric { get; }

  public TravelingSalesmanCoordinatesData((double X, double Y)[] coordinates, DistanceMetric metric = DistanceMetric.Euclidean, TravelingSalesmanProblemInstanceInformation? instanceInformation = null)
    : base(coordinates.Length, instanceInformation) 
  {
    if (coordinates.Length < 1) throw new ArgumentException("The coordinates must have at least one city.");
    
    Coordinates = coordinates.ToArray(); // clone coordinates to prevent modification
    DistanceMetric = metric;
  }
  
  public TravelingSalesmanCoordinatesData(double[,] coordinates, DistanceMetric metric = DistanceMetric.Euclidean, TravelingSalesmanProblemInstanceInformation? instanceInformation = null)
    : base(coordinates.GetLength(0), instanceInformation)
  {
    if (coordinates.GetLength(1) != 2) throw new ArgumentException("The coordinates must have two columns.");
    if (coordinates.GetLength(0) < 1) throw new ArgumentException("The coordinates must have at least one city.");
    
    var data = new (double X, double Y)[coordinates.GetLength(0)];
    for (int i = 0; i < coordinates.GetLength(0); i++) {
      data[i] = (coordinates[i, 0], coordinates[i, 1]);
    }
    Coordinates = data;
    DistanceMetric = metric;
  }
  

  public override double GetDistance(int fromCity, int toCity) {
    (double x1, double y1) = Coordinates[fromCity];
    (double x2, double y2) = Coordinates[toCity];

    return DistanceMetric switch {
      DistanceMetric.Unknown => throw new ArgumentException("The distance metric is unknown."),
      DistanceMetric.Euclidean => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)),
      DistanceMetric.Manhattan => Math.Abs(x1 - x2) + Math.Abs(y1 - y2),
      DistanceMetric.Chebyshev => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2)),
      _ => throw new NotImplementedException()
    };
  }
}

public enum DistanceMetric {
  Unknown,
  Euclidean,
  Manhattan,
  Chebyshev
}
