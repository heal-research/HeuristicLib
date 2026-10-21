using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AnyTerminator<TCandidate>
    : MultiOperator<Termination, TCandidate>
{
    public AnyTerminator(params IReadOnlyList<IOperator<Termination, TCandidate>> childTerminators)
        : base(childTerminators)
    {
    }

    protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

    private sealed class Factory(AnyTerminator<TCandidate> source) : OperatorExecutorFactory<Termination, TCandidate>
    {
        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
            new Executor<TRunSearchSpace, TRunProblem, TRunSearchState>([.. source.ChildOperators.Select(child => scope.Resolve(child))]);
    }

    private sealed class Executor<TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> childTerminators)
        : ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            childTerminators.Any(terminator => terminator.IsTerminalState(state, random, searchSpace, problem));
    }
}

public static class AnyTerminator
{
    public static AnyTerminator<TCandidate> Create<TCandidate>(params IReadOnlyList<IOperator<Termination, TCandidate>> childTerminators) =>
        new(childTerminators);
}

public static class AnyTerminatorExtensions
{
    extension<TCandidate>(IOperator<Termination, TCandidate> terminator)
    {
        public AnyTerminator<TCandidate> Or(params IReadOnlyList<IOperator<Termination, TCandidate>> otherTerminators) =>
            AnyTerminator.Create([terminator, .. otherTerminators]);
    }
}
