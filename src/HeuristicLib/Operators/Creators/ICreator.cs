using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TCandidate> : IOperator
{
    ExecutorFactory<ICreationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface ICreationExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class CreatorAdvice<TCandidate> : Advice<ICreator<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="creator">The configuration the pointcut selected.</param>
    public abstract WrapperExecutorFactory<ICreationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ICreator<TCandidate> creator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class CreatorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> creator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            creator is IOperator<TCandidate, Creation> prepared
                ? scope.ResolveOperator<TCandidate, Creation, TSearchSpace, TProblem, ISearchState, ICreationExecutor<TCandidate, TSearchSpace, TProblem>>(
                    prepared, Creation.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, ISearchState>()).Executor
                : scope.Resolve(creator, static source => source.CreateExecutorFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        [return: NotNullIfNotNull(nameof(creator))]
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate>? creator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            creator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(creator);

        /// <remarks>A true result carries the instance the run will use, so validating and creating are one step.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            ICreator<TCandidate> creator,
            [NotNullWhen(true)] out ICreationExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                executor = scope.Resolve<TCandidate, TSearchSpace, TProblem>(creator);
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

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem> Resolve(ICreator<TCandidate> creator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(creator);

        [return: NotNullIfNotNull(nameof(creator))]
        public ICreationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(ICreator<TCandidate>? creator) =>
            creator is null ? null : scope.Resolve(creator);

        public bool TryResolve(
            ICreator<TCandidate> creator,
            [NotNullWhen(true)] out ICreationExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(creator, out executor, out reason);
    }

    private static WrapperExecutorFactory<ICreationExecutor<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, ICreator<TCandidate> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((CreatorAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(creator);
}
