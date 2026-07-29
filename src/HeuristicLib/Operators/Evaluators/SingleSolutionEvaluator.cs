using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record SingleSolutionEvaluator<TCandidate, TSearchSpace, TProblem>
  : StatelessEvaluator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
      BatchExecution.Execute(candidates, (candidate, r) => Evaluate(candidate, r, searchSpace, problem), random, Concurrency);
}

public abstract record SingleSolutionEvaluator<TCandidate, TSearchSpace>
  : StatelessEvaluator<TCandidate, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract ObjectiveVector Evaluate(TCandidate solution, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
      BatchExecution.Execute(candidates, (candidate, r) => Evaluate(candidate, r, searchSpace), random, Concurrency);
}

public abstract record SingleSolutionEvaluator<TCandidate>
  : StatelessEvaluator<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();

    public abstract ObjectiveVector Evaluate(TCandidate solution, IRandomNumberGenerator random);

    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
      BatchExecution.Execute(candidates, (candidate, r) => Evaluate(candidate, r), random, Concurrency);
}
