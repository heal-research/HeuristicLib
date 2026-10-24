using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies evaluating candidates when declaring an operator slot.</summary>
public sealed class Evaluation : IItemOperation<Evaluation>
{
    static bool IOperation<Evaluation>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Evaluation>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static IExecutor IItemOperation<Evaluation>.Route<TCandidate, TSearchSpace, TProblem, TSearchState>(WeightedRouter<IExecutor> children) =>
        new RoutedExecutor<TCandidate, TSearchSpace, TProblem>(children);

    static bool IOperation<Evaluation>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<ObjectiveVector>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<ObjectiveVector> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Evaluate(candidates, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(IReadOnlyList<ObjectiveVector> result) => candidates.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<ObjectiveVector>>(new(Executor, candidates, random, searchSpace, problem));
    }

    private sealed class RoutedExecutor<TCandidate, TSearchSpace, TProblem>(WeightedRouter<IExecutor> children)
        : RoutingExecutor<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>>(children), IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Children.Route(candidates, random, (child, batch) => child.Evaluate(batch, random, searchSpace, problem));
    }
}
