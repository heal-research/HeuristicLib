using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer<TCandidate> : IOperator
{
    IReplacerExecution<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IReplacerExecution<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorExecution
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
      IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
      ObjectiveDirections objective, int count,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class ReplacerResolutionExtensions
{
    extension(ResolutionScope scope)
    {
        public IReplacerExecution<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            scope.Resolve(replacer, static (creationTarget, childScope) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childScope));

        public IReplacerExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate>? replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            replacer is null ? null : scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacerExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                execution = scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);
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
        public IReplacerExecution<TCandidate, TSearchSpace, TProblem> Resolve(IReplacer<TCandidate> replacer) =>
            scope.Scope.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        public IReplacerExecution<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IReplacer<TCandidate>? replacer) =>
            replacer is null ? null : scope.Resolve(replacer);

        public bool TryResolve(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacerExecution<TCandidate, TSearchSpace, TProblem>? execution,
            [NotNullWhen(false)] out string? reason) =>
            scope.Scope.TryResolve(replacer, out execution, out reason);
    }
}
