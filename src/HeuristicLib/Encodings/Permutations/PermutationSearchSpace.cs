using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.Permutations;

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

    public static IMutator<Permutation> CreateDefaultMutator(PermutationSearchSpace searchSpace) =>
        new InversionMutator();
}
