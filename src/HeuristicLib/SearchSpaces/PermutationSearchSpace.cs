using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces;

public record class PermutationSearchSpace : SearchSpace<Permutation>, ISubspaceComparable<PermutationSearchSpace> {
  public int Length { get; }
  
  public PermutationSearchSpace(int length) {
    Length = length;
  }

  public override bool Contains(Permutation genotype) {
    return genotype.Count == Length;
  }
  
  public virtual bool IsSubspaceOf(PermutationSearchSpace other) {
    return other.Length == Length;
  }

  public static implicit operator IntegerVectorSearchSpace(PermutationSearchSpace permutationSpace) {
    return new IntegerVectorSearchSpace(permutationSpace.Length, 0, permutationSpace.Length);
  }
}

// public class PermutationSearchSpace<TPhenotype>
//   : SearchSpace<Permutation, TPhenotype>,
//     ICreatorProvidingSearchSpace<Permutation, PermutationSearchSpace>, ICrossoverProvidingSearchSpace<Permutation, PermutationSearchSpace>, IMutatorProvidingSearchSpace<Permutation, PermutationSearchSpace>
// {
//   public required ICreator<Permutation, PermutationSearchSpace> Creator { get; init; }
//   public required ICrossover<Permutation, PermutationSearchSpace> Crossover { get; init; }
//   public required IMutator<Permutation, PermutationSearchSpace> Mutator { get; init; }
//   
//   public PermutationSearchSpace(IDecoder<Permutation, TPhenotype> decoder) 
//     : base(decoder) { }
// }

// public class PermutationSearchSpace : PermutationSearchSpace<Permutation> { // Genotype = Phenotype
//   public PermutationSearchSpace(PermutationSearchSpace parameter) : base(Operators.Decoder.Identity<Permutation>()) { }
// }



public record class RandomPermutationCreator : Creator<Permutation, PermutationSearchSpace> {
  public override RandomPermutationCreatorInstance CreateInstance() => new RandomPermutationCreatorInstance(this);
}

public class RandomPermutationCreatorInstance : CreatorInstance<Permutation, PermutationSearchSpace, RandomPermutationCreator> {
  public RandomPermutationCreatorInstance(RandomPermutationCreator parameters) : base(parameters) {}
  public override Permutation Create(PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    int[] elements = Enumerable.Range(0, searchSpace.Length).ToArray();
    for (int i = elements.Length - 1; i > 0; i--) {
      int j = random.Integer(i + 1);
      (elements[i], elements[j]) = (elements[j], elements[i]);
    }
    return new Permutation(elements);
  }
}


public record class OrderCrossover : Crossover<Permutation, PermutationSearchSpace> {
  public override OrderCrossoverInstance CreateInstance() => new OrderCrossoverInstance(this);
}

public class OrderCrossoverInstance : CrossoverInstance<Permutation, PermutationSearchSpace, OrderCrossover> {
  public OrderCrossoverInstance(OrderCrossover parameters) : base(parameters) { }
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
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

public record class PartiallyMatchedCrossover : Crossover<Permutation, PermutationSearchSpace> {
  public override PartiallyMatchedCrossoverInstance CreateInstance() => new PartiallyMatchedCrossoverInstance(this);
}

public class PartiallyMatchedCrossoverInstance : CrossoverInstance<Permutation, PermutationSearchSpace, PartiallyMatchedCrossover> {
  public PartiallyMatchedCrossoverInstance(PartiallyMatchedCrossover parameters) : base(parameters) { }
  public override Permutation Cross(Permutation parent1, Permutation parent2, PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    return Permutation.OrderCrossover(parent1, parent2, random); // implement PMX
  }
}


public record class SwapMutator : Mutator<Permutation, PermutationSearchSpace> {
  public override SwapMutatorInstance CreateInstance() => new SwapMutatorInstance(this);
}

public class SwapMutatorInstance : MutatorInstance<Permutation, PermutationSearchSpace, SwapMutator> {
  public SwapMutatorInstance(SwapMutator parameters) : base(parameters) {}
  public override Permutation Mutate(Permutation solution, PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    return Permutation.SwapRandomElements(solution, random);
  }
}


public record class InversionMutator : Mutator<Permutation, PermutationSearchSpace> {
  public override InversionMutatorInstance CreateInstance() => new InversionMutatorInstance(this);
}

public class InversionMutatorInstance : MutatorInstance<Permutation, PermutationSearchSpace, InversionMutator> {
  public InversionMutatorInstance(InversionMutator parameters) : base(parameters) { }
  public override Permutation Mutate(Permutation parent, PermutationSearchSpace searchSpace, IRandomNumberGenerator random) {
    int start = random.Integer(parent.Count);
    int end = random.Integer(start, parent.Count);
    int[] newElements = parent.ToArray();
    Array.Reverse(newElements, start, end - start + 1);
    return new Permutation(newElements);
  }
}

// // ToDo: move to different file
// public static class GeneticAlgorithmBuilderPermutationSearchSpaceExtensions {
//   // For type inference
//   public static GeneticAlgorithmBuilder<Permutation, PermutationSearchSpace> UsingSearchSpace<TPhenotype>(this GeneticAlgorithmBuilder<Permutation, TPhenotype> builder, PermutationSearchSpace<TPhenotype> searchSpace) {
//     return builder.UsingSearchSpace<Permutation, PermutationSearchSpace, PermutationSearchSpace>(searchSpace);
//   }
// }
