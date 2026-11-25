using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AllTerminator<TCandidate>
    : MultiOperator<Termination, TCandidate>
{
    public AllTerminator(params IReadOnlyList<IOperator<Termination, TCandidate>> childTerminators)
        : base(childTerminators)
    {
    }

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        ImmutableArray<ITerminationExecutor<TRunSearchState>> childTerminators = [.. ChildOperators.Select(child => scope.Resolve(child))];
        return new Executor<TRunSearchState>(childTerminators);
    }

    private sealed class Executor<TSearchState>(ImmutableArray<ITerminationExecutor<TSearchState>> childTerminators)
        : TerminationExecutor<TSearchState>
        where TSearchState : class, ISearchState
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) =>
            childTerminators.All(terminator => terminator.IsTerminalState(state, random));
    }
}

public static class AllTerminator
{
    public static AllTerminator<TCandidate> Create<TCandidate>(params IReadOnlyList<IOperator<Termination, TCandidate>> childTerminators) =>
        new(childTerminators);
}

public static class AllTerminatorExtensions
{
    extension<TCandidate>(IOperator<Termination, TCandidate> terminator)
    {
        public AllTerminator<TCandidate> And(params IReadOnlyList<IOperator<Termination, TCandidate>> otherTerminators) =>
            AllTerminator.Create([terminator, .. otherTerminators]);
    }
}
