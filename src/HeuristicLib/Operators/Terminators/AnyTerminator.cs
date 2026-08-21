using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AnyTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : MultiTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public AnyTerminator(params IReadOnlyList<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
        : base(childTerminators)
    {
    }

    protected override MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators) =>
        new Instance(childTerminators);

    private sealed class Instance(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
        : MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminators)
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
            ChildTerminators.Any(terminator => terminator.IsTerminalState(state, searchSpace, problem));
    }
}

public static class AnyTerminator
{
    public static AnyTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(params IReadOnlyList<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(childTerminators);
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
