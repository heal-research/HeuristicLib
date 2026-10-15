using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface ISelector<TCandidate> : IOperator
{
    ExecutionFactory<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface ISelectorExecution<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecution
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class SelectorAdvice<TCandidate> : Advice<ISelector<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="selector">The configuration the pointcut selected.</param>
    public abstract WrapperExecutionFactory<ISelectorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(ISelector<TCandidate> selector)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class SelectorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ISelectorExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate> selector)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(selector, static source => source.CreateExecutionFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        [return: NotNullIfNotNull(nameof(selector))]
        public ISelectorExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate>? selector)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            selector is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(selector);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            ISelector<TCandidate> selector,
            [NotNullWhen(true)] out ISelectorExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                execution = scope.Resolve<TCandidate, TSearchSpace, TProblem>(selector);
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
        public ISelectorExecution<TCandidate, TSearchSpace, TProblem> Resolve(ISelector<TCandidate> selector) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(selector);

        [return: NotNullIfNotNull(nameof(selector))]
        public ISelectorExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional(ISelector<TCandidate>? selector) =>
            selector is null ? null : scope.Resolve(selector);

        public bool TryResolve(
            ISelector<TCandidate> selector,
            [NotNullWhen(true)] out ISelectorExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(selector, out execution, out reason);
    }

    private static WrapperExecutionFactory<ISelectorExecution<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, ISelector<TCandidate> selector)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((SelectorAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(selector);
}
