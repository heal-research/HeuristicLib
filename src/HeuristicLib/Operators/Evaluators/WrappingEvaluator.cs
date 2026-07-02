using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record WrappingEvaluator<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IEvaluator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected delegate IReadOnlyList<ObjectiveVector> InnerEvaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected IEvaluator<TCandidate, TSearchSpace, TProblem> InnerEvaluator { get; }

    protected WrappingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> innerEvaluator)
    {
        InnerEvaluator = innerEvaluator;
    }

    public IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, instanceRegistry.Resolve(InnerEvaluator).Evaluate, CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TExecutionState executionState,
      InnerEvaluate innerEvaluate, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(WrappingEvaluator<TCandidate, TSearchSpace, TProblem, TExecutionState> wrappingEvaluator,
      InnerEvaluate innerEvaluate, TExecutionState executionState)
      : IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return wrappingEvaluator.Evaluate(candidates, executionState, innerEvaluate, random, searchSpace, problem);
        }
    }
}

public abstract record WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
  : WrappingEvaluator<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingEvaluator(IEvaluator<TCandidate, TSearchSpace, TProblem> innerEvaluator)
      : base(innerEvaluator)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, NoState executionState,
      InnerEvaluate innerEvaluate, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem)
      => Evaluate(candidates, innerEvaluate, random, searchSpace, problem);

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates,
      InnerEvaluate innerEvaluate, IRandomNumberGenerator random,
      TSearchSpace searchSpace, TProblem problem);
}
