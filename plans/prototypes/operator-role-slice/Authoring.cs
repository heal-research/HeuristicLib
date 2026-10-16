using HEAL.HeuristicLib.Operators;

namespace RoleSlice;

public abstract record Mutator<TCandidate, TSearchSpace, TProblem> : BoundOperator<TCandidate, MutationRole, TSearchSpace, TProblem, IMutatorExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;
public abstract record Mutator<TCandidate, TSearchSpace> : Mutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> where TSearchSpace : class, ISearchSpace<TCandidate>;
public abstract record Mutator<TCandidate> : Mutator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract record StatelessMutator<TCandidate> : Mutator<TCandidate>, IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public sealed override ExecutionFactory<IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory() => _ => this;
    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);
    IReadOnlyList<TCandidate> IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> space, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => Mutate(parents, random);
}
public abstract record SingleCandidateMutator<TCandidate> : StatelessMutator<TCandidate>, IExecutionConcurrencyConfiguration
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();
    public abstract TCandidate MutateCandidate(TCandidate parent, IRandomNumberGenerator random);
    public sealed override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) =>
        BatchExecution.Execute(parents, this, static (parent, itemRandom, operation) => operation.MutateCandidate(parent, itemRandom), random, Concurrency);
}
public abstract record StatefulMutator<TCandidate, TState> : Mutator<TCandidate> where TState : class
{
    protected abstract TState CreateInitialState();
    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state, IRandomNumberGenerator random);
    public sealed override ExecutionFactory<IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }
    private sealed class Execution(StatefulMutator<TCandidate, TState> source, TState state) : IMutatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> space, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => source.Mutate(parents, state, random);
    }
}

public abstract record Evaluator<TCandidate, TSearchSpace, TProblem> : BoundOperator<TCandidate, EvaluationRole, TSearchSpace, TProblem, IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;
public abstract record Evaluator<TCandidate> : Evaluator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;
public abstract record StatelessEvaluator<TCandidate> : Evaluator<TCandidate>, IEvaluatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public sealed override ExecutionFactory<IEvaluatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory() => _ => this;
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, IProblem<TCandidate, ISearchSpace<TCandidate>> problem);
    IReadOnlyList<ObjectiveVector> IEvaluatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> space, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => Evaluate(candidates, random, problem);
}

public abstract record Creator<TCandidate, TSearchSpace> : BoundOperator<TCandidate, CreationRole, TSearchSpace, IProblem<TCandidate, TSearchSpace>, ICreatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;
public abstract record SingleCandidateCreator<TCandidate, TSearchSpace> : Creator<TCandidate, TSearchSpace>, ICreatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>, IExecutionConcurrencyConfiguration where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();
    public sealed override ExecutionFactory<ICreatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutionFactory() => _ => this;
    public abstract TCandidate CreateCandidate(IRandomNumberGenerator random, TSearchSpace space);
    public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace space, IProblem<TCandidate, TSearchSpace> problem) =>
        BatchExecution.Execute(count, (source: this, space), static (itemRandom, context) => context.source.CreateCandidate(itemRandom, context.space), random, Concurrency);
}

public abstract record Crossover<TCandidate> : BoundOperator<TCandidate, CrossoverRole, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ICrossoverExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>>;
public abstract record SingleCandidateCrossover<TCandidate> : Crossover<TCandidate>, ICrossoverExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public sealed override ExecutionFactory<ICrossoverExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory() => _ => this;
    public abstract TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random);
    public IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> space, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        BatchExecution.Execute(parents, this, static (pair, itemRandom, source) => source.CrossParents(pair, itemRandom), random, ExecutionConcurrency.Sequential());
}

public abstract record Selector<TCandidate> : BoundOperator<TCandidate, SelectionRole, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISelectorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>>;
public abstract record StatelessSelector<TCandidate> : Selector<TCandidate>, ISelectorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public sealed override ExecutionFactory<ISelectorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory() => _ => this;
    public abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random);
    IReadOnlyList<EvaluatedCandidate<TCandidate>> ISelectorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> space, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => Select(population, objective, count, random);
}

