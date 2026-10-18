using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Identifies evaluating candidates when declaring an operator slot.</summary>
public sealed class Evaluation : IOperationDescriptor<Evaluation>
{
    public static OperationExecutorAdapter<Evaluation, TCandidate, TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class ExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationExecutorAdapter<Evaluation, TCandidate, TSearchSpace, TProblem, TRunSearchState,
            IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> CreateInvocationWrapperExecutor(
            IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> child, IInvocationWrapper invocationWrapper) =>
            new InvocationWrapperExecutor(invocationWrapper.Wrap<
                (IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                    TSearchSpace searchSpace, TProblem problem), IReadOnlyList<ObjectiveVector>>(
                args => child.Evaluate(args.candidates, args.random, args.searchSpace, args.problem), static (args, _) => args.candidates.Count));

        private sealed class InvocationWrapperExecutor(
            Func<(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem), IReadOnlyList<ObjectiveVector>> invoke)
            : IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem) =>
                invoke((candidates, random, searchSpace, problem));
        }

    }
}
