using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public class NoCrossover<TGenotype> : Crossover<TGenotype> {
  public static readonly NoCrossover<TGenotype> Instance = new();

  public override IReadOnlyList<TGenotype> Cross(IReadOnlyList<IParents<TGenotype>> parents, IRandomNumberGenerator random) {
    // ToDo: configurable or different classes?
    return parents.Select(x => x.Parent1).ToArray();
  }
}
