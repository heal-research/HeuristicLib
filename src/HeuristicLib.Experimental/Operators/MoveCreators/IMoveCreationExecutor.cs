using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IMoveCreationExecutor<in TCandidate, out TMove> : IOperatorExecutor
{
    IEnumerable<TMove> Moves(
        TCandidate candidate, IRandomNumberGenerator random);
}

public static class MoveCreationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMoveCreationExecutor<TCandidate, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveCreation<TMove>, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveCreationExecutor<TCandidate, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveCreation<TMove>, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveCreation<TMove>, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out IMoveCreationExecutor<TCandidate, TMove>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveCreationExecutor<TCandidate, TMove> Resolve<TMove>(IOperator<MoveCreation<TMove>, TCandidate> source) =>
            (IMoveCreationExecutor<TCandidate, TMove>)scope.ResolveOperator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, ISearchState>(source, scope.Problem);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveCreationExecutor<TCandidate, TMove>? ResolveOptional<TMove>(IOperator<MoveCreation<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveCreation<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveCreationExecutor<TCandidate, TMove>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IMoveCreationExecutor<TCandidate, TMove> Resolve<TMove>(IOperator<MoveCreation<TMove>, TCandidate> source) =>
            (IMoveCreationExecutor<TCandidate, TMove>)scope.ResolveOperator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source, scope.Problem);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveCreationExecutor<TCandidate, TMove>? ResolveOptional<TMove>(IOperator<MoveCreation<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveCreation<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveCreationExecutor<TCandidate, TMove>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
