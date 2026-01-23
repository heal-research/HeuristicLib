using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public interface IEvaluator<in TGenotype, in TSearchSpace, in TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
{
  IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
