using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IEvaluator<TCandidate> : IOperator
{
    ExecutorFactory<IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IEvaluationExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class EvaluatorAdvice<TCandidate> : Advice<IEvaluator<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="evaluator">The configuration the pointcut selected.</param>
    public abstract WrapperExecutorFactory<IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IEvaluator<TCandidate> evaluator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class EvaluatorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> evaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            evaluator is IOperator<TCandidate, Evaluation> prepared
                ? scope.ResolveOperator<TCandidate, Evaluation, TSearchSpace, TProblem, ISearchState, IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>>(
                    prepared, Evaluation.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, ISearchState>()).Executor
                : scope.Resolve(evaluator, static source => source.CreateExecutorFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        [return: NotNullIfNotNull(nameof(evaluator))]
        public IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate>? evaluator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            evaluator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(evaluator);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IEvaluator<TCandidate> evaluator,
            [NotNullWhen(true)] out IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                executor = scope.Resolve<TCandidate, TSearchSpace, TProblem>(evaluator);
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
        public IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IEvaluator<TCandidate> evaluator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(evaluator);

        [return: NotNullIfNotNull(nameof(evaluator))]
        public IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IEvaluator<TCandidate>? evaluator) =>
            evaluator is null ? null : scope.Resolve(evaluator);

        public bool TryResolve(
            IEvaluator<TCandidate> evaluator,
            [NotNullWhen(true)] out IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(evaluator, out executor, out reason);
    }

    private static WrapperExecutorFactory<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, IEvaluator<TCandidate> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((EvaluatorAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(evaluator);
}
