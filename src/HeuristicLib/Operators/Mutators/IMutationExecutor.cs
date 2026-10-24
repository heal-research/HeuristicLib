using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IMutationExecutor<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class MutationOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Mutation, TCandidate> source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Mutation, TCandidate>? source)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Mutation, TCandidate> source,
            [NotNullWhen(true)] out IMutationExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>().TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Mutation, TCandidate> source) =>
            (IMutationExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Mutation, TCandidate, TSearchSpace, TProblem, ISearchState>(source);

        public IMutationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Mutation, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (IMutationExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Mutation, TCandidate, TSearchSpace, TProblem, ISearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Mutation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Mutation, TCandidate> source,
            [NotNullWhen(true)] out IMutationExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Mutation, TCandidate> source) =>
            (IMutationExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Mutation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);

        public IMutationExecutor<TCandidate, TSearchSpace, TProblem> Resolve<TControl>(IOperator<Mutation, TCandidate> source, out TControl? control)
            where TControl : class
        {
            var executor = (IMutationExecutor<TCandidate, TSearchSpace, TProblem>)scope.Scope.ResolveOperator<Mutation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(source);
            control = ExecutorControls.Find<TControl>(executor);
            return executor;
        }

        [return: NotNullIfNotNull(nameof(source))]
        public IMutationExecutor<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IOperator<Mutation, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Mutation, TCandidate> source,
            [NotNullWhen(true)] out IMutationExecutor<TCandidate, TSearchSpace, TProblem>? executor, [NotNullWhen(false)] out string? reason) =>
            ResolutionScope.TryResolve(() => scope.Resolve(source), out executor, out reason);
    }
}
