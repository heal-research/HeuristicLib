using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
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
/// <see cref="IOperator{Evaluation, TCandidate}"/> as a configured dependency, which keeps those evaluations
/// visible to budgets, termination, caching and analysis.
/// <para>
/// Continuing the search with the refined candidate is Lamarckian refinement. <c>RefinementEvaluator</c> instead
/// measures a transient refined copy and reports its objective vector for the original candidate, which is Baldwinian.
/// </para>
/// </remarks>
public interface IRefinementExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class RefinementOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IRefinementExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Refinement, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IRefinementExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Refinement, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Refinement, TCandidate> source,
            [NotNullWhen(true)] out IRefinementExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IRefinementExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Refinement, TCandidate> source) =>
            (IRefinementExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Refinement, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

        public IRefinementExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Refinement, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (IRefinementExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Refinement, TCandidate, TSearchSpace, TProblem, ISearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IRefinementExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Refinement, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Refinement, TCandidate> source,
            [NotNullWhen(true)] out IRefinementExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
        {
            try
            {
                executor = scope.Resolve(source);
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

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IRefinementExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Refinement, TCandidate> source) =>
            (IRefinementExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Refinement, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        public IRefinementExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Refinement, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (IRefinementExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Refinement, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IRefinementExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Refinement, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Refinement, TCandidate> source,
            [NotNullWhen(true)] out IRefinementExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
        {
            try
            {
                executor = scope.Resolve(source);
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
}
