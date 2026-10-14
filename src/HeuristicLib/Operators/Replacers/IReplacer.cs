using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer<TCandidate> : IOperator
{
    ExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IReplacerExecution<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecution
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
      IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
      ObjectiveDirections objective, int count,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class ReplacerAdvice<TCandidate> : Advice<IReplacer<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="replacer">The configuration the pointcut selected.</param>
    public abstract WrapperExecutionFactory<IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IReplacer<TCandidate> replacer)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class ReplacerResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IReplacerExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(replacer, static source => source.CreateExecutionFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        [return: NotNullIfNotNull(nameof(replacer))]
        public IReplacerExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate>? replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            replacer is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacerExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                execution = scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);
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
        public IReplacerExecution<TCandidate, TSearchSpace, TProblem> Resolve(IReplacer<TCandidate> replacer) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        [return: NotNullIfNotNull(nameof(replacer))]
        public IReplacerExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IReplacer<TCandidate>? replacer) =>
            replacer is null ? null : scope.Resolve(replacer);

        public bool TryResolve(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacerExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(replacer, out execution, out reason);
    }

    private static WrapperExecutionFactory<IReplacerExecution<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, IReplacer<TCandidate> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((ReplacerAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(replacer);
}
