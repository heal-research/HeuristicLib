using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record AllTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
  : MultiTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public AllTerminator(params ImmutableArray<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> terminators)
      : base(terminators)
    {
    }

    protected override bool IsTerminalState(TSearchState searchState,
      IReadOnlyList<InnerIsTerminalState> innerTerminators,
      TSearchSpace searchSpace, TProblem problem)
    {
        return innerTerminators.All(t => t(searchState, searchSpace, problem));
    }
}
