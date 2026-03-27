using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Creators.IntegerVectorCreators;

public record UniformDistributedCreator : SingleSolutionCreator<IntegerVector, IntegerVectorSearchSpace>
{
  public override IntegerVector Create(IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace)
  {
    return IntegerVector.CreateUniform(searchSpace.Length, searchSpace.Minimum, searchSpace.Maximum, random);
  }
}
