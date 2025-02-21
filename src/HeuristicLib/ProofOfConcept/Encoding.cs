using System.Collections;

namespace HEAL.HeuristicLib.ProofOfConcept;

public interface IEncoding {}

public interface IEncoding<TSolution> : IEncoding {
  ICreator<TSolution> Creator { get; }
  IMutator<TSolution> Mutator { get; }
  ICrossover<TSolution> Crossover { get; }
}

public record PermutationEncodingParameters(int Length);

public class PermutationEncoding : IEncoding<Permutation> {
  public PermutationEncodingParameters Parameters { get; }
  public ICreator<Permutation> Creator { get; }
  public IMutator<Permutation> Mutator { get; }
  public ICrossover<Permutation> Crossover { get; }

  public PermutationEncoding(PermutationEncodingParameters parameters, ICreator<Permutation> creator, IMutator<Permutation> mutator, ICrossover<Permutation> crossover) {
    Parameters = parameters;
    Creator = creator;
    Mutator = mutator;
    Crossover = crossover;
  }
}

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

public class Permutation : IEnumerable<int> {
  private readonly int[] elements;

  public Permutation(IEnumerable<int> elements) {
    this.elements = elements.ToArray();
  }

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public static Permutation CreateRandom(int length) {
    int[] elements = Enumerable.Range(0, length).ToArray();
    var rnd = new Random();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = rnd.Next(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }

  public static Permutation OrderCrossover(Permutation parent1, Permutation parent2) {
    var rnd = new Random();
    int length = parent1.elements.Length;
    int start = rnd.Next(length);
    int end = rnd.Next(start, length);
    var childElements = new int[length];
    Array.Fill(childElements, -1);

    for (int i = start; i <= end; i++) {
      childElements[i] = parent1.elements[i];
    }

    int currentIndex = 0;
    for (int i = 0; i < length; i++) {
      if (!childElements.Contains(parent2.elements[i])) {
        while (childElements[currentIndex] != -1) {
          currentIndex++;
        }
        childElements[currentIndex] = parent2.elements[i];
      }
    }

    return new Permutation(childElements);
  }

  public static Permutation SwapRandomElements(Permutation permutation) {
    var rnd = new Random();
    int length = permutation.elements.Length;
    int index1 = rnd.Next(length);
    int index2 = rnd.Next(length);
    var newElements = (int[])permutation.elements.Clone();
    (newElements[index1], newElements[index2]) = (newElements[index2], newElements[index1]);
    return new Permutation(newElements);
  }
}

public class PermutationCreator : ICreator<Permutation> {
  private readonly PermutationEncodingParameters parameters;

  public PermutationCreator(PermutationEncodingParameters parameters) {
    this.parameters = parameters;
  }

  public Permutation Create() => Permutation.CreateRandom(parameters.Length);
}

public class OrderCrossover : ICrossover<Permutation> {
  private readonly PermutationEncodingParameters parameters;

  public OrderCrossover(PermutationEncodingParameters parameters) {
    this.parameters = parameters;
  }

  public Permutation Crossover(Permutation parent1, Permutation parent2) {
    return Permutation.OrderCrossover(parent1, parent2);
  }
}

public class SwapMutation : IMutator<Permutation> {
  private readonly PermutationEncodingParameters parameters;

  public SwapMutation(PermutationEncodingParameters parameters) {
    this.parameters = parameters;
  }

  public Permutation Mutate(Permutation solution) {
    return Permutation.SwapRandomElements(solution);
  }
}

public class RealVector : IEnumerable<double> {
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
