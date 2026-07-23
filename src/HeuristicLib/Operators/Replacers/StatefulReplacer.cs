using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution instances or execution instance resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution instance. Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulReplacer<TCandidate, TSearchSpace, TProblem, TState>
    : Replacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override IReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulReplacer<TCandidate, TSearchSpace, TProblem, TState> replacer, TState state) : IReplacerInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random, searchSpace, problem);
    }
}

public abstract record StatefulReplacer<TCandidate, TSearchSpace, TState>
    : Replacer<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override IReplacerInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateReplacerInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulReplacer<TCandidate, TSearchSpace, TState> replacer, TState state) : IReplacerInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) => replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random, searchSpace);
    }
}

public abstract record StatefulReplacer<TCandidate, TState>
    : Replacer<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random);

    protected sealed override IReplacerInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateReplacerInstance(ExecutionInstanceRegistry registry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulReplacer<TCandidate, TState> replacer, TState state) : IReplacerInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => replacer.Replace(previousPopulation, offspringPopulation, objective, count, state, random);
    }
}
