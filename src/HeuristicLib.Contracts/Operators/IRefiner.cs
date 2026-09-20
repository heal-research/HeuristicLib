using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
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
    IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceResolver resolver)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IRefinerInstance<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class RefinerResolverExtensions
{
    extension(ExecutionInstanceResolver resolver)
    {
        public IRefinerInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate> refiner)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            resolver.Resolve(refiner, static (creationTarget, childResolver) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childResolver));

        public IRefinerInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate>? refiner)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            refiner is null ? null : resolver.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IRefiner<TCandidate> refiner,
            [NotNullWhen(true)] out IRefinerInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = resolver.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);
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
        public IRefinerInstance<TCandidate, TSearchSpace, TProblem> Resolve(IRefiner<TCandidate> refiner) =>
            resolver.Resolver.Resolve<TCandidate, TSearchSpace, TProblem>(refiner);

        public IRefinerInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IRefiner<TCandidate>? refiner) =>
            refiner is null ? null : resolver.Resolve(refiner);

        public bool TryResolve(
            IRefiner<TCandidate> refiner,
            [NotNullWhen(true)] out IRefinerInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Resolver.TryResolve(refiner, out instance, out reason);
    }
}
