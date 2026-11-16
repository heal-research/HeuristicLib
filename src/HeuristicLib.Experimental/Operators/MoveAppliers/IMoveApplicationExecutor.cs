using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IMoveApplicationExecutor<TCandidate, in TMove> : IOperatorExecutor
{
    TCandidate Apply(
        TCandidate candidate, TMove move, IRandomNumberGenerator random);
}

public static class MoveApplicationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMoveApplicationExecutor<TCandidate, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveApplication<TMove>, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveApplicationExecutor<TCandidate, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveApplication<TMove>, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveApplication<TMove>, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out IMoveApplicationExecutor<TCandidate, TMove>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveApplicationExecutor<TCandidate, TMove> Resolve<TMove>(IOperator<MoveApplication<TMove>, TCandidate> source) =>
            (IMoveApplicationExecutor<TCandidate, TMove>)scope.ResolveOperator<MoveApplication<TMove>, TCandidate, TSearchSpace, TProblem, ISearchState>(source, scope.Problem);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveApplicationExecutor<TCandidate, TMove>? ResolveOptional<TMove>(IOperator<MoveApplication<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveApplication<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveApplicationExecutor<TCandidate, TMove>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IMoveApplicationExecutor<TCandidate, TMove> Resolve<TMove>(IOperator<MoveApplication<TMove>, TCandidate> source) =>
            (IMoveApplicationExecutor<TCandidate, TMove>)scope.ResolveOperator<MoveApplication<TMove>, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source, scope.Problem);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveApplicationExecutor<TCandidate, TMove>? ResolveOptional<TMove>(IOperator<MoveApplication<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveApplication<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveApplicationExecutor<TCandidate, TMove>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
