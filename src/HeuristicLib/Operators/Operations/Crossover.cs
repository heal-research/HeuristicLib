using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies combining parent groups into offspring when declaring an operator slot.</summary>
public sealed class Crossover : IOperationDescriptor<Crossover>
{
    public static OperationExecutorAdapter<Crossover, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Crossover, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> CreateInvocationWrapperExecutor(
            ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> child, IInvocationWrapper invocationWrapper) =>
            new InvocationWrapperExecutor(invocationWrapper.Wrap<
                (IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
                    TSearchSpace searchSpace, TProblem problem), IReadOnlyList<TCandidate>>(
                args => child.Cross(args.parents, args.random, args.searchSpace, args.problem), static (_, result) => result.Count));

        private sealed class InvocationWrapperExecutor(
            Func<(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem), IReadOnlyList<TCandidate>> invoke)
            : ICrossoverExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                invoke((parents, random, searchSpace, problem));
        }

    }
}
