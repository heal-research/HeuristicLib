using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies creating candidates when declaring an operator slot.</summary>
public sealed class Creation : IOperationDescriptor<Creation>
{
    public static OperationExecutorAdapter<Creation, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Creation, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            ICreationExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override ICreationExecutor<TCandidate, TSearchSpace, TProblem> CreateInvocationWrapperExecutor(
            ICreationExecutor<TCandidate, TSearchSpace, TProblem> child, IInvocationWrapper invocationWrapper) =>
            new InvocationWrapperExecutor(invocationWrapper.Wrap<
                (int count, IRandomNumberGenerator random,
                    TSearchSpace searchSpace, TProblem problem), IReadOnlyList<TCandidate>>(
                args => child.Create(args.count, args.random, args.searchSpace, args.problem), static (_, result) => result.Count));

        private sealed class InvocationWrapperExecutor(
            Func<(int count, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem), IReadOnlyList<TCandidate>> invoke)
            : ICreationExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                invoke((count, random, searchSpace, problem));
        }

    }
}
