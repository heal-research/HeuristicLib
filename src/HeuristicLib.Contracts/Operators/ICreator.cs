using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TCandidate> : IOperator
{
    ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceResolver resolver)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface ICreatorInstance<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class CreatorResolverExtensions
{
    extension(ExecutionInstanceResolver resolver)
    {
        public ICreatorInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> creator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            resolver.Resolve(creator, static (creationTarget, childResolver) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childResolver));

        public ICreatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate>? creator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            creator is null ? null : resolver.Resolve<TCandidate, TSearchSpace, TProblem>(creator);

        /// <remarks>A true result carries the instance the run will use, so validating and creating are one step.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            ICreator<TCandidate> creator,
            [NotNullWhen(true)] out ICreatorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = resolver.Resolve<TCandidate, TSearchSpace, TProblem>(creator);
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

    extension<TCandidate, TSearchSpace, TProblem>(TypedExecutionResolver<TCandidate, TSearchSpace, TProblem> resolver)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICreatorInstance<TCandidate, TSearchSpace, TProblem> Resolve(ICreator<TCandidate> creator) =>
            resolver.Resolver.Resolve<TCandidate, TSearchSpace, TProblem>(creator);

        public ICreatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(ICreator<TCandidate>? creator) =>
            creator is null ? null : resolver.Resolve(creator);

        public bool TryResolve(
            ICreator<TCandidate> creator,
            [NotNullWhen(true)] out ICreatorInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Resolver.TryResolve(creator, out instance, out reason);
    }
}
