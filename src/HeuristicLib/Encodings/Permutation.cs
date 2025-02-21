using System.Collections;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Encodings;



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


public class Permutation : IReadOnlyList<int> {
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
