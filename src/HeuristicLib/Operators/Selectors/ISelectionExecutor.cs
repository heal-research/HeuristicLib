using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Executes selection using the objective of the problem bound during resolution.</summary>
public interface ISelectionExecutor<TCandidate> : IOperatorExecutor
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, int count, IRandomNumberGenerator random);
}

public static class SelectionOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public ISelectionExecutor<TCandidate> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Selection, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public ISelectionExecutor<TCandidate>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Selection, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Selection, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out ISelectionExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ISelectionExecutor<TCandidate> Resolve(IOperator<Selection, TCandidate> source) =>
            (ISelectionExecutor<TCandidate>)scope.ResolveOperator<Selection, TCandidate, TSearchSpace, TProblem>(source);

        [return: NotNullIfNotNull(nameof(source))]
        public ISelectionExecutor<TCandidate>? ResolveOptional(IOperator<Selection, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Selection, TCandidate> source,
            [NotNullWhen(true)] out ISelectionExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
