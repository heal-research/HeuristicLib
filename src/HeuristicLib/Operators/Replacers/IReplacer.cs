using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer<TCandidate> : IOperator
{
    ExecutorFactory<IReplacementExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IReplacementExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
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
    public abstract WrapperExecutorFactory<IReplacementExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IReplacer<TCandidate> replacer)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class ReplacerResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IReplacementExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            replacer is IOperator<Replacement, TCandidate> prepared
                ? (IReplacementExecutor<TCandidate, TSearchSpace, TProblem>)scope.ResolveOperator<Replacement, TCandidate, TSearchSpace, TProblem, ISearchState>(prepared)
                : scope.Resolve(replacer, static source => source.CreateExecutorFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        [return: NotNullIfNotNull(nameof(replacer))]
        public IReplacementExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate>? replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            replacer is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacementExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                executor = scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);
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
        public IReplacementExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IReplacer<TCandidate> replacer) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        [return: NotNullIfNotNull(nameof(replacer))]
        public IReplacementExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IReplacer<TCandidate>? replacer) =>
            replacer is null ? null : scope.Resolve(replacer);

        public bool TryResolve(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacementExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(replacer, out executor, out reason);
    }

    private static WrapperExecutorFactory<IReplacementExecutor<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, IReplacer<TCandidate> replacer)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((ReplacerAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(replacer);
}
