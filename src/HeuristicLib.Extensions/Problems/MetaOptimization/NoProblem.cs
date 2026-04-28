using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public record NoProblem<T1, TS1>(TS1 SearchSpace) : IProblem<T1, TS1> where TS1 : class, ISearchSpace<T1>
{
  public Objective Objective => ZeroObjective.Instance;
  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<T1> genotypes, IRandomNumberGenerator random) => throw new NotImplementedException();
}
