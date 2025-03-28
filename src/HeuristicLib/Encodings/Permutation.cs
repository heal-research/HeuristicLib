using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

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

public class PermutationEncoding<TPhenotype>
  : Encoding<Permutation, TPhenotype>,
    ICreatorProvidingEncoding<Permutation, PermutationEncodingParameter>, ICrossoverProvidingEncoding<Permutation, PermutationEncodingParameter>, IMutatorProvidingEncoding<Permutation, PermutationEncodingParameter>
{
  public required ICreator<Permutation, PermutationEncodingParameter> Creator { get; init; }
  public required ICrossover<Permutation, PermutationEncodingParameter> Crossover { get; init; }
  public required IMutator<Permutation, PermutationEncodingParameter> Mutator { get; init; }
  
  public PermutationEncoding(IDecoder<Permutation, TPhenotype> decoder) 
    : base(decoder) { }
}

// public class PermutationEncoding : PermutationEncoding<Permutation> { // Genotype = Phenotype
//   public PermutationEncoding(PermutationEncodingParameter parameter) : base(Operators.Decoder.Identity<Permutation>()) { }
// }

public sealed class Permutation : IReadOnlyList<int>, IEquatable<Permutation> {
  private readonly int[] elements;

  public Permutation(IEnumerable<int> elements) {
    this.elements = elements.ToArray();
  }
  
  public static implicit operator Permutation(int[] elements) => new(elements);

  public int this[int index] => elements[index];

  public int this[Index index] => elements[index];

  public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)elements).GetEnumerator();

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => elements.GetEnumerator();

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

  public static Permutation Range(int count) {
    return new Permutation(Enumerable.Range(0, count));
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

public class RandomPermutationCreator : CreatorBase<Permutation, PermutationEncodingParameter> {
  public override Permutation Create(PermutationEncodingParameter encoding, IRandomNumberGenerator random) {
    int[] elements = Enumerable.Range(0, encoding.Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = random.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }
}


public class OrderCrossover : CrossoverBase<Permutation, PermutationEncodingParameter> {
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncodingParameter encoding, IRandomNumberGenerator random) {
    return Permutation.OrderCrossover(parent1, parent2, random);
  }

  // public record BreakPoints(int First, int Second) {
  //   public static BreakPoints SingleRandom(int length, IRandomNumberGenerator rng) {
  //     int first = rng.Integer(1, length - 1);
  //     int second = rng.Integer(first + 1, length);
  //     return new BreakPoints(first, second);
  //   }
  //   public static IEnumerable<BreakPoints> MultipleRandom(int length, int count, IRandomNumberGenerator rng) {
  //     int maxPossiblePairs = length * (length - 1) / 2;
  //     count = Math.Min(count, maxPossiblePairs);
  //     var chosenBreakPoints = new HashSet<BreakPoints>();
  //     while (chosenBreakPoints.Count < count) {
  //       var breakPoints = SingleRandom(length, rng);
  //       if (chosenBreakPoints.Add(breakPoints)) {
  //         yield return breakPoints;
  //       }
  //     }
  //   }
  //   public static IEnumerable<BreakPoints> Exhaustive(int length) {
  //     for (int first = 1; first < length - 1; first++) {
  //       for (int second = first + 1; second < length; second++) {
  //         yield return new BreakPoints(first, second);
  //       }
  //     }
  //   }
  // }
}

public class PartiallyMatchedCrossover : CrossoverBase<Permutation, PermutationEncodingParameter> {
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationEncodingParameter encoding, IRandomNumberGenerator random) {
    return Permutation.OrderCrossover(parent1, parent2, random); // implement PMX
  }
}


public class SwapMutator : MutatorBase<Permutation, PermutationEncodingParameter> {
  public override Permutation Mutate(Permutation solution, PermutationEncodingParameter encoding, IRandomNumberGenerator random) {
    return Permutation.SwapRandomElements(solution, random);
  }
}

public class InversionMutator : MutatorBase<Permutation, PermutationEncodingParameter> {
  public override Permutation Mutate(Permutation parent, PermutationEncodingParameter encoding, IRandomNumberGenerator random) {
    int start = random.Integer(parent.Count);
    int end = random.Integer(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}

// // ToDo: move to different file
// public static class GeneticAlgorithmBuilderPermutationEncodingExtensions {
//   // For type inference
//   public static GeneticAlgorithmBuilder<Permutation, PermutationEncodingParameter> UsingEncoding<TPhenotype>(this GeneticAlgorithmBuilder<Permutation, TPhenotype> builder, PermutationEncoding<TPhenotype> encoding) {
//     return builder.UsingEncoding<Permutation, PermutationEncodingParameter, PermutationEncoding>(encoding);
//   }
// }
