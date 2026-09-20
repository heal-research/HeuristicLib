using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public interface IReplacer<TCandidate> : IOperator
{
    IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceResolver resolver)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public interface IReplacerInstance<TCandidate, in TSearchSpace, in TProblem>
    : IOperatorInstance
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(
      IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation,
      ObjectiveDirections objective, int count,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public static class ReplacerResolverExtensions
{
    extension(ExecutionInstanceResolver resolver)
    {
        public IReplacerInstance<TCandidate, TSearchSpace, TProblem> Resolve<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            resolver.Resolve(replacer, static (creationTarget, childResolver) => creationTarget.CreateExecutionInstance<TSearchSpace, TProblem>(childResolver));

        public IReplacerInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate>? replacer)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            replacer is null ? null : resolver.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        public bool TryResolve<TCandidate, TSearchSpace, TProblem>(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacerInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
        {
            try
            {
                instance = resolver.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);
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
        public IReplacerInstance<TCandidate, TSearchSpace, TProblem> Resolve(IReplacer<TCandidate> replacer) =>
            resolver.Resolver.Resolve<TCandidate, TSearchSpace, TProblem>(replacer);

        public IReplacerInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IReplacer<TCandidate>? replacer) =>
            replacer is null ? null : resolver.Resolve(replacer);

        public bool TryResolve(
            IReplacer<TCandidate> replacer,
            [NotNullWhen(true)] out IReplacerInstance<TCandidate, TSearchSpace, TProblem>? instance,
            [NotNullWhen(false)] out string? reason) =>
            resolver.Resolver.TryResolve(replacer, out instance, out reason);
    }
}
