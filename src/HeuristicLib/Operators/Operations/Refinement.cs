using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies refining candidates when declaring an operator slot.</summary>
public sealed class Refinement : IOperationDescriptor<Refinement>
{
    public static OperationExecutorAdapter<Refinement, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Refinement, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            IRefinementExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override IRefinementExecutor<TCandidate, TSearchSpace, TProblem> CreateInvocationWrapperExecutor(
            IRefinementExecutor<TCandidate, TSearchSpace, TProblem> child, IInvocationWrapper invocationWrapper) =>
            new InvocationWrapperExecutor(invocationWrapper.Wrap<
                (IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                    TSearchSpace searchSpace, TProblem problem), IReadOnlyList<TCandidate>>(
                args => child.Refine(args.candidates, args.random, args.searchSpace, args.problem), static (_, result) => result.Count));

        private sealed class InvocationWrapperExecutor(
            Func<(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem), IReadOnlyList<TCandidate>> invoke)
            : IRefinementExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                invoke((candidates, random, searchSpace, problem));
        }

    }
}
