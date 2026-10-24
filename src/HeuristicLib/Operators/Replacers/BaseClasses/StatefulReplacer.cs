using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateExecutionState"/> must return a fresh state object. Resolution requests it once per owning execution state, and every executor built there shares it.
/// Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulReplacer<TCandidate, TSearchSpace, TProblem, TState>
    : Replacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override IReplacementExecutor<TCandidate, TSearchSpace, TProblem> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulReplacer<TCandidate, TSearchSpace, TProblem, TState> replacer, TState state)
        : ReplacementExecutor<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random, searchSpace, problem);
    }
}

public abstract record StatefulReplacer<TCandidate, TSearchSpace, TState>
    : Replacer<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override IReplacementExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulReplacer<TCandidate, TSearchSpace, TState> replacer, TState state)
        : ReplacementExecutor<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace) => replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random, searchSpace);
    }
}

public abstract record StatefulReplacer<TCandidate, TState>
    : Replacer<TCandidate>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random);

    protected sealed override IReplacementExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulReplacer<TCandidate, TState> replacer, TState state)
        : ReplacementExecutor<TCandidate>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) => replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random);
    }
}
