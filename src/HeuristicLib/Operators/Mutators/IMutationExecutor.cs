using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IMutationExecutor<TCandidate> : IOperatorExecutor
{
    IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);
}

public static class MutationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMutationExecutor<TCandidate> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Mutation, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMutationExecutor<TCandidate>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Mutation, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Mutation, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out IMutationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMutationExecutor<TCandidate> Resolve(IOperator<Mutation, TCandidate> source) =>
            (IMutationExecutor<TCandidate>)scope.ResolveOperator<Mutation, TCandidate, TSearchSpace, TProblem, ISearchState>(source, scope.Problem);

        public IMutationExecutor<TCandidate> Resolve<TControl>(IOperator<Mutation, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (IMutationExecutor<TCandidate>)scope.ResolveOperator<Mutation, TCandidate, TSearchSpace, TProblem, ISearchState>(source, scope.Problem);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IMutationExecutor<TCandidate>? ResolveOptional(IOperator<Mutation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Mutation, TCandidate> source,
            [NotNullWhen(true)] out IMutationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IMutationExecutor<TCandidate> Resolve(IOperator<Mutation, TCandidate> source) =>
            (IMutationExecutor<TCandidate>)scope.ResolveOperator<Mutation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source, scope.Problem);

        public IMutationExecutor<TCandidate> Resolve<TControl>(IOperator<Mutation, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (IMutationExecutor<TCandidate>)scope.ResolveOperator<Mutation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source, scope.Problem);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IMutationExecutor<TCandidate>? ResolveOptional(IOperator<Mutation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Mutation, TCandidate> source,
            [NotNullWhen(true)] out IMutationExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
