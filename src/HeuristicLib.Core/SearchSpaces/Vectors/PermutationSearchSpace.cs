using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record PermutationSearchSpace(int Length) : SearchSpace<Permutation>, ISubSearchSpaceComparable<PermutationSearchSpace>
{
  public override bool Contains(Permutation genotype) => genotype.Count == Length;

  public virtual bool IsSubspaceOf(PermutationSearchSpace other) => other.Length == Length;

  public static implicit operator IntegerVectorSearchSpace(PermutationSearchSpace permutationSpace) => new(permutationSpace.Length, 0, permutationSpace.Length);
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

// // ToDo: move to different file
// public static class GeneticAlgorithmBuilderPermutationSearchSpaceExtensions {
//   // For type inference
//   public static GeneticAlgorithmBuilder<Permutation, PermutationSearchSpace> UsingSearchSpace<TPhenotype>(this GeneticAlgorithmBuilder<Permutation, TPhenotype> builder, PermutationSearchSpace<TPhenotype> searchSpace) {
//     return builder.UsingSearchSpace<Permutation, PermutationSearchSpace, PermutationSearchSpace>(searchSpace);
//   }
// }
