using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record SingleCandidateEvaluator<TCandidate, TSearchSpace, TProblem>
    : StatelessEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract ObjectiveVector EvaluateCandidate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
        BatchExecution.Execute(
            candidates,
            (evaluator: this, searchSpace, problem),
            static (candidate, itemRandom, state) => state.evaluator.EvaluateCandidate(candidate, itemRandom, state.searchSpace, state.problem),
            random,
            Concurrency);
}

public abstract record SingleCandidateEvaluator<TCandidate, TSearchSpace>
    : StatelessEvaluator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract ObjectiveVector EvaluateCandidate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
        BatchExecution.Execute(
            candidates,
            (evaluator: this, searchSpace),
            static (candidate, itemRandom, state) => state.evaluator.EvaluateCandidate(candidate, itemRandom, state.searchSpace),
            random,
            Concurrency);
}

public abstract record SingleCandidateEvaluator<TCandidate>
    : StatelessEvaluator<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract ObjectiveVector EvaluateCandidate(TCandidate candidate, IRandomNumberGenerator random);

    public sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
        BatchExecution.Execute(
            candidates,
            this,
            static (candidate, itemRandom, evaluator) => evaluator.EvaluateCandidate(candidate, itemRandom),
            random,
            Concurrency);
}
