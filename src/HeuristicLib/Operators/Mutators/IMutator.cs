using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IMutator<TCandidate> : IOperator
{
    ExecutorFactory<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IMutationExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class MutatorAdvice<TCandidate> : Advice<IMutator<TCandidate>>
{
    /// <summary>Prepares this advice for one join point and returns a constructor accepting the execution it wraps.</summary>
    /// <remarks>
    /// Resolution calls this once per join point and owner state. Validate the run's types and allocate state that must
    /// survive rebuilding the execution graph here.
    /// </remarks>
    /// <param name="mutator">The configuration the pointcut selected.</param>
    public abstract WrapperExecutorFactory<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IMutator<TCandidate> mutator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public static class MutatorResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate> mutator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            mutator is IOperator<TCandidate, Mutation> prepared
                ? scope.ResolveOperator<TCandidate, Mutation, TSearchSpace, TProblem, ISearchState, IMutationExecutor<TCandidate, TSearchSpace, TProblem>>(
                    prepared, Mutation.CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, ISearchState>()).Executor
                : scope.Resolve(mutator, static source => source.CreateExecutorFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>);

        [return: NotNullIfNotNull(nameof(mutator))]
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate>? mutator)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            mutator is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(mutator);

        /// <remarks>A true result carries the instance the run will use, so validating and creating are one step.</remarks>
        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IMutator<TCandidate> mutator,
            [NotNullWhen(true)] out IMutationExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                executor = scope.Resolve<TCandidate, TSearchSpace, TProblem>(mutator);
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
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IMutator<TCandidate> mutator) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(mutator);

        /// <summary>Resolves the wrapped mutator and an optional control from the same selected raw binding.</summary>
        /// <remarks>
        /// Observation wrappers need not implement the source's control interfaces. Resolving the control separately
        /// preserves access to it without bypassing the wrapped mutation operation.
        /// Request an operation-free control and invoke mutation through the returned execution. The control is null
        /// when the raw binding does not implement its type. Configured wrappers expose their own controls explicitly.
        /// </remarks>
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IMutator<TCandidate> mutator, out TControl? control)
            where TControl : class =>
            mutator is IOperator<TCandidate, Mutation> prepared
                ? MutationOperationResolutionExtensions.Resolve(scope, prepared, out control)
                : scope.Scope.Resolve(mutator, static source => source.CreateExecutorFactory<TSearchSpace, TProblem>(), CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>, static executor => executor as TControl, out control);

        [return: NotNullIfNotNull(nameof(mutator))]
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IMutator<TCandidate>? mutator) =>
            mutator is null ? null : scope.Resolve(mutator);

        public bool TryResolve(
            IMutator<TCandidate> mutator,
            [NotNullWhen(true)] out IMutationExecutor<TCandidate, TSearchSpace, TProblem>? executor,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(mutator, out executor, out reason);
    }

    private static WrapperExecutorFactory<IMutationExecutor<TCandidate, TSearchSpace, TProblem>> CreateWrapperFactory<TCandidate, TSearchSpace, TProblem>(Advice advice, IMutator<TCandidate> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        ((MutatorAdvice<TCandidate>)advice).CreateWrapperFactory<TSearchSpace, TProblem>(mutator);
}
