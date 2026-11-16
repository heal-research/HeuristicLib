using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <remarks>The search state is invariant, since <c>Transform</c> returns it as well as reading it.</remarks>
public interface IInterceptionExecutor<TSearchState>
    : IOperatorExecutor
    where TSearchState : class, ISearchState
{
    TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random);
}

public static class InterceptionOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IInterceptionExecutor<TSearchState> Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IOperator<Interception<TSearchState>, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IInterceptionExecutor<TSearchState>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(IOperator<Interception<TSearchState>, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IOperator<Interception<TSearchState>, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out IInterceptionExecutor<TSearchState>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>(problem).TryResolve(source, out executor, out reason);
    }

    /// <remarks>
    /// An interceptor returns the search state its operation names, so its executor uses that state rather than the
    /// scope's.
    /// </remarks>
    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IInterceptionExecutor<TSearchState> Resolve<TSearchState>(IOperator<Interception<TSearchState>, TCandidate> source)
            where TSearchState : class, ISearchState =>
            (IInterceptionExecutor<TSearchState>)scope.ResolveOperator<Interception<TSearchState>, TCandidate, TSearchSpace, TProblem, TSearchState>(source, scope.Problem);

        public IInterceptionExecutor<TSearchState> Resolve<TSearchState, TControl>(IOperator<Interception<TSearchState>, TCandidate> source, out TControl? control)
            where TSearchState : class, ISearchState
            where TControl : class
        {
            var executor = scope.Resolve(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IInterceptionExecutor<TSearchState>? ResolveOptional<TSearchState>(IOperator<Interception<TSearchState>, TCandidate>? source)
            where TSearchState : class, ISearchState => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TSearchState>(IOperator<Interception<TSearchState>, TCandidate> source,
            [NotNullWhen(true)] out IInterceptionExecutor<TSearchState>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchState : class, ISearchState =>
            scope.For<TCandidate, TSearchSpace, TProblem, TSearchState>(scope.Problem).TryResolveOperator(source, out executor, out reason);
    }
}
