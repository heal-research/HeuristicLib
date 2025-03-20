using System.Collections;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;

public record class PermutationEncodingParameter : EncodingParameterBase<Permutation> {
  public int Length { get; }
  
  public PermutationEncodingParameter(int length) {
    Length = length;
  }

  public override bool IsValidGenotype(Permutation genotype) {
    return genotype.Count == Length;
  }
}

public class PermutationEncoding 
  : Encoding<Permutation, PermutationEncodingParameter>,
    ICreatorProvider<Permutation>, ICrossoverProvider<Permutation>, IMutatorProvider<Permutation>
{
  public required ICreator<Permutation> Creator { get; init; }
  public required ICrossover<Permutation> Crossover { get; init; }
  public required IMutator<Permutation> Mutator { get; init; }
  
  public PermutationEncoding(PermutationEncodingParameter parameter) 
    : base(parameter) { }
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
  public PermutationEncodingParameter EncodingParameter { get; }
  
  public RandomPermutationCreator(PermutationEncodingParameter encodingParameter) {
    EncodingParameter = encodingParameter;
  }
  
  public override Permutation Create(IRandomNumberGenerator random) {
    int[] elements = Enumerable.Range(0, EncodingParameter.Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = random.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }
}


public class OrderCrossover : CrossoverBase<Permutation> {
  public override Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random) {
    return Permutation.OrderCrossover(parent1, parent2, random);
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
}

public class PartiallyMatchedCrossover : CrossoverBase<Permutation> {
  public override Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator random) {
    return Permutation.OrderCrossover(parent1, parent2, random); // implement PMX
  }
}


public class SwapMutator : MutatorBase<Permutation> {
  public PermutationEncodingParameter EncodingParameter { get; }
  
  public SwapMutator(PermutationEncodingParameter encodingParameter) {
    EncodingParameter = encodingParameter;
  }
  
  public override Permutation Mutate(Permutation solution, IRandomNumberGenerator random) {
    return Permutation.SwapRandomElements(solution, random);
  }
}

public class InversionMutator : MutatorBase<Permutation> {
  public override Permutation Mutate(Permutation parent, IRandomNumberGenerator random) {
    int start = random.Integer(parent.Count);
    int end = random.Integer(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}
