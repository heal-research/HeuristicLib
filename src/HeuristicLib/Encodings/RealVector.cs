using System.Collections;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;



//public record RealVectorEncodingDescriptor(int Length, RealVector Minimum, RealVector Maximum);

public class RealVectorEncoding : EncodingBase<RealVector> {
  public int Length { get; }
  public RealVector Minimum { get; }
  public RealVector Maximum { get; }


  public RealVectorEncoding(int length, RealVector minimum, RealVector maximum) {
    if (length != minimum.Count && minimum.Count != 1) throw new ArgumentException("Minimum vector must be of length 1 or match the encoding length");
    if (length != maximum.Count && maximum.Count != 1) throw new ArgumentException("Maximum vector must be of length 1 or match the encoding length");
    
    Length = length;
    Minimum = minimum;
    Maximum = maximum;
  }

  public override bool IsValidGenotype(RealVector genotype) {
    return genotype.Count == Length
           && (genotype >= Minimum).All()
           && (genotype <= Maximum).All();
  }
}

public class AlphaBetaBlendCrossover : ICrossover<RealVector> {
  private readonly double alpha;
  private readonly double beta;

  public AlphaBetaBlendCrossover(double alpha = 0.7, double beta = 0.3) {
    this.alpha = alpha;
    this.beta = beta;
  }

  public RealVector Crossover(RealVector parent1, RealVector parent2) {
    return alpha * parent1 + beta * parent2;
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

  
  public static bool AreCompatible(RealVector a, RealVector b) {
    return a.Count == b.Count || a.Count == 1 || b.Count == 1;
  }
  public static bool AreCompatible(RealVector vector, params IEnumerable<RealVector> others) {
    return others.All(v => AreCompatible(vector, v));
  }
  public static bool AreCompatible(int length, params IEnumerable<RealVector> vectors) {
    return vectors.All(v => v.Count == length || v.Count == 1);
  }
  
  public static int BroadcastLength(RealVector a, RealVector b) {
    return Math.Max(a.Count, b.Count);
  }
  public static int BroadcastLength(RealVector vector, IEnumerable<RealVector> others) {
    if (!AreCompatible(vector, others)) throw new ArgumentException("Vectors must be compatible for broadcasting");
    return others.Max(v => v.Count);
  }
  
  public static RealVector CreateNormal(RealVector mean, RealVector std, IRandomNumberGenerator random) {
    int targetLength = BroadcastLength(mean, std);
    
    // Box-Muller transform to generate normal distributed random values
    RealVector u1 = 1.0 - new RealVector(random.Random(targetLength));
    RealVector u2 = 1.0 - new RealVector(random.Random(targetLength));
    RealVector randStdNormal = Sqrt(u1 * 2) * Sin(2 * Math.PI * u2);
    
    // Apply mean and sigma for this dimension
    RealVector value = mean + std * randStdNormal;

    return value;
  }

  public static RealVector CreateUniform(RealVector low, RealVector high, IRandomNumberGenerator random) {
    int targetLength = BroadcastLength(low, high);
    
    RealVector value = new RealVector(random.Random(targetLength));
    value = low + (high - low) * value;
    return value;
  }

  public static RealVector Sqrt(RealVector vector) {
    return new RealVector(vector.Select(Math.Sqrt));
  }

  public static RealVector Sin(RealVector vector) {
    return new RealVector(vector.Select(Math.Sin));
  }
  
  public static RealVector Clamp(RealVector input, RealVector? min, RealVector? max) {
    if (min == null && max == null) return input; // No clamping needed
    
    // Validate lengths
    if (min != null && min.Count != 1 && min.Count != input.Count)
        throw new ArgumentException($"Min vector must be of length 1 or match input length ({input.Count})");
    
    if (max != null && max.Count != 1 && max.Count != input.Count)
        throw new ArgumentException($"Max vector must be of length 1 or match input length ({input.Count})");
    
    double[] result = new double[input.Count];
    
    for (int i = 0; i < input.Count; i++) {
        double value = input[i];
        
        // Apply lower bound if present
        if (min != null) {
            double minValue = min.Count == 1 ? min[0] : min[i];
            value = Math.Max(value, minValue);
        }
        
        // Apply upper bound if present
        if (max != null) {
            double maxValue = max.Count == 1 ? max[0] : max[i];
            value = Math.Min(value, maxValue);
        }
        
        result[i] = value;
    }
    
    return new RealVector(result);
  }
  
  public static BoolVector operator >(RealVector a, RealVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for comparison");
    
    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];
    
    for (int i = 0; i < length; i++) {
      double aValue = a.Count == 1 ? a[0] : a[i];
      double bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue > bValue;
    }
    
    return new BoolVector(result);
  }
  
  public static BoolVector operator <(RealVector a, RealVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for comparison");
    
    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];
    
    for (int i = 0; i < length; i++) {
      double aValue = a.Count == 1 ? a[0] : a[i];
      double bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue < bValue;
    }
    
    return new BoolVector(result);
  }
  
  public static BoolVector operator >=(RealVector a, RealVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for comparison");
    
    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];
    
    for (int i = 0; i < length; i++) {
      double aValue = a.Count == 1 ? a[0] : a[i];
      double bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue >= bValue;
    }
    
    return new BoolVector(result);
  }
  
  public static BoolVector operator <=(RealVector a, RealVector b) {
    if (!AreCompatible(a, b)) throw new ArgumentException("Vectors must be compatible for comparison");
    
    int length = BroadcastLength(a, b);
    bool[] result = new bool[length];
    
    for (int i = 0; i < length; i++) {
      double aValue = a.Count == 1 ? a[0] : a[i];
      double bValue = b.Count == 1 ? b[0] : b[i];
      result[i] = aValue <= bValue;
    }
    
    return new BoolVector(result);
  }
}

