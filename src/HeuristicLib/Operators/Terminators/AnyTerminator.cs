using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AnyTerminator<TCandidate>
    : MultiTerminator<TCandidate>
{
    public AnyTerminator(params IReadOnlyList<ITerminator<TCandidate>> childTerminators)
        : base(childTerminators)
    {
    }

    protected override ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CombineExecutionInstances<TRunSearchSpace, TRunProblem, TRunSearchState>(ImmutableArray<ITerminatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> childTerminators) =>
        new Instance<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminators);

    private sealed class Instance<TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
        : MultiTerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
            ChildTerminators.Any(terminator => terminator.IsTerminalState(state, searchSpace, problem));
    }
}

public static class AnyTerminator
{
    public static AnyTerminator<TCandidate> Create<TCandidate>(params IReadOnlyList<ITerminator<TCandidate>> childTerminators) =>
        new(childTerminators);
}

public static class AnyTerminatorExtensions
{
    extension<TCandidate>(ITerminator<TCandidate> terminator)
    {
        public AnyTerminator<TCandidate> Or(params IReadOnlyList<ITerminator<TCandidate>> otherTerminators) =>
            AnyTerminator.Create([terminator, .. otherTerminators]);
    }
}
