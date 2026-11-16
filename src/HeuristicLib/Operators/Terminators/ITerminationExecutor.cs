using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminationExecutor<in TSearchState>
    : IOperatorExecutor
    where TSearchState : class, ISearchState
{
    bool IsTerminalState(TSearchState state, IRandomNumberGenerator random);
}

public static class TerminationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ITerminationExecutor<TRunSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(IOperator<Termination, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TRunSearchState>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public ITerminationExecutor<TRunSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TRunSearchState>(IOperator<Termination, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TRunSearchState>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(IOperator<Termination, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out ITerminationExecutor<TRunSearchState>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TRunSearchState>(problem).TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public ITerminationExecutor<TRunSearchState> Resolve(IOperator<Termination, TCandidate> source) =>
            (ITerminationExecutor<TRunSearchState>)scope.ResolveOperator<Termination, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source, scope.Problem);

        [return: NotNullIfNotNull(nameof(source))]
        public ITerminationExecutor<TRunSearchState>? ResolveOptional(IOperator<Termination, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Termination, TCandidate> source,
            [NotNullWhen(true)] out ITerminationExecutor<TRunSearchState>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
