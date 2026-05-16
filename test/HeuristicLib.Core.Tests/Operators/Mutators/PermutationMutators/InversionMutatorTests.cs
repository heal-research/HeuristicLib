using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.PermutationMutators;

public class InversionMutatorTests
{
  [Fact]
  public void Mutate_MatchesReferenceExample()
  {
    var parent = Permutation.Create(0, 1, 2, 3, 4, 5, 6, 7, 8);

    var result = InversionMutator.Mutate(parent, new SequenceRandomNumberGenerator(), start: 1, end: 4);

    result.ShouldBe(Permutation.Create(0, 4, 3, 2, 1, 5, 6, 7, 8));
  }
}
