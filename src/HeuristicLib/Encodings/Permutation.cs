using System.Collections;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;


public class PermutationEncoding : EncodingBase<Permutation>
  //, ICreatorProvider<Permutation>, ICrossoverProvider<Permutation> 
{
  public PermutationEncoding(int length) 
    : base() {
    Length = length;
  }
  
  public int Length { get; }

  public override bool IsValidGenotype(Permutation genotype) {
    return genotype.Count == Length;
  }

  // public ICrossover<Permutation> GetCreator() {
  //   throw new NotImplementedException();
  // }
  // public ICrossover<Permutation> GetCrossover() {
  //   throw new NotImplementedException();
  // }
}





public class Permutation : IReadOnlyList<int>, IEquatable<Permutation> {
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
  public RandomPermutationCreator(int length, RandomSource randomSource) {
    Length = length;
    RandomSource = randomSource;
  }
  public int Length { get; }
  public RandomSource RandomSource { get; }

  public override Permutation Create() {
    var rng = RandomSource.CreateRandomNumberGenerator();
    int[] elements = Enumerable.Range(0, Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = rng.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }

  public record Parameters(int Length, RandomSource RandomSource) : CreatorParameters;
  
  public class Template : CreatorTemplateBase<RandomPermutationCreator, Permutation, Parameters> {
    public override RandomPermutationCreator Parameterize(Parameters parameters) {
      return new RandomPermutationCreator(parameters.Length, parameters.RandomSource);
    }
  }
}


public class OrderCrossover : CrossoverBase<Permutation> {
  public override Permutation Crossover(Permutation parent1, Permutation parent2) {
    return Permutation.OrderCrossover(parent1, parent2);
  }

  public record Parameters() : CrossoverParameters;
  
  public class Template : CrossoverTemplateBase<OrderCrossover, Permutation, Parameters> {
    public override OrderCrossover Parameterize(Parameters parameters) {
      return new OrderCrossover();
    }
  }
}


public class SwapMutator : MutatorBase<Permutation> {
  public override Permutation Mutate(Permutation solution) {
    return Permutation.SwapRandomElements(solution);
  }

  public record Parameters() : MutationParameters;
  
  public class Template : MutatorTemplateBase<SwapMutator, Permutation, Parameters> {
    public override SwapMutator Parameterize(Parameters parameters) {
      return new SwapMutator();
    }
  }
}
