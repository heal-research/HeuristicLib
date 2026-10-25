using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
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
        ImmutableArray<ITerminationExecutor<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> childTerminators = [.. ChildOperators.Select(child => scope.Resolve(child))];
        return new Executor<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminators);
    }

    private sealed class Executor<TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
        : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            childTerminators.All(terminator => terminator.IsTerminalState(state, random, searchSpace, problem));
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
