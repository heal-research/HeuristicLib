using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record AnyTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : MultiTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public AnyTerminator(params IReadOnlyList<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> terminators)
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

public static class AnyTerminator
{
    public static AnyTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(params IReadOnlyList<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> terminators)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new([.. terminators]);
}

public static class AnyTerminatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public AnyTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Or(params IReadOnlyList<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> otherTerminators) =>
            AnyTerminator.Create([terminator, .. otherTerminators]);
    }
}
