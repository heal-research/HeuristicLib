using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies selecting candidates from a population when declaring an operator slot.</summary>
public sealed class Selection : IOperationDescriptor<Selection>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem>
        : OperationExecutorAdapter<ISelectionExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override bool SupportsCandidateWork => true;

        protected override ISelectionExecutor<TCandidate, TSearchSpace, TProblem> CreateWrappedExecutor<TWrapper>(
            ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor,
            IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<EvaluatedCandidate<TCandidate>>>, ISelectionArguments<TCandidate>
        {
            private readonly ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
            private readonly IRandomNumberGenerator random = random;

            public IRandomNumberGenerator Random => random;
            public ISearchSpace SearchSpace => searchSpace;
            public IProblem Problem => problem;
            public IReadOnlyList<EvaluatedCandidate<TCandidate>> Population => population;
            public ObjectiveDirections Objective => objective;
            public int Count => count;
            public IReadOnlyList<EvaluatedCandidate<TCandidate>> Proceed() => executor.Select(population, objective, count, random, searchSpace, problem);
            public IReadOnlyList<EvaluatedCandidate<TCandidate>> ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
                ((ISelectionExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Select(population, objective, count, random ?? this.random, searchSpace, problem);
            public int CountCandidateWork(IReadOnlyList<EvaluatedCandidate<TCandidate>> result) => result.Count;
        }

        private sealed class WrappedExecutor<TWrapper>(ISelectionExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper)
            : ISelectionExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, IReadOnlyList<EvaluatedCandidate<TCandidate>>>(new Invocation(executor, population, objective, count, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl, TWrapper>(executor, wrapper);
        }
    }
}

/// <summary>The arguments of one selection call, which its observations read.</summary>
internal interface ISelectionArguments<TCandidate>
{
    IReadOnlyList<EvaluatedCandidate<TCandidate>> Population { get; }
    ObjectiveDirections Objective { get; }
    int Count { get; }
}
