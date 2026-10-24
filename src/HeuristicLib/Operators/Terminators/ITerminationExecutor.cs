using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminationExecutor<TCandidate, in TSearchSpace, in TProblem, in TSearchState>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class TerminationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(IOperator<Termination, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TRunSearchState>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TRunSearchState>(IOperator<Termination, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TRunSearchState>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TRunSearchState>(IOperator<Termination, TCandidate> source,
            [NotNullWhen(true)] out ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TRunSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TRunSearchState>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> Resolve(IOperator<Termination, TCandidate> source) =>
            (ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>)scope.Scope.ResolveOperator<Termination, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState> Resolve<TControl>(IOperator<Termination, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>)scope.Scope.ResolveOperator<Termination, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>? ResolveOptional(IOperator<Termination, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Termination, TCandidate> source,
            [NotNullWhen(true)] out ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TRunSearchState>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }
}
