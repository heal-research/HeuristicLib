using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

public interface IMoveCreator<TCandidate, out TMove> : IOperator
{
    IMoveCreatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMoveCreatorInstance<in TCandidate, in TSearchSpace, in TProblem, out TMove>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IEnumerable<TMove> Moves(
        TCandidate candidate,
        IRandomNumberGenerator random,
        TSearchSpace searchSpace,
        TProblem problem);
}

public static class MoveCreatorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveCreator<TCandidate, TMove> moveCreator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(moveCreator, static (creationTarget, childScope) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childScope));

        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveCreator<TCandidate, TMove>? moveCreator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveCreator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveCreator<TCandidate, TMove> moveCreator,
            [NotNullWhen(true)] out IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                instance = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveCreator<TCandidate, TMove> moveCreator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);

        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveCreator<TCandidate, TMove>? moveCreator) =>
            moveCreator is null ? null : scope.Resolve(moveCreator);

        public bool TryResolve<TMove>(
            IMoveCreator<TCandidate, TMove> moveCreator,
            [NotNullWhen(true)] out IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(moveCreator, out instance, out reason);
    }
}
