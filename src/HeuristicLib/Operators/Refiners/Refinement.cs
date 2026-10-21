using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies refining candidates when declaring an operator slot.</summary>
public sealed class Refinement : IItemOperation<Refinement>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem>
        : OperationExecutorAdapter<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override bool SupportsCandidateWork => true;

        protected override IRefinementExecutor<TCandidate, TSearchSpace, TProblem> CreateWrappedExecutor<TWrapper>(
            IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor,
            IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IItemInvocation<TCandidate>, IRefinementArguments<TCandidate>
        {
            private readonly IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;
            public IReadOnlyList<TCandidate> Candidates => candidates;
            public IReadOnlyList<TCandidate> Proceed() => executor.Refine(candidates, random, searchSpace, problem);
            public IReadOnlyList<TCandidate> ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((IRefinementExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Refine(candidates, random ?? this.random, searchSpace, problem);
            public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
            public int ItemCount => candidates.Count;
            public IReadOnlyList<TCandidate> ProceedWithItems(IExecutor executor, IReadOnlyList<int> items)
            {
                var source = candidates;
                return ((IRefinementExecutor<TCandidate, TSearchSpace, TProblem>)executor).Refine([.. items.Select(item => source[item])], random, searchSpace, problem);
            }
        }

        private sealed class WrappedExecutor<TWrapper>(IRefinementExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper)
            : IRefinementExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, IReadOnlyList<TCandidate>>(new Invocation(executor, candidates, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
        }
    }
}

/// <summary>The arguments of one refinement call, which its observations read.</summary>
internal interface IRefinementArguments<TCandidate>
{
    IReadOnlyList<TCandidate> Candidates { get; }
}
