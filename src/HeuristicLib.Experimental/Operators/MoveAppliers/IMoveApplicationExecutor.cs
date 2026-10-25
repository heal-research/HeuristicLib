using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IMoveApplicationExecutor<TCandidate, in TSearchSpace, in TProblem, in TMove>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    TCandidate Apply(
        TCandidate candidate,
        TMove move,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}

public static class MoveApplicationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveApplication<TMove>, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveApplication<TMove>, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveApplication<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IOperator<MoveApplication<TMove>, TCandidate> source) =>
            (IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>)scope.ResolveOperator<MoveApplication<TMove>, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IOperator<MoveApplication<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveApplication<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IOperator<MoveApplication<TMove>, TCandidate> source) =>
            (IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>)scope.ResolveOperator<MoveApplication<TMove>, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IOperator<MoveApplication<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveApplication<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveApplicationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
