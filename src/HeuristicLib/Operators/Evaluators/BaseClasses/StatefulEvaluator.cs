using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution nodes or execution node resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution node. Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulEvaluator<TCandidate, TSearchSpace, TProblem, TState>
    : Evaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ResolutionScope scope) => new Execution(this, CreateInitialState());

    private sealed class Execution(StatefulEvaluator<TCandidate, TSearchSpace, TProblem, TState> evaluator, TState state)
        : EvaluatorExecution<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => evaluator.Evaluate(candidates, state, random, searchSpace, problem);
    }
}

public abstract record StatefulEvaluator<TCandidate, TSearchSpace, TState>
    : Evaluator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override IEvaluatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ResolutionScope scope) => new Execution(this, CreateInitialState());

    private sealed class Execution(StatefulEvaluator<TCandidate, TSearchSpace, TState> evaluator, TState state)
        : EvaluatorExecution<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace) => evaluator.Evaluate(candidates, state, random, searchSpace);
    }
}

public abstract record StatefulEvaluator<TCandidate, TState>
    : Evaluator<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random);

    public sealed override IEvaluatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ResolutionScope scope) => new Execution(this, CreateInitialState());

    private sealed class Execution(StatefulEvaluator<TCandidate, TState> evaluator, TState state)
        : EvaluatorExecution<TCandidate>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) => evaluator.Evaluate(candidates, state, random);
    }
}
