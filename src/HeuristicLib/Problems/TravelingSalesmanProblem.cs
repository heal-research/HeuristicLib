using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Problems;

public class Tour {
  public IReadOnlyList<int> Cities { get; }
  public Tour(IEnumerable<int> cities) {
    Cities = cities.ToList();
  }
}

public class TravelingSalesmanProblem : ProblemBase<Tour, TravelingSalesmanInstance>, IEncodableProblem<Tour, Permutation, PermutationEncoding<Tour>> {

  //public override Objective Objective => SingleObjective.Minimize;

  public override Fitness Evaluate(Tour solution, TravelingSalesmanInstance instance) {
    var tour = solution.Cities;
    double totalDistance = 0.0;
    for (int i = 0; i < tour.Count - 1; i++) {
      totalDistance += instance.GetDistance(tour[i], tour[i + 1]);
    }
    totalDistance += instance.GetDistance(tour[^1], tour[0]); // Return to the starting city
    return totalDistance;
  }
  
  public PermutationEncoding<Tour> GetEncoding() {
    return new PermutationEncoding<Tour>(new TourDecoder()) {
      Creator = new RandomPermutationCreator(),
      Crossover = new OrderCrossover(),
      Mutator = new InversionMutator()
    };
  }
  
  #region Default Instance
  public static TravelingSalesmanInstance CreateDefaultInstance() {
    var problemData = new TravelingSalesmanCoordinatesData(DefaultProblemData);
    return new TravelingSalesmanInstance(problemData);
  }
  private static readonly double[,] DefaultProblemData = new double[,] {
    { 100, 100 }, { 100, 200 }, { 100, 300 }, { 100, 400 },
    { 200, 100 }, { 200, 200 }, { 200, 300 }, { 200, 400 },
    { 300, 100 }, { 300, 200 }, { 300, 300 }, { 300, 400 },
    { 400, 100 }, { 400, 200 }, { 400, 300 }, { 400, 400 }
  };
  #endregion
  
  private sealed class TourDecoder : IDecoder<Permutation, Tour> {
    public Tour Decode(Permutation genotype) => new Tour(genotype);
  }
}

public class TravelingSalesmanInstance : IBindableProblemInstance<PermutationEncodingParameter, Permutation> {
  private readonly ITravelingSalesmanProblemData problemData;
  
  public int NumberOfCities => problemData.NumberOfCities;
  public TravelingSalesmanInstanceInformation? InstanceInformation { get; }
  
  public TravelingSalesmanInstance(ITravelingSalesmanProblemData problemData, TravelingSalesmanInstanceInformation? instanceInformation = null) {
    this.problemData = problemData;
    InstanceInformation = instanceInformation;
  }

  public double GetDistance(int fromCity, int toCity) => problemData.GetDistance(fromCity, toCity);
  
  public Objective GetObjective() => SingleObjective.Minimize;
  
  public PermutationEncodingParameter GetEncodingParameter() {
    return new PermutationEncodingParameter(length: NumberOfCities);
  }
}

public class TravelingSalesmanInstanceInformation {
  public required string Name { get; init; }
  public string? Description { get; init; }
  public string? Publication { get; init; }
  public double? BestKnownQuality { get; init; }
  public Permutation? BestKnownSolution { get; init; }
}


public interface ITravelingSalesmanProblemData {
  int NumberOfCities { get; }
  double GetDistance(int fromCity, int toCity);
}

public class TravelingSalesmanDistanceMatrixProblemData : ITravelingSalesmanProblemData {
  private readonly double[,] distances;
  
  public int NumberOfCities => distances.GetLength(0);
  public IReadOnlyList<IReadOnlyList<double>> Distances => Clone(distances);
  
  #pragma warning disable S2368
  public TravelingSalesmanDistanceMatrixProblemData(double[,] distances) {
    if (distances.GetLength(0) != distances.GetLength(1)) throw new ArgumentException("The distance matrix must be square.");
    if (distances.GetLength(0) < 1) throw new ArgumentException("The distance matrix must have at least one city.");
    this.distances = (double[,])distances.Clone(); // clone distances to prevent modification
  }
  #pragma warning restore S2368

  public double GetDistance(int fromCity, int toCity) {
    return distances[fromCity, toCity];
  }
  
  private static IReadOnlyList<IReadOnlyList<double>> Clone(double[,] array) {
    double[][] result = new double[array.GetLength(0)][];
    for (int i = 0; i < array.GetLength(0); i++) {
      result[i] = new double[array.GetLength(1)];
      for (int j = 0; j < array.GetLength(1); j++) {
        result[i][j] = array[i, j];
      }
    }
    return result;
  }
}

public class TravelingSalesmanCoordinatesData : ITravelingSalesmanProblemData {
  public int NumberOfCities => Coordinates.Count;
  public IReadOnlyList<(double X, double Y)> Coordinates { get; }
  public DistanceMetric DistanceMetric { get; }

  public TravelingSalesmanCoordinatesData((double X, double Y)[] coordinates, DistanceMetric metric = DistanceMetric.Euclidean) {
    if (coordinates.Length < 1) throw new ArgumentException("The coordinates must have at least one city.");
    
    Coordinates = coordinates.ToArray(); // clone coordinates to prevent modification
    DistanceMetric = metric;
  }

  #pragma warning disable S2368
  public TravelingSalesmanCoordinatesData(double[,] coordinates, DistanceMetric metric = DistanceMetric.Euclidean) {
    if (coordinates.GetLength(1) != 2) throw new ArgumentException("The coordinates must have two columns.");
    if (coordinates.GetLength(0) < 1) throw new ArgumentException("The coordinates must have at least one city.");
    
    var data = new (double X, double Y)[coordinates.GetLength(0)];
    for (int i = 0; i < coordinates.GetLength(0); i++) {
      data[i] = (coordinates[i, 0], coordinates[i, 1]);
    }
    Coordinates = data;
    DistanceMetric = metric;
  }
  #pragma warning restore S2368

  public double GetDistance(int fromCity, int toCity) {
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


// public static class GeneticAlgorithmBuilderTravelingSalesmanProblemExtensions {
//   public static GeneticAlgorithmBuilder<Permutation, PermutationEncodingParameter> UsingProblem(this GeneticAlgorithmBuilder<Permutation, PermutationEncodingParameter> builder, TravelingSalesmanProblem problem) {
//     builder.WithEvaluator(problem.CreateEvaluator());
//     builder.WithGoal(problem.Goal);
//     builder.WithEncoding(problem.CreatePermutationEncoding());
//     builder.WithGeneticAlgorithmSpec(problem.CreateGASpec());
//     return builder;
//   }
// }

// public interface IProblemInstanceProvider<out TProblemInstance> {
//   TProblemInstance Load();
// }
