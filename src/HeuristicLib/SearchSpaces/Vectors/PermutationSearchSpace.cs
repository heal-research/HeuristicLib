using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record PermutationSearchSpace(int Length)
    : SearchSpace<Permutation>,
      IEncodingDefaultCreator<Permutation, PermutationSearchSpace>,
      IEncodingDefaultCrossover<Permutation, PermutationSearchSpace>,
      IEncodingDefaultMutator<Permutation, PermutationSearchSpace>
{
    //uniqueness of elements is guaranteed by Permutation class
    public override bool Contains(Permutation candidate) => candidate.Count == Length;

    public static implicit operator IntegerVectorSearchSpace(PermutationSearchSpace permutationSpace) =>
      new(permutationSpace.Length, 0, permutationSpace.Length - 1);

    public static ICreator<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> CreateDefaultCreator(PermutationSearchSpace searchSpace) =>
        new RandomPermutationCreator();

    public static ICrossover<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> CreateDefaultCrossover(PermutationSearchSpace searchSpace) =>
        new EdgeRecombinationCrossover();

    public static IMutator<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>> CreateDefaultMutator(PermutationSearchSpace searchSpace) =>
        new InversionMutator();
}
