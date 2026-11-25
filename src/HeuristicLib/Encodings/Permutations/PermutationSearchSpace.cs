using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.Permutations;

public record PermutationSearchSpace(int Length)
    : SearchSpace<Permutation>,
      IRecommends<IOperator<Creation, Permutation>>,
      IRecommends<IOperator<Crossover, Permutation>>,
      IRecommends<IOperator<Mutation, Permutation>>
{
    //uniqueness of elements is guaranteed by Permutation class
    public override bool Contains(Permutation candidate) => candidate.Count == Length;

    public static implicit operator IntegerVectorSearchSpace(PermutationSearchSpace permutationSpace) =>
      new(permutationSpace.Length, 0, permutationSpace.Length - 1);

    public bool TryCreateRecommendedOperator([NotNullWhen(true)] out IOperator<Creation, Permutation>? recommendation)
    {
        recommendation = new RandomPermutationCreator();
        return true;
    }

    public bool TryCreateRecommendedOperator([NotNullWhen(true)] out IOperator<Crossover, Permutation>? recommendation)
    {
        recommendation = new EdgeRecombinationCrossover();
        return true;
    }

    public bool TryCreateRecommendedOperator([NotNullWhen(true)] out IOperator<Mutation, Permutation>? recommendation)
    {
        recommendation = new InversionMutator();
        return true;
    }
}
