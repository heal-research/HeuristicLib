using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ICrossover<TCandidate, in TSearchSpace, in TProblem>
  : IOperator<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public interface ICrossoverInstance<TCandidate, in TSearchSpace, in TProblem>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
