using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record Evaluator<TCandidate, TSearchSpace, TProblem, TExecutionState>
  : IEvaluator<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TExecutionState executionState,
      IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new EvaluatorInstance(this, CreateInitialState());

    private sealed class EvaluatorInstance(Evaluator<TCandidate, TSearchSpace, TProblem, TExecutionState> evaluator, TExecutionState executionState)
      : IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            return evaluator.Evaluate(candidates, executionState, random, searchSpace, problem);
        }
    }
}

public abstract record Evaluator<TCandidate, TSearchSpace, TExecutionState>
  : IEvaluator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TExecutionState executionState,
      IRandomNumberGenerator random, TSearchSpace searchSpace);

    public IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new EvaluatorInstance(this, CreateInitialState());

    private sealed class EvaluatorInstance(Evaluator<TCandidate, TSearchSpace, TExecutionState> evaluator, TExecutionState executionState)
      : IEvaluatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem)
        {
            return evaluator.Evaluate(candidates, executionState, random, searchSpace);
        }
    }
}

public abstract record Evaluator<TCandidate, TExecutionState>
  : IEvaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
  where TExecutionState : class
{
    protected abstract TExecutionState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TExecutionState executionState,
      IRandomNumberGenerator random);

    public IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
      new EvaluatorInstance(this, CreateInitialState());

    private sealed class EvaluatorInstance(Evaluator<TCandidate, TExecutionState> evaluator, TExecutionState executionState)
      : IEvaluatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
        {
            return evaluator.Evaluate(candidates, executionState, random);
        }
    }
}
