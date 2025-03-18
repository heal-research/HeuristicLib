using System.Collections;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public sealed record class PermutationEncoding : EncodingBase<Permutation, PermutationEncoding> {
  public int Length { get; }
  
  public PermutationEncoding(int length) {
    Length = length;
  }

  public override bool IsValidGenotype(Permutation genotype) {
    return genotype.Count == Length;
  }
}

public sealed class Permutation : IReadOnlyList<int>, IEquatable<Permutation> {
  private readonly int[] elements;

  public Permutation(IEnumerable<int> elements) {
    this.elements = elements.ToArray();
  }

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => elements.GetEnumerator();

  public static Permutation CreateRandom(int length, IRandomNumberGenerator rng) {
    int[] elements = Enumerable.Range(0, length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = rng.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }

  public static Permutation OrderCrossover(Permutation parent1, Permutation parent2, IRandomNumberGenerator rng) {
    int length = parent1.elements.Length;
    int start = rng.Integer(length);
    int end = rng.Integer(start, length);
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

  public static Permutation SwapRandomElements(Permutation permutation, IRandomNumberGenerator rng) {
    int length = permutation.elements.Length;
    int index1 = rng.Integer(length);
    int index2 = rng.Integer(length);
    var newElements = (int[])permutation.elements.Clone();
    (newElements[index1], newElements[index2]) = (newElements[index2], newElements[index1]);
    return new Permutation(newElements);
  }

  public int Count => elements.Length;

  public bool Contains(int value) => elements.Contains(value);

  public bool Equals(Permutation? other) {
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    if (Count != other.Count) return false;
    return elements.SequenceEqual(other.elements);
  }

  public override bool Equals(object? obj) {
    if (obj is null) return false;
    if (ReferenceEquals(this, obj)) return true;
    return obj is Permutation other && Equals(other);
  }

  public override int GetHashCode() {
    return elements.Aggregate(17, (current, item) => current * 23 + item.GetHashCode());
  }

  public static bool operator ==(Permutation? left, Permutation? right) {
    if (left is null) return right is null;
    return left.Equals(right);
  }

  public static bool operator !=(Permutation? left, Permutation? right) {
    return !(left == right);
  }
}

public class RandomPermutationCreator : CreatorBase<Permutation> {
  public PermutationEncoding Encoding { get; }
  public IRandomSource RandomSource { get; }
  
  public RandomPermutationCreator(PermutationEncoding encoding, IRandomSource randomSource) {
    Encoding = encoding;
    RandomSource = randomSource;
  }
  
  public override Permutation Create() {
    var rng = RandomSource.CreateRandomNumberGenerator();
    int[] elements = Enumerable.Range(0, Encoding.Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = rng.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }
  
  public class Factory : IOperatorFactory<RandomPermutationCreator>, IEncodingDependentOperatorFactory<PermutationEncoding>, IStochasticOperatorFactory {
    private PermutationEncoding? encoding;
    private IRandomSource? randomSource;
  
    public void SetEncoding(PermutationEncoding encoding) => this.encoding = encoding;
    public void SetRandom(IRandomSource randomSource) => this.randomSource = randomSource;

    public RandomPermutationCreator Create() {
      if (encoding is null) throw new InvalidOperationException("Encoding must be set.");
      if (randomSource is null) throw new InvalidOperationException("Random source must be set.");
      return new RandomPermutationCreator(encoding, randomSource);
    }
  }
}


public class OrderCrossover : CrossoverBase<Permutation> {
  public IRandomSource RandomSource { get; }

  public OrderCrossover(IRandomSource randomSource) {
    RandomSource = randomSource;
  }
  
  public override Permutation Cross(Permutation parent1, Permutation parent2) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    return Permutation.OrderCrossover(parent1, parent2, rng);
  }

  public record BreakPoints(int First, int Second) {
    public static BreakPoints SingleRandom(int length, IRandomNumberGenerator rng) {
      int first = rng.Integer(1, length - 1);
      int second = rng.Integer(first + 1, length);
      return new BreakPoints(first, second);
    }
    public static IEnumerable<BreakPoints> MultipleRandom(int length, int count, IRandomNumberGenerator rng) {
      int maxPossiblePairs = length * (length - 1) / 2;
      count = Math.Min(count, maxPossiblePairs);
      var chosenBreakPoints = new HashSet<BreakPoints>();
      while (chosenBreakPoints.Count < count) {
        var breakPoints = SingleRandom(length, rng);
        if (chosenBreakPoints.Add(breakPoints)) {
          yield return breakPoints;
        }
      }
    }
    public static IEnumerable<BreakPoints> Exhaustive(int length) {
      for (int first = 1; first < length - 1; first++) {
        for (int second = first + 1; second < length; second++) {
          yield return new BreakPoints(first, second);
        }
      }
    }
  }
  
  public class Factory : IOperatorFactory<OrderCrossover>, IStochasticOperatorFactory {
    private IRandomSource? randomSource;
    
    public void SetRandom(IRandomSource randomSource) => this.randomSource = randomSource;
    
    public OrderCrossover Create() {
      if (randomSource is null) throw new InvalidOperationException("Random source must be set.");
      return new OrderCrossover(randomSource);
    }
  }
}

public class SwapMutator : MutatorBase<Permutation> {
  public PermutationEncoding Encoding { get; }
  public IRandomSource RandomSource { get; }
  
  public SwapMutator(PermutationEncoding encoding, IRandomSource randomSource) {
    Encoding = encoding;
    RandomSource = randomSource;
  }
  
  public override Permutation Mutate(Permutation solution) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    return Permutation.SwapRandomElements(solution, rng);
  }
  
  public class Factory : IOperatorFactory<SwapMutator>, IEncodingDependentOperatorFactory<PermutationEncoding>, IStochasticOperatorFactory {
    private PermutationEncoding? encoding;
    private IRandomSource? randomSource;
    
    public void SetEncoding(PermutationEncoding encoding) => this.encoding = encoding;
    public void SetRandom(IRandomSource randomSource) => this.randomSource = randomSource;
    
    public SwapMutator Create() {
      if (encoding is null) throw new InvalidOperationException("Encoding must be set.");
      if (randomSource is null) throw new InvalidOperationException("Random source must be set.");
      return new SwapMutator(encoding, randomSource);
    }
  }
}

public class InversionMutator : MutatorBase<Permutation> {
  public PermutationEncoding Encoding { get; }
  public IRandomSource RandomSource { get; }
  
  public InversionMutator(PermutationEncoding encoding, IRandomSource randomSource) {
    Encoding = encoding;
    RandomSource = randomSource;
  }
  
  public override Permutation Mutate(Permutation parent) {
    var rng = RandomSource.CreateRandomNumberGenerator();
    int start = rng.Integer(parent.Count);
    int end = rng.Integer(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
  
  public class Factory : IOperatorFactory<InversionMutator>, IEncodingDependentOperatorFactory<PermutationEncoding>, IStochasticOperatorFactory {
    private PermutationEncoding? encoding;
    private IRandomSource? randomSource;
    
    public void SetEncoding(PermutationEncoding encoding) => this.encoding = encoding;
    public void SetRandom(IRandomSource randomSource) => this.randomSource = randomSource;
    
    public InversionMutator Create() {
      if (encoding is null) throw new InvalidOperationException("Encoding must be set.");
      if (randomSource is null) throw new InvalidOperationException("Random source must be set.");
      return new InversionMutator(encoding, randomSource);
    }
  }
}
