using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

[Equatable]
public partial record AnyTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : MultiTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public AnyTerminator(params ImmutableArray<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> terminators)
      : base(terminators)
    {
    }

    protected override MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateTerminatorInstance(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> innerTerminators) =>
        new Instance(innerTerminators);

    private sealed class Instance(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> innerTerminators)
        : MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(innerTerminators)
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
            InnerTerminators.Any(terminator => terminator.IsTerminalState(state, searchSpace, problem));
    }
}
