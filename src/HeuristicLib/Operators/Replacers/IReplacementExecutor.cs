using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Executes replacement using the objective of the problem bound during resolution.</summary>
public interface IReplacementExecutor<TCandidate> : IOperatorExecutor
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
      IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, int count, IRandomNumberGenerator random);
}

public static class ReplacementOperationResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IReplacementExecutor<TCandidate> Resolve<TCandidate, TSearchSpace, TProblem>(IOperator<Replacement, TCandidate> source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).Resolve(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IReplacementExecutor<TCandidate>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IOperator<Replacement, TCandidate>? source, TProblem problem)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).ResolveOptional(source);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(IOperator<Replacement, TCandidate> source, TProblem problem,
            [NotNullWhen(true)] out IReplacementExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope.For<TCandidate, TSearchSpace, TProblem>(problem).TryResolve(source, out executor, out reason);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReplacementExecutor<TCandidate> Resolve(IOperator<Replacement, TCandidate> source) =>
            (IReplacementExecutor<TCandidate>)scope.ResolveOperator<Replacement, TCandidate, TSearchSpace, TProblem>(source);

        [return: NotNullIfNotNull(nameof(source))]
        public IReplacementExecutor<TCandidate>? ResolveOptional(IOperator<Replacement, TCandidate>? source) => source is null ? null : scope.Resolve(source);

        public bool TryResolve(IOperator<Replacement, TCandidate> source,
            [NotNullWhen(true)] out IReplacementExecutor<TCandidate>? executor, [NotNullWhen(false)] out string? reason) =>
            scope.TryResolveOperator(source, out executor, out reason);
    }
}
