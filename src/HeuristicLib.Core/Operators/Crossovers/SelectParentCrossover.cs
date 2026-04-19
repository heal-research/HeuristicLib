using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public record SelectFirstParentCrossover<TGenotype>
  : SingleSolutionCrossover<TGenotype>
{
  public static readonly SelectFirstParentCrossover<TGenotype> Instance = new();

  public override TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random) => SelectFirstParentCrossover.Cross(parents, random);
}

public static class SelectFirstParentCrossover
{
  public static TGenotype Cross<TGenotype>(IParents<TGenotype> parents, IRandomNumberGenerator random)
  {
    return parents.Parent1;
  }
}

public record SelectSecondParentCrossover<TGenotype>
  : SingleSolutionCrossover<TGenotype>
{
  public static readonly SelectSecondParentCrossover<TGenotype> Instance = new();

  public override TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random) => SelectSecondParentCrossover.Cross(parents, random);
}

public static class SelectSecondParentCrossover
{
  public static TGenotype Cross<TGenotype>(IParents<TGenotype> parents, IRandomNumberGenerator random)
  {
    return parents.Parent2;
  }
}