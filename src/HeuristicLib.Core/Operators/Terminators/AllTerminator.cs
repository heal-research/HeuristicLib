using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record AllTerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  : MultiTerminator<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public AllTerminator(params ImmutableArray<ITerminator<TGenotype, TSearchSpace, TProblem, TSearchState>> terminators)
    : base(terminators)
  {
  }

  protected override bool ShouldTerminate(TSearchState searchState,
    IReadOnlyList<InnerShouldTerminate> innerTerminators,
    TSearchSpace searchSpace, TProblem problem)
  {
    return innerTerminators.All(t => t(searchState, searchSpace, problem));
  }
}
