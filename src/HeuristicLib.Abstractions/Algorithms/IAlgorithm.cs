using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem, out TAlgorithmResult> : IAlgorithm<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmResult : IAlgorithmState
{
  new TAlgorithmResult Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null);
}

public interface IAlgorithm<TGenotype, in TSearchSpace, in TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  IAlgorithmState Execute(TProblem problem, TSearchSpace? searchSpace = null, IRandomNumberGenerator? random = null);
}
