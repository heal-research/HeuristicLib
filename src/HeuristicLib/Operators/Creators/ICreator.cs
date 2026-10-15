using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ICreator<TCandidate> : IOperator
{
    ExecutionFactory<ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface ICreatorExecution<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecution
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
    public abstract WrapperExecutionFactory<ICreatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ICreator<TCandidate> creator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class CreatorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ICreatorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> creator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(creator, static source => source.CreateExecutionFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        [return: NotNullIfNotNull(nameof(creator))]
        public ICreatorExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate>? creator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            creator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(creator);

        /// <remarks>A true result carries the instance the run will use, so validating and creating are one step.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            ICreator<TCandidate> creator,
            [NotNullWhen(true)] out ICreatorExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                execution = scope.Resolve<TCandidate, TSearchSpace, TProblem>(creator);
                reason = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                execution = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ICreatorExecution<TCandidate, TSearchSpace, TProblem> Resolve(ICreator<TCandidate> creator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(creator);

        [return: NotNullIfNotNull(nameof(creator))]
        public ICreatorExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional(ICreator<TCandidate>? creator) =>
            creator is null ? null : scope.Resolve(creator);

        public bool TryResolve(
            ICreator<TCandidate> creator,
            [NotNullWhen(true)] out ICreatorExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(creator, out execution, out reason);
    }

    private static WrapperExecutionFactory<ICreatorExecution<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, ICreator<TCandidate> creator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((CreatorAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(creator);
}
