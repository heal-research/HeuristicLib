using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public class RandomCrossover<TGenotype> 
  : SingleSolutionStatelessCrossover<TGenotype>
  where TGenotype : class
{
  public double Bias { get; }

  public RandomCrossover(double bias = 0.5)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(bias, 0);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(bias, 1);
    
    Bias = bias;
  }

  public override TGenotype Cross(IParents<TGenotype> parents, IRandomNumberGenerator random)
  {
    return random.NextDouble() < Bias ? parents.Parent1 : parents.Parent2;
  }
}
