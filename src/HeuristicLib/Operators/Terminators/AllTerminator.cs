using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AllTerminator<TCandidate>
    : MultiTerminator<TCandidate>
{
    public AllTerminator(params IReadOnlyList<ITerminator<TCandidate>> childTerminators)
        : base(childTerminators)
    {
    }

    protected override ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> CombineExecutionInstances<TRunSearchSpace, TRunProblem, TRunSearchState>(ImmutableArray<ITerminatorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> childTerminators) =>
        new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminators);

    private sealed class Execution<TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
        : MultiTerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(childTerminators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
            ChildTerminators.All(terminator => terminator.IsTerminalState(state, searchSpace, problem));
    }
}

public static class AllTerminator
{
    public static AllTerminator<TCandidate> Create<TCandidate>(params IReadOnlyList<ITerminator<TCandidate>> childTerminators) =>
        new(childTerminators);
}

public static class AllTerminatorExtensions
{
    extension<TCandidate>(ITerminator<TCandidate> terminator)
    {
        public AllTerminator<TCandidate> And(params IReadOnlyList<ITerminator<TCandidate>> otherTerminators) =>
            AllTerminator.Create([terminator, .. otherTerminators]);
    }
}
