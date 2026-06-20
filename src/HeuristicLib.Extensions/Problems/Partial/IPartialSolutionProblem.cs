using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Partial;

public interface IPartialSolutionProblem<TGenotype, out TSearchSpace, in TChoice> : IProblem<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  bool IsTerminal(TGenotype genotype, IRandomNumberGenerator random);
  ObjectiveVector Bound(TGenotype genotype, IRandomNumberGenerator random);
  ObjectiveVector? EvaluatePartial(TGenotype genotypes, IRandomNumberGenerator random);
  TGenotype ApplyChoice(TGenotype genotype, TChoice choice);
}
