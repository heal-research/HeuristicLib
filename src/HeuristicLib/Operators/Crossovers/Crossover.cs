using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies combining parent groups into offspring when declaring an operator slot.</summary>
public sealed class Crossover : IOperationDescriptor<Crossover>
{
    public static OperationExecutorAdapter CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem>
        : OperationExecutorAdapter<ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override bool SupportsCandidateWork => true;

        protected override ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> CreateWrappedExecutor<TWrapper>(
            ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper) =>
            new WrappedExecutor<TWrapper>(executor, wrapper);

        private readonly struct Invocation(ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> executor,
            IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<TCandidate>>
        {
            public IReadOnlyList<TCandidate> Proceed() => executor.Cross(parents, random, searchSpace, problem);
            public int CountCandidateWork(IReadOnlyList<TCandidate> result) => result.Count;
        }

        private sealed class WrappedExecutor<TWrapper>(ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> executor, TWrapper wrapper)
            : ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>, IExecutorControlProvider
            where TWrapper : struct, IInvocationWrapper
        {
            public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                wrapper.Invoke<Invocation, IReadOnlyList<TCandidate>>(new Invocation(executor, parents, random, searchSpace, problem));

            public TControl? Get<TControl>() where TControl : class => ExecutorControls.Find<TControl>(executor);
        }
    }
}
