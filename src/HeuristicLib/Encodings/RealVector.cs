using System.Collections;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;



public record RealVectorEncodingParameters(int Length, double Min, double Max);

public class RealVectorEncoding : IEncoding<RealVector> {
  public RealVectorEncodingParameters Parameters { get; }
  public ICreator<RealVector> Creator { get; }
  public IMutator<RealVector> Mutator { get; }
  public ICrossover<RealVector> Crossover { get; }

  public RealVectorEncoding(RealVectorEncodingParameters parameters, ICreator<RealVector> creator, IMutator<RealVector> mutator, ICrossover<RealVector> crossover) {
    Parameters = parameters;
    Creator = creator;
    Mutator = mutator;
    Crossover = crossover;
  }
}

public class AlphaBetaBlendCrossover : ICrossover<RealVector> {
  private readonly double alpha;
  private readonly double beta;

  public AlphaBetaBlendCrossover(double alpha, double beta) {
    this.alpha = alpha;
    this.beta = beta;
  }

  public RealVector Crossover(RealVector parent1, RealVector parent2) {
    double[] newElements = new double[parent1.Count()];
    for (int i = 0; i < newElements.Length; i++) {
      newElements[i] = alpha * parent1[i] + beta * parent2[i];
    }
    return new RealVector(newElements);
  }
}


public class RealVector : IReadOnlyList<double> {
  private readonly double[] elements;

  public RealVector(IEnumerable<double> elements) {
    this.elements = elements.ToArray();
  }

  public RealVector(double value) {
    elements = new[] { value };
  }

  public static implicit operator RealVector(double value) => new RealVector(value);

  public double this[int index] => elements[index];

  public double this[Index index] => elements[index];

  public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public static RealVector Add(RealVector a, RealVector b) {
    if (a.elements.Length != b.elements.Length) {
      if (a.elements.Length == 1) {
        return new RealVector(b.elements.Select(x => x + a.elements[0]));
      }
      if (b.elements.Length == 1) {
        return new RealVector(a.elements.Select(x => x + b.elements[0]));
      }
      throw new ArgumentException("Vectors must be of the same length or one of length one");
    }
    var result = new double[a.elements.Length];
    for (int i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] + b.elements[i];
    }
    return new RealVector(result);
  }

  public static RealVector Subtract(RealVector a, RealVector b) {
    if (a.elements.Length != b.elements.Length) {
      if (a.elements.Length == 1) {
        return new RealVector(b.elements.Select(x => x - a.elements[0]));
      }
      if (b.elements.Length == 1) {
        return new RealVector(a.elements.Select(x => x - b.elements[0]));
      }
      throw new ArgumentException("Vectors must be of the same length or one of length one");
    }
    var result = new double[a.elements.Length];
    for (int i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] - b.elements[i];
    }
    return new RealVector(result);
  }

  public static RealVector Multiply(RealVector a, RealVector b) {
    if (a.elements.Length != b.elements.Length) {
      if (a.elements.Length == 1) {
        return new RealVector(b.elements.Select(x => x * a.elements[0]));
      }
      if (b.elements.Length == 1) {
        return new RealVector(a.elements.Select(x => x * b.elements[0]));
      }
      throw new ArgumentException("Vectors must be of the same length or one of length one");
    }
    var result = new double[a.elements.Length];
    for (int i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] * b.elements[i];
    }
    return new RealVector(result);
  }

  public static RealVector Divide(RealVector a, RealVector b) {
    if (a.elements.Length != b.elements.Length) {
      if (a.elements.Length == 1) {
        return new RealVector(b.elements.Select(x => x / a.elements[0]));
      }
      if (b.elements.Length == 1) {
        return new RealVector(a.elements.Select(x => x / b.elements[0]));
      }
      throw new ArgumentException("Vectors must be of the same length or one of length one");
    }
    var result = new double[a.elements.Length];
    for (int i = 0; i < a.elements.Length; i++) {
      result[i] = a.elements[i] / b.elements[i];
    }
    return new RealVector(result);
  }

  public static RealVector operator +(RealVector a, RealVector b) => Add(a, b);
  public static RealVector operator -(RealVector a, RealVector b) => Subtract(a, b);
  public static RealVector operator *(RealVector a, RealVector b) => Multiply(a, b);
  public static RealVector operator /(RealVector a, RealVector b) => Divide(a, b);

  public int Count => elements.Length;

  public bool Contains(double value) => elements.Contains(value);
}

public class RealVectorCreator : ICreator<RealVector> {
  private readonly RealVectorEncodingParameters parameters;

  public RealVectorCreator(RealVectorEncodingParameters parameters) {
    this.parameters = parameters;
  }

  public RealVector Create() {
    var rnd = new Random();
    return new RealVector(Enumerable.Range(0, parameters.Length)
      .Select(_ => parameters.Min + rnd.NextDouble() * (parameters.Max - parameters.Min))
    );
  }
}

public class GaussianMutation : IMutator<RealVector> {
  private readonly double mutationRate;
  private readonly double mutationStrength;

  public GaussianMutation(double mutationRate, double mutationStrength) {
    this.mutationRate = mutationRate;
    this.mutationStrength = mutationStrength;
  }

  public RealVector Mutate(RealVector solution) {
    var rnd = new Random();
    var newElements = solution.ToArray();
    for (int i = 0; i < newElements.Length; i++) {
      if (rnd.NextDouble() < mutationRate) {
        newElements[i] += mutationStrength * (rnd.NextDouble() - 0.5);
      }
    }
    return new RealVector(newElements);
  }
}


public class SinglePointCrossover : ICrossover<RealVector>
{
  private readonly Random random = new();

  public RealVector Crossover(RealVector parent1, RealVector parent2)
  {
    int crossoverPoint = random.Next(1, parent1.Count);
    var offspringValues = new double[parent1.Count];
    for (int i = 0; i < crossoverPoint; i++) {
      offspringValues[i] = parent1[i];
    }
    for (int i = crossoverPoint; i < parent2.Count; i++) {
      offspringValues[i] = parent2[i];
    }
    return new RealVector(offspringValues);
  }
}

public class GaussianMutator : IMutator<RealVector>
{
  private readonly Random random = new();
  private readonly double mutationRate;
  private readonly double mutationStrength;

  public GaussianMutator(double mutationRate, double mutationStrength)
  {
    this.mutationRate = mutationRate;
    this.mutationStrength = mutationStrength;
  }

  public RealVector Mutate(RealVector individual) {
    var mutatedValues = individual.ToList();

    for (int i = 0; i < mutatedValues.Count; i++)
    {
      if (random.NextDouble() < mutationRate)
      {
        mutatedValues[i] += random.NextDouble() * mutationStrength * 2 - mutationStrength;
      }
    }
    return new RealVector(mutatedValues);
  }
}

public class RandomCreator : ICreator<RealVector>
{
  private readonly Random random = new();
  private readonly int chromosomeLength;
  private readonly double minValue;
  private readonly double maxValue;

  public RandomCreator(int chromosomeLength, double minValue, double maxValue)
  {
    this.chromosomeLength = chromosomeLength;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }

  public RealVector Create()
  {
    var values = new double[chromosomeLength];
    for (int i = 0; i < chromosomeLength; i++)
    {
      values[i] = random.NextDouble() * (maxValue - minValue) + minValue;
    }
    return new RealVector(values);
  }
}
