using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AnyTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>(params IReadOnlyList<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> terminators)
  : Terminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IReadOnlyList<ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> Terminators { get; } = terminators;

  public override Func<TAlgorithmState, bool> CreateShouldTerminatePredicate(TSearchSpace searchSpace, TProblem problem) {
    var predicates = Terminators.Select(t => t.CreateShouldTerminatePredicate(searchSpace, problem)).ToList();

    return state => predicates.Any(p => p(state));
  }
}
