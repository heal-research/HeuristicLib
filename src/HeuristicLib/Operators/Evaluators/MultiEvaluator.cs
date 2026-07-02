using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

[Equatable]
public abstract partial record MultiEvaluator<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IEvaluator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [OrderedEquality] protected ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> InnerEvaluators { get; }

    protected delegate IReadOnlyList<ObjectiveVector> InnerEvaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected MultiEvaluator(ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> innerEvaluators)
    {
        InnerEvaluators = innerEvaluators;
    }

    public IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new Instance(this, InnerEvaluators.Select(instanceRegistry.Resolve).Select(x => (InnerEvaluate)x.Evaluate).ToArray(), CreateInitialState());

    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TExecutionState executionState,
      IReadOnlyList<InnerEvaluate> innerEvaluators,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Instance(MultiEvaluator<TCandidate, TSearchSpace, TProblem, TExecutionState> multiEvaluator,
      IReadOnlyList<InnerEvaluate> innerEvaluators, TExecutionState executionState)
      : IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return multiEvaluator.Evaluate(candidates, executionState, innerEvaluators, random, searchSpace, problem);
        }
    }
}

public abstract record MultiEvaluator<TCandidate, TSearchSpace, TProblem>
  : MultiEvaluator<TCandidate, TSearchSpace, TProblem, NoState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiEvaluator(ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> innerEvaluators)
      : base(innerEvaluators)
    {
    }

    protected sealed override NoState CreateInitialState() => NoState.Instance;

    protected sealed override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, NoState executionState,
      IReadOnlyList<InnerEvaluate> innerEvaluators,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
      => Evaluate(candidates, innerEvaluators, random, searchSpace, problem);

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates,
      IReadOnlyList<InnerEvaluate> innerEvaluators,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
