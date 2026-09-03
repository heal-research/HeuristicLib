using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveCreators;

public interface IMoveCreator<TCandidate, out TMove> : IOperator
{
    IMoveCreatorInstance<TCandidate, TRunSearchSpace, TRunProblem, TMove> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
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

public static class MoveCreatorResolverExtensions
{
    extension(ExecutionInstanceRegistry registry)
    {
        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TCandidate, TSearchSpace, TProblem, TMove>(IMoveCreator<TCandidate, TMove> moveCreator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            registry.Resolve(moveCreator, static (creationTarget, childRegistry) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childRegistry));

        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TCandidate, TSearchSpace, TProblem, TMove>(IMoveCreator<TCandidate, TMove>? moveCreator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            moveCreator is null ? null : registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem, TMove>(
            IMoveCreator<TCandidate, TMove> moveCreator,
            [NotNullWhen(true)] out IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);
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

    extension<TCandidate, TSearchSpace, TProblem>(ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem> resolver)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove> Resolve<TMove>(IMoveCreator<TCandidate, TMove> moveCreator) =>
            resolver.Registry.Resolve<TCandidate, TSearchSpace, TProblem, TMove>(moveCreator);

        public IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? ResolveOptional<TMove>(IMoveCreator<TCandidate, TMove>? moveCreator) =>
            moveCreator is null ? null : resolver.Resolve(moveCreator);

        public bool TryResolve<TMove>(
            IMoveCreator<TCandidate, TMove> moveCreator,
            [NotNullWhen(true)] out IMoveCreatorInstance<TCandidate, TSearchSpace, TProblem, TMove>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Registry.TryResolve(moveCreator, out instance, out reason);
    }
}
