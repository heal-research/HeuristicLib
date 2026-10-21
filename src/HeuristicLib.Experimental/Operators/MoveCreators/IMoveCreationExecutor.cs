using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IMoveCreationExecutor<in TCandidate, in TSearchSpace, in TProblem, out TMove>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IEnumerable<TMove> Moves(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}

public static class MoveCreationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveCreation<TMove>, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveCreation<TMove>, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(IOperator<MoveCreation<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IOperator<MoveCreation<TMove>, TCandidate> source) =>
            (IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>)scope.Scope.ResolveOperator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IOperator<MoveCreation<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveCreation<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason)
        {
            try
            {
                executor = scope.Resolve(source);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                executor = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IOperator<MoveCreation<TMove>, TCandidate> source) =>
            (IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>)scope.Scope.ResolveOperator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IOperator<MoveCreation<TMove>, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve<TMove>(IOperator<MoveCreation<TMove>, TCandidate> source,
            [NotNullWhen(true)] out IMoveCreationExecutor<TCandidate, TSearchSpace, TProblem, TMove>? executor, [NotNullWhen(false)] out string? reason)
        {
            try
            {
                executor = scope.Resolve(source);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                executor = null;
                reason = exception.Message;
                return false;
            }
        }
    }
}
