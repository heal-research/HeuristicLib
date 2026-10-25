using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// A scope that names the candidate, search space and problem once, so that a role binding on that triple does not
/// name them again at every call.
/// </summary>
/// <remarks>
/// It resolves exactly like the scope it was created from and adds only the types. <c>With</c> and
/// <c>CreateChildScope</c> keep them.
/// </remarks>
public class ResolutionScope<TCandidate, TSearchSpace, TProblem> : ResolutionScope
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ResolutionScope(ResolutionScope scope)
        : base(scope)
    {
    }

    /// <inheritdoc cref="ResolutionScope.With"/>
    public new ResolutionScope<TCandidate, TSearchSpace, TProblem> With(params IReadOnlyList<Aspect> aspects) =>
        new(base.With(aspects));

    /// <inheritdoc cref="ResolutionScope.CreateChildScope"/>
    public new ResolutionScope<TCandidate, TSearchSpace, TProblem> CreateChildScope(params IReadOnlyList<Aspect> aspects) =>
        new(base.CreateChildScope(aspects));
}

/// <summary>
/// A scope that also names the search state, for an algorithm resolving terminators and interceptors alongside its
/// other operators.
/// </summary>
/// <remarks>
/// Terminators and interceptors name only their candidate, so the state they run over comes from the scope rather
/// than from the call site.
/// </remarks>
public sealed class ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState>
    : ResolutionScope<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public ResolutionScope(ResolutionScope scope)
        : base(scope)
    {
    }

    /// <inheritdoc cref="ResolutionScope.With"/>
    public new ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> With(params IReadOnlyList<Aspect> aspects) =>
        new(base.With(aspects));

    /// <inheritdoc cref="ResolutionScope.CreateChildScope"/>
    public new ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> CreateChildScope(params IReadOnlyList<Aspect> aspects) =>
        new(base.CreateChildScope(aspects));
}

/// <summary>Creates a typed scope over a resolution scope, naming the triple once per creation method.</summary>
public static class ResolutionScopeExtensions
{
    extension(ResolutionScope scope)
    {
        public ResolutionScope<TCandidate, TSearchSpace, TProblem> For<TCandidate, TSearchSpace, TProblem>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(scope);

        /// <summary>
        /// Names the search state as well, which an algorithm knows because it is the state the algorithm produces.
        /// The result still serves every role that binds on the triple alone.
        /// </summary>
        public ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> For<TCandidate, TSearchSpace, TProblem, TSearchState>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => new(scope);
    }

    /// <summary>Resolves an operator for its operation and this scope's types, returning its executor untyped.</summary>
    /// <remarks>
    /// An operation's resolution extensions cast the result to its executor interface. This is a classic extension
    /// method so that the operation stays the first type argument.
    /// </remarks>
    public static IExecutor ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        scope.ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

    /// <inheritdoc cref="ResolveOperator{TOperation, TCandidate, TSearchSpace, TProblem}"/>
    public static IExecutor ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope, IOperator<TOperation, TCandidate> source)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        scope.ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(source);

    /// <summary>Resolves an operator for its operation and this scope's types, reporting a failure as a reason instead of throwing.</summary>
    /// <remarks>
    /// The executor variable's type, the operation's executor interface, selects the type the result is returned as, so an
    /// operation's <c>TryResolve</c> extension passes its own variables and names no type argument.
    /// </remarks>
    public static bool TryResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TExecutor>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem> scope, IOperator<TOperation, TCandidate> source,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TExecutor : class, IExecutor =>
        scope.TryResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, ISearchState, TExecutor>(source, out executor, out reason);

    /// <inheritdoc cref="TryResolveOperator{TOperation, TCandidate, TSearchSpace, TProblem, TExecutor}"/>
    public static bool TryResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, TExecutor>(
        this ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope, IOperator<TOperation, TCandidate> source,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TExecutor : class, IExecutor =>
        scope.TryResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, TExecutor>(source, out executor, out reason);
}
