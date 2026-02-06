using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public class NoCrossover<TGenotype>
  : SingleSolutionStatelessCrossover<TGenotype>
  where TGenotype : class
{
  public static readonly NoCrossover<TGenotype> Instance = new();

  public override TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random)
  {
    // ToDo: configurable or different classes?
    return parents.Parent1;
  }
}