public class GaussianMutation : IMutator<RealVector> {
  public double MutationRate { get; }
  public double MutationStrength { get; }
  public IRandomNumberGenerator Random { get; }

  public GaussianMutation(double mutationRate, double mutationStrength, IRandomNumberGenerator random) {
    this.MutationRate = mutationRate;
    this.MutationStrength = mutationStrength;
    this.Random = random;
  }

  public RealVector Mutate(RealVector solution) {
    double[] newElements = solution.ToArray();
    for (int i = 0; i < newElements.Length; i++) {
      if (Random.Random() < MutationRate) {
        newElements[i] += MutationStrength * (Random.Random() - 0.5);
      }
    }
    return new RealVector(newElements);
  }
  
  public record Parameter(double MutationRate, double MutationStrength);
}


public class SinglePointCrossover : ICrossover<RealVector>
{
  public SinglePointCrossover(IRandomNumberGenerator random) {
    Random = random;
  }
  public IRandomNumberGenerator Random { get; }

  public RealVector Crossover(RealVector parent1, RealVector parent2)
  {
    int crossoverPoint = Random.Integer(1, parent1.Count);
    double[] offspringValues = new double[parent1.Count];
    for (int i = 0; i < crossoverPoint; i++) {
      offspringValues[i] = parent1[i];
    }
    for (int i = crossoverPoint; i < parent2.Count; i++) {
      offspringValues[i] = parent2[i];
    }
    return new RealVector(offspringValues);
  }
  
  public record Parameter();
}

public class GaussianMutator : IMutator<RealVector>
{
  public double MutationRate { get; }
  public double MutationStrength { get; }
  public IRandomNumberGenerator Random { get; }

  public GaussianMutator(double mutationRate, double mutationStrength, IRandomNumberGenerator random)
  {
    MutationRate = mutationRate;
    MutationStrength = mutationStrength;
    Random = random;
  }

  public RealVector Mutate(RealVector individual) {
    var mutatedValues = individual.ToList();

    for (int i = 0; i < mutatedValues.Count; i++)
    {
      if (Random.Random() < MutationRate)
      {
        mutatedValues[i] += Random.Random() * MutationStrength * 2 - MutationStrength;
      }
    }
    return new RealVector(mutatedValues);
  }
  
  public record Parameter(double MutationRate, double MutationStrength);
}

public class NormalDistributedCreator : ICreator<RealVector>
{
  public int Length { get; }
  public RealVector Means { get; }
  public RealVector Sigmas { get; }
  public RealVector Minimum { get; }
  public RealVector Maximum { get; }
  public IRandomNumberGenerator Random { get; }

  public NormalDistributedCreator(int length, RealVector means, RealVector sigmas, RealVector minimum, RealVector maximum, IRandomNumberGenerator random) {
    if (!RealVector.AreCompatible(length, means, sigmas, minimum, maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    
    Length = length;
    Means = means;
    Sigmas = sigmas;
    Minimum = minimum;
    Maximum = maximum;
    Random = random;
  }

  public RealVector Create() {
    RealVector value = RealVector.CreateNormal(Means, Sigmas, Random);
    // Clamp value to min/max bounds
    value = RealVector.Clamp(value, Minimum, Maximum);
    return value;
  }
}

public record NormalDistributedCreatorParameters(int Length, RealVector Means, RealVector Sigmas, RealVector Minimum, RealVector Maximum, IRandomNumberGenerator Random)
  : CreatorParameters;

public class NormalDistributedCreatorProvider : ICreatorProvider<RealVector, NormalDistributedCreatorParameters> {
  public ICreator<RealVector> Create(NormalDistributedCreatorParameters parameters) {
    return new NormalDistributedCreator(parameters.Length, parameters.Means, parameters.Sigmas, parameters.Minimum, parameters.Maximum, parameters.Random);
  }
}

public class UniformDistributedCreator : ICreator<RealVector>
{
  public int Length { get; }
  public RealVector Minimum { get; }
  public RealVector Maximum { get; }
  public IRandomNumberGenerator Random { get; }

  public UniformDistributedCreator(int length, RealVector minimum, RealVector maximum, IRandomNumberGenerator random) {
    if (!RealVector.AreCompatible(length, minimum, maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    
    Length = length;
    Minimum = minimum;
    Maximum = maximum;
    Random = random;
  }

  public RealVector Create()
  {
    return RealVector.CreateUniform(Minimum, Minimum, Random);
  }
  
  public static UniformDistributedCreator FromEncoding(RealVectorEncoding encoding, IRandomNumberGenerator random) {
    return new UniformDistributedCreator(encoding.Length, encoding.Minimum, encoding.Maximum, random);
  }
}

public record UniformDistributedCreatorParameters(int Length, RealVector Minimum, RealVector Maximum, IRandomNumberGenerator Random) : CreatorParameters;

public class UniformDistributedCreatorProvider : ICreatorProvider<RealVector, UniformDistributedCreatorParameters> {
  public ICreator<RealVector> Create(UniformDistributedCreatorParameters p) {
    var parameters = (UniformDistributedCreatorParameters)p;
    return new UniformDistributedCreator(parameters.Length, parameters.Minimum, parameters.Maximum, parameters.Random);
  }
}
