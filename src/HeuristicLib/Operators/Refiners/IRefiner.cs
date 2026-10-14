using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// The operator role that works on existing candidates to improve a chosen aspect of them. Common refinements fit
/// candidate parameters, repair invalid candidates, simplify or normalize a representation, and apply local-improvement
/// procedures.
/// </summary>
/// <remarks>
/// A refiner differs from a mutator by intent. A mutator perturbs a candidate to create variation, while a refiner
/// moves a candidate towards a better one along a specific dimension of quality. Where objective values guide the
/// refinement or decide whether to keep its result, the refiner takes an
/// <see cref="IEvaluator{TCandidate}"/> as a configured dependency, which keeps those evaluations
/// visible to budgets, termination, caching and analysis.
/// <para>
/// Continuing the search with the refined candidate is Lamarckian refinement. <c>RefinementEvaluator</c> instead
/// measures a transient refined copy and reports its objective vector for the original candidate, which is Baldwinian.
/// </para>
/// </remarks>
public interface IRefiner<TCandidate> : IOperator
{
    ExecutionFactory<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IRefinerExecution<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecution
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class RefinerAdvice<TCandidate> : Advice<IRefiner<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="refiner">The configuration the pointcut selected.</param>
    public abstract WrapperExecutionFactory<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IRefiner<TCandidate> refiner)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class RefinerResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IRefinerExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate> refiner)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(refiner, static source => source.CreateExecutionFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        [return: NotNullIfNotNull(nameof(refiner))]
        public IRefinerExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate>? refiner)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            refiner is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IRefiner<TCandidate> refiner,
            [NotNullWhen(true)] out IRefinerExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                execution = scope.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);
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
        public IRefinerExecution<TCandidate, TSearchSpace, TProblem> Resolve(IRefiner<TCandidate> refiner) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);

        [return: NotNullIfNotNull(nameof(refiner))]
        public IRefinerExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IRefiner<TCandidate>? refiner) =>
            refiner is null ? null : scope.Resolve(refiner);

        public bool TryResolve(
            IRefiner<TCandidate> refiner,
            [NotNullWhen(true)] out IRefinerExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(refiner, out execution, out reason);
    }

    private static WrapperExecutionFactory<IRefinerExecution<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, IRefiner<TCandidate> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((RefinerAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(refiner);
}
