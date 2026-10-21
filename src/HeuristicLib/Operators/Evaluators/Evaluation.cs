using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies evaluating candidates when declaring an operator slot.</summary>
public sealed class Evaluation : IItemOperation<Evaluation>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem>
        : OperationExecutorAdapter<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override bool SupportsCandidateWork => true;

        protected override IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> CreateWrappedExecutor<TWrapper>(
            IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor,
            IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IItemInvocation<ObjectiveVector>, IEvaluationArguments<TCandidate>
        {
            private readonly IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;
            public IReadOnlyList<TCandidate> Candidates => candidates;
            public IReadOnlyList<ObjectiveVector> Proceed() => executor.Evaluate(candidates, random, searchSpace, problem);
            public IReadOnlyList<ObjectiveVector> ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Evaluate(candidates, random ?? this.random, searchSpace, problem);
            public int CountCandidateWork(IReadOnlyList<ObjectiveVector> result) => candidates.Count;
            public int ItemCount => candidates.Count;
            public IReadOnlyList<ObjectiveVector> ProceedWithItems(IExecutor executor, IReadOnlyList<int> items)
            {
                var source = candidates;
                return ((IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>)executor).Evaluate([.. items.Select(item => source[item])], random, searchSpace, problem);
            }
        }

        private sealed class WrappedExecutor<TWrapper>(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper)
            : IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, IReadOnlyList<ObjectiveVector>>(new Invocation(executor, candidates, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
        }
    }
}

/// <summary>The arguments of one evaluation call, which its observations read.</summary>
internal interface IEvaluationArguments<TCandidate>
{
    IReadOnlyList<TCandidate> Candidates { get; }
}
