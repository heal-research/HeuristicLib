using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for each preparation. Bindings of that execution share the state.
/// Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulEvaluator<TCandidate, TSearchSpace, TProblem, TState>
    : Evaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override ExecutorFactory<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>> CreateExecutorFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulEvaluator<TCandidate, TSearchSpace, TProblem, TState> evaluator, TState state)
        : EvaluationExecutor<TCandidate, TSearchSpace, TProblem>
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

    public sealed override ExecutorFactory<IEvaluationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutorFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulEvaluator<TCandidate, TSearchSpace, TState> evaluator, TState state)
        : EvaluationExecutor<TCandidate, TSearchSpace>
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

    public sealed override ExecutorFactory<IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutorFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulEvaluator<TCandidate, TState> evaluator, TState state)
        : EvaluationExecutor<TCandidate>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) => evaluator.Evaluate(candidates, state, random);
    }
}
