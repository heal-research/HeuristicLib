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
    public static OperationExecutorAdapter<Selection, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Selection, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            ISelectionExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override ISelectionExecutor<TCandidate, TSearchSpace, TProblem> CreateInvocationWrapperExecutor(
            ISelectionExecutor<TCandidate, TSearchSpace, TProblem> child, IInvocationWrapper invocationWrapper) =>
            new InvocationWrapperExecutor(invocationWrapper.Wrap<
                (IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random,
                    TSearchSpace searchSpace, TProblem problem), IReadOnlyList<EvaluatedCandidate<TCandidate>>>(
                args => child.Select(args.population, args.objective, args.count, args.random, args.searchSpace, args.problem), static (_, result) => result.Count));

        private sealed class InvocationWrapperExecutor(
            Func<(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem), IReadOnlyList<EvaluatedCandidate<TCandidate>>> invoke)
            : ISelectionExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                invoke((population, objective, count, random, searchSpace, problem));
        }

    }
}
