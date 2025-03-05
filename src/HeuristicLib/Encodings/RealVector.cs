namespace HEAL.HeuristicLib.Encodings;

using System.Collections;
using Algorithms;
using Operators;

public record class RealVectorEncoding : EncodingBase<RealVector, RealVectorEncoding> {
  public int Length { get; }
  public RealVector Minimum { get; }
  public RealVector Maximum { get; }

  public RealVectorEncoding(int length, RealVector minimum, RealVector maximum) {
    if (!RealVector.AreCompatible(length, minimum, maximum)) throw new ArgumentException("Minimum and Maximum vector must be of length 1 or match the encoding length");
    Length = length;
    Minimum = minimum;
    Maximum = maximum;
  }
  
  public override bool IsValidGenotype(RealVector genotype) {
    return genotype.Count == Length
           && (genotype >= Minimum).All()
           && (genotype <= Maximum).All();
  }

  // public override bool Equals(IEncoding<RealVector>? other) {
  //   if (other is null) return false;
  //   if (ReferenceEquals(this, other)) return true;
  //   if (other is RealVectorEncoding otherRealVectorEncoding) {
  //     return Length == otherRealVectorEncoding.Length 
  //            && Minimum == otherRealVectorEncoding.Minimum
  //            && Maximum == otherRealVectorEncoding.Maximum;
  //   }
  //   return false;
  // }
}

public class AlphaBetaBlendCrossover : CrossoverBase<RealVector>, IEncodingOperator<RealVector, RealVectorEncoding> {
  public RealVectorEncoding Encoding { get; }
  public double Alpha { get; }
  public double Beta { get; }

  public AlphaBetaBlendCrossover(RealVectorEncoding encoding, double alpha = 0.7, double beta = 0.3) {
    Encoding = encoding;
    Alpha = alpha;
    Beta = beta;
  }

  public override RealVector Crossover(RealVector parent1, RealVector parent2) {
    return Alpha * parent1 + Beta * parent2;
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
  
  public static RealVector CreateNormal(RealVector mean, RealVector std, RandomSource randomSource) {
    int targetLength = BroadcastLength(mean, std);
    var rng = randomSource.CreateRandomNumberGenerator();
    
    // Box-Muller transform to generate normal distributed random values
    RealVector u1 = 1.0 - new RealVector(rng.Random(targetLength));
    RealVector u2 = 1.0 - new RealVector(rng.Random(targetLength));
    RealVector randStdNormal = Sqrt(u1 * 2) * Sin(2 * Math.PI * u2);
    
    // Apply mean and sigma for this dimension
    RealVector value = mean + std * randStdNormal;

    return value;
  }

  public static RealVector CreateUniform(RealVector low, RealVector high, RandomSource randomSource) {
    int targetLength = BroadcastLength(low, high);
    var rng = randomSource.CreateRandomNumberGenerator();
    
    RealVector value = new RealVector(rng.Random(targetLength));
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

public class GaussianMutator : MutatorBase<RealVector>, IEncodingOperator<RealVector, RealVectorEncoding> {
  public RealVectorEncoding Encoding { get; }
  public double MutationRate { get; }
  public double MutationStrength { get; }
  public RandomSource RandomSource { get; }

  public GaussianMutator(RealVectorEncoding encoding, double mutationRate, double mutationStrength, RandomSource randomSource) {
    this.Encoding = encoding;
    this.MutationRate = mutationRate;
    this.MutationStrength = mutationStrength;
    this.RandomSource = randomSource;
  }

  public override RealVector Mutate(RealVector solution) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    double[] newElements = solution.ToArray();
    for (int i = 0; i < newElements.Length; i++) {
      if (rng.Random() < MutationRate) {
        newElements[i] += MutationStrength * (rng.Random() - 0.5);
      }
    }
    return new RealVector(newElements);
  }
}


public class SinglePointCrossover : CrossoverBase<RealVector>, IEncodingOperator<RealVector, RealVectorEncoding> {
  public RealVectorEncoding Encoding { get; }
  public RandomSource RandomSource { get; }
  public SinglePointCrossover(RealVectorEncoding encoding, RandomSource randomSource) {
    this.Encoding = encoding;
    this.RandomSource = randomSource;
  }
  
  public override RealVector Crossover(RealVector parent1, RealVector parent2) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    int crossoverPoint = rng.Integer(1, parent1.Count);
    double[] offspringValues = new double[parent1.Count];
    for (int i = 0; i < crossoverPoint; i++) {
      offspringValues[i] = parent1[i];
    }
    for (int i = crossoverPoint; i < parent2.Count; i++) {
      offspringValues[i] = parent2[i];
    }
    return new RealVector(offspringValues);
  }
}

public class NormalDistributedCreator : CreatorBase<RealVector>, IEncodingOperator<RealVector, RealVectorEncoding> {
  public RealVectorEncoding Encoding { get; }
  public RealVector Means { get; }
  public RealVector Sigmas { get; }
  public RandomSource RandomSource { get; }

  public NormalDistributedCreator(RealVectorEncoding encoding, RealVector means, RealVector sigmas, RandomSource randomSource) {
    if (!RealVector.AreCompatible(encoding.Length, means, sigmas, encoding.Minimum, encoding.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    Encoding = encoding;
    Means = means;
    Sigmas = sigmas;
    RandomSource = randomSource;
  }

  public override RealVector Create() {
    RealVector value = RealVector.CreateNormal(Means, Sigmas, RandomSource);
    // Clamp value to min/max bounds
    value = RealVector.Clamp(value, Encoding.Minimum, Encoding.Maximum);
    return value;
  }
}

public class UniformDistributedCreator : CreatorBase<RealVector>, IEncodingOperator<RealVector, RealVectorEncoding> {
  public RealVectorEncoding Encoding { get; }
  public RealVector? Minimum { get; }
  public RealVector? Maximum { get; }
  public RandomSource RandomSource { get; }

  public UniformDistributedCreator(RealVectorEncoding encoding, RealVector? minimum, RealVector? maximum, RandomSource randomSource) {
    if (minimum is not null && (minimum < encoding.Minimum).Any()) throw new ArgumentException("Minimum values must be greater or equal to encoding minimum values");
    if (maximum is not null && (maximum > encoding.Maximum).Any()) throw new ArgumentException("Maximum values must be less or equal to encoding maximum values");
    if (!RealVector.AreCompatible(encoding.Length, minimum ?? encoding.Minimum, maximum ?? encoding.Maximum)) throw new ArgumentException("Vectors must have compatible lengths");
    
    Encoding = encoding;
    Minimum = minimum;
    Maximum = maximum;
    RandomSource = randomSource;
  }

  public override RealVector Create() {
    return RealVector.CreateUniform(Minimum ?? Encoding.Minimum, Maximum ?? Encoding.Minimum, RandomSource);
  }
}

