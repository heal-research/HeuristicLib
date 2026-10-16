using HEAL.HeuristicLib.Operators;

namespace RoleSlice;

// This is a streaming execution-family descriptor. Public terminology for algorithms
// sharing the generic configuration substrate remains a migration design question.
public sealed class AlgorithmRole<TState> : IRole<AlgorithmRole<TState>> where TState : class, ISearchState
{
    public static RoleBinding<TCandidate, AlgorithmRole<TState>, TSearchSpace, TProblem> CreateBinding<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new AlgorithmBinding<TCandidate, TSearchSpace, TProblem, TState>();
}
public sealed class AlgorithmBinding<TCandidate, TSearchSpace, TProblem, TState> : ExecutionBinding<TCandidate, AlgorithmRole<TState>, TSearchSpace, TProblem, IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class, ISearchState;
public abstract record Algorithm<TCandidate, TState> : OperatorConfiguration, IOperator<TCandidate, AlgorithmRole<TState>> where TState : class, ISearchState
{
    public virtual bool Fits(ExecutionSignature signature) => signature.SearchState == typeof(TState);
    public abstract ExecutionFactory<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>> CreateExecutionFactory<TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
    public PreparedFactory Prepare<TSearchSpace, TProblem>(RoleBinding<TCandidate, AlgorithmRole<TState>, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => PreparedFactory.Raw(CreateExecutionFactory<TSearchSpace, TProblem>());
}

public sealed record GeneticAlgorithm<TCandidate> : Algorithm<TCandidate, PopulationState<TCandidate>>
{
    public required IOperator<TCandidate, CreationRole> Creator { get; init; }
    public required IOperator<TCandidate, CrossoverRole> Crossover { get; init; }
    public required IOperator<TCandidate, MutationRole> Mutator { get; init; }
    public IOperator<TCandidate, EvaluationRole> Evaluator { get; init; } = new ProblemEvaluator<TCandidate>();
    public IOperator<TCandidate, SelectionRole> Selector { get; init; } = new TournamentSelector<TCandidate>(2);
    public int PopulationSize { get; init; } = ConstructionDefaults.PopulationSize;
    public int MaximumGenerations { get; init; } = ConstructionDefaults.MaximumGenerations;
    public int Elites { get; init; } = ConstructionDefaults.Elites;
    public double MutationRate { get; init; } = ConstructionDefaults.MutationRate;
    public override bool Fits(ExecutionSignature signature) => base.Fits(signature) && signature.Fits(Creator, Crossover, Mutator, Evaluator, Selector);
    public override ExecutionFactory<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>> CreateExecutionFactory<TSearchSpace, TProblem>()
    {
        if (PopulationSize <= 0)
            throw new InvalidOperationException("PopulationSize must be positive.");
        return scope =>
        {
            var typed = new TypedScope<TCandidate, TSearchSpace, TProblem>(scope);
            return new Execution<TSearchSpace, TProblem>(this, typed.Resolve(Creator), typed.Resolve(Crossover), typed.Resolve(Mutator), typed.Resolve(Evaluator), typed.Resolve(Selector));
        };
    }
    private sealed class Execution<TSearchSpace, TProblem>(GeneticAlgorithm<TCandidate> settings, ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator,
        ICrossoverExecution<TCandidate, TSearchSpace, TProblem> crossover, IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator, IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator, ISelectorExecution<TCandidate, TSearchSpace, TProblem> selector)
        : IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly WeightedRouter<IMutatorExecution<TCandidate, TSearchSpace, TProblem>?> mutationRouter = WeightedRouter.AtRate(mutator, settings.MutationRate);
        public async IAsyncEnumerable<PopulationState<TCandidate>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, PopulationState<TCandidate>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            var previous = initialState;
            for (var iteration = 0; iteration < settings.MaximumGenerations; iteration++)
            {
                ct.ThrowIfCancellationRequested();
                var iterationRandom = random.Fork(iteration);
                PopulationState<TCandidate> current;
                if (previous is null)
                {
                    var candidates = creator.Create(settings.PopulationSize, iterationRandom, problem.SearchSpace, problem);
                    current = Population.From(candidates.ToEvaluated(evaluator.Evaluate(candidates, iterationRandom, problem.SearchSpace, problem))).ToPopulationState();
                }
                else
                {
                    var old = previous.Population.EvaluatedCandidates;
                    var selected = selector.Select(old, problem.Objective, settings.PopulationSize * 2, iterationRandom, problem.SearchSpace, problem);
                    var pairs = Enumerable.Range(0, settings.PopulationSize).Select(i => new Parents<TCandidate>(selected[2 * i].Candidate, selected[2 * i + 1].Candidate)).ToArray();
                    var offspring = crossover.Cross(pairs, iterationRandom, problem.SearchSpace, problem);
                    offspring = mutationRouter.Route(offspring, iterationRandom, (choice, batch) => choice is null ? batch : choice.Mutate(batch, iterationRandom, problem.SearchSpace, problem));
                    var evaluated = offspring.ToEvaluated(evaluator.Evaluate(offspring, iterationRandom, problem.SearchSpace, problem));
                    current = Population.From(ElitismReplacer.Replace(old, evaluated, problem.Objective, settings.PopulationSize, settings.Elites)).ToPopulationState();
                }
                previous = current;
                yield return current;
                await Task.Yield();
            }
        }
    }
}
public static class GeneticAlgorithm
{
    public static GeneticAlgorithm<TCandidate> Create<TCandidate>(IOperator<TCandidate, CreationRole> creator,
        IOperator<TCandidate, CrossoverRole> crossover, IOperator<TCandidate, MutationRole> mutator,
        IOperator<TCandidate, EvaluationRole>? evaluator = null, IOperator<TCandidate, SelectionRole>? selector = null,
        int populationSize = ConstructionDefaults.PopulationSize, int maximumGenerations = ConstructionDefaults.MaximumGenerations,
        int elites = ConstructionDefaults.Elites, double mutationRate = ConstructionDefaults.MutationRate) =>
        new()
        {
            Creator = creator,
            Crossover = crossover,
            Mutator = mutator,
            Evaluator = evaluator ?? new ProblemEvaluator<TCandidate>(),
            Selector = selector ?? new TournamentSelector<TCandidate>(2),
            PopulationSize = populationSize,
            MaximumGenerations = maximumGenerations,
            Elites = elites,
            MutationRate = mutationRate
        };
}

public sealed record HillClimber<TCandidate> : Algorithm<TCandidate, PopulationState<TCandidate>>
{
    public required IOperator<TCandidate, CreationRole> Creator { get; init; }
    public required IOperator<TCandidate, MutationRole> Mutator { get; init; }
    public IOperator<TCandidate, EvaluationRole> Evaluator { get; init; } = new ProblemEvaluator<TCandidate>();
    public int MaximumIterations { get; init; } = ConstructionDefaults.MaximumIterations;
    public override bool Fits(ExecutionSignature signature) => base.Fits(signature) && signature.Fits(Creator, Mutator, Evaluator);
    public override ExecutionFactory<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>> CreateExecutionFactory<TSearchSpace, TProblem>() => scope =>
    {
        var typed = new TypedScope<TCandidate, TSearchSpace, TProblem>(scope);
        return new Execution<TSearchSpace, TProblem>(this, typed.Resolve(Creator), typed.Resolve(Mutator), typed.Resolve(Evaluator));
    };
    private sealed class Execution<TSearchSpace, TProblem>(HillClimber<TCandidate> settings, ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator, IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator, IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> evaluator)
        : IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public async IAsyncEnumerable<PopulationState<TCandidate>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, PopulationState<TCandidate>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            var best = initialState?.Population.EvaluatedCandidates.MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer);
            for (var i = 0; i < settings.MaximumIterations; i++)
            {
                ct.ThrowIfCancellationRequested();
                var iterationRandom = random.Fork(i);
                var candidates = best is null ? creator.Create(1, iterationRandom, problem.SearchSpace, problem) : mutator.Mutate([best.Candidate], iterationRandom, problem.SearchSpace, problem);
                var proposal = candidates.ToEvaluated(evaluator.Evaluate(candidates, iterationRandom, problem.SearchSpace, problem))[0];
                if (best is null || problem.Objective.TotalOrderComparer.Compare(proposal.ObjectiveVector, best.ObjectiveVector) < 0)
                    best = proposal;
                yield return Population.From([best]).ToPopulationState();
            }
        }
    }
}

public static class HillClimber
{
    public static HillClimber<TCandidate> Create<TCandidate>(IOperator<TCandidate, CreationRole> creator,
        IOperator<TCandidate, MutationRole> mutator, IOperator<TCandidate, EvaluationRole>? evaluator = null,
        int maximumIterations = ConstructionDefaults.MaximumIterations) =>
        new() { Creator = creator, Mutator = mutator, Evaluator = evaluator ?? new ProblemEvaluator<TCandidate>(), MaximumIterations = maximumIterations };
}

public sealed record Cycle<TCandidate, TState> : Algorithm<TCandidate, TState> where TState : class, ISearchState
{
    public Cycle(IReadOnlyList<IOperator<TCandidate, AlgorithmRole<TState>>> algorithms) => Algorithms = algorithms.ToValueArray();
    public ValueArray<IOperator<TCandidate, AlgorithmRole<TState>>> Algorithms { get; init; }
    public int MaximumCycles { get; init; } = ConstructionDefaults.MaximumCycles;
    public ChildScopePolicy ScopePolicy { get; init; } = ChildScopePolicy.Fresh;
    public override bool Fits(ExecutionSignature signature) => base.Fits(signature) && signature.Fits([.. Algorithms]);
    public override ExecutionFactory<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>> CreateExecutionFactory<TSearchSpace, TProblem>()
    {
        if (Algorithms.Count == 0)
            throw new InvalidOperationException("At least one algorithm is required.");
        var scopeSelector = new ChildScopeSelector(ScopePolicy);
        return scope => new Execution<TSearchSpace, TProblem>(this, scope, scopeSelector);
    }
    private sealed class Execution<TSearchSpace, TProblem>(Cycle<TCandidate, TState> settings, ResolutionScope scope, ChildScopeSelector scopeSelector) : IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public async IAsyncEnumerable<TState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var state = initialState;
            var binding = (AlgorithmBinding<TCandidate, TSearchSpace, TProblem, TState>)BindingCache<TCandidate, AlgorithmRole<TState>, TSearchSpace, TProblem>.Binding;
            for (var cycle = 0; cycle < settings.MaximumCycles; cycle++)
                for (var i = 0; i < settings.Algorithms.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var source = settings.Algorithms[i];
                    var child = binding.Resolve(scopeSelector.GetScopeFor(source, scope), source).Operation;
                    await foreach (var produced in child.RunStreamingAsync(problem, random.Fork(cycle).Fork(i), state, ct))
                    {
                        state = produced;
                        yield return produced;
                    }
                }
        }
    }
}
public static class AlgorithmComposition
{
    public static Cycle<TCandidate, TState> CycleWith<TCandidate, TState>(this IOperator<TCandidate, AlgorithmRole<TState>> first,
        IOperator<TCandidate, AlgorithmRole<TState>> second, int maximumCycles = ConstructionDefaults.MaximumCycles, ChildScopePolicy scopePolicy = ChildScopePolicy.Fresh)
        where TState : class, ISearchState => new([first, second]) { MaximumCycles = maximumCycles, ScopePolicy = scopePolicy };
    public static EvaluationBudget<TCandidate, TState> WithEvaluationBudget<TCandidate, TState>(this IOperator<TCandidate, AlgorithmRole<TState>> algorithm,
        IOperator<TCandidate, EvaluationRole> evaluator, int maximumEvaluations) where TState : class, ISearchState => new(algorithm, evaluator, maximumEvaluations);
}

// A second meta-algorithm: a prepared private inspection aspect bounds child advances.
// Its budget has batch granularity and is checked before the next MoveNextAsync.
public sealed record EvaluationBudget<TCandidate, TState>(IOperator<TCandidate, AlgorithmRole<TState>> ChildAlgorithm,
    IOperator<TCandidate, EvaluationRole> Evaluator, int MaximumEvaluations) : Algorithm<TCandidate, TState> where TState : class, ISearchState
{
    public override bool Fits(ExecutionSignature signature) => base.Fits(signature) && signature.Fits(ChildAlgorithm, Evaluator);
    public override ExecutionFactory<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>> CreateExecutionFactory<TSearchSpace, TProblem>()
    {
        var count = new CountAccumulator();
        var aspect = Evaluator.CountCandidates(count);
        return scope => new Execution<TSearchSpace, TProblem>(this, scope.With(aspect), count);
    }
    private sealed class Execution<TSearchSpace, TProblem>(EvaluationBudget<TCandidate, TState> settings, ResolutionScope scope, CountAccumulator count)
        : IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public async IAsyncEnumerable<TState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (count.CurrentCount >= settings.MaximumEvaluations)
                yield break;
            var binding = (AlgorithmBinding<TCandidate, TSearchSpace, TProblem, TState>)BindingCache<TCandidate, AlgorithmRole<TState>, TSearchSpace, TProblem>.Binding;
            var child = binding.Resolve(scope, settings.ChildAlgorithm).Operation;
            await using var iterator = child.RunStreamingAsync(problem, random, initialState, ct).GetAsyncEnumerator(ct);
            while (count.CurrentCount < settings.MaximumEvaluations && await iterator.MoveNextAsync())
                yield return iterator.Current;
        }
    }
}

// TArguments narrow migration bridge reuses the actual hosting lifecycle. Production APIs remain untouched.
public sealed class HostBridge<TCandidate, TState>(IOperator<TCandidate, AlgorithmRole<TState>> source) : IAlgorithm<TCandidate, TState> where TState : class, ISearchState
{
    public bool Fits(ExecutionSignature signature) => source.Fits(signature);
    public ExecutionFactory<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TState>> CreateExecutionFactory<TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => scope => ((AlgorithmBinding<TCandidate, TSearchSpace, TProblem, TState>)BindingCache<TCandidate, AlgorithmRole<TState>, TSearchSpace, TProblem>.Binding).Resolve(scope, source).Operation;
    public ExecutionFactory<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>> CreateExecutionFactory<TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        if (typeof(TRunSearchState) != typeof(TState))
            throw new InvalidOperationException("Search-state mismatch.");
        return (ExecutionFactory<IAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>>)(object)CreateExecutionFactory<TSearchSpace, TProblem>();
    }
}
public static class Hosting
{
    public static AlgorithmRun<TCandidate, TSearchSpace, TProblem, TState> PrepareRun<TCandidate, TState, TProblem, TSearchSpace>(this IOperator<TCandidate, AlgorithmRole<TState>> algorithm, Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random)
        where TState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
    {
        if (!algorithm.Fits(ExecutionSignature.For<TSearchSpace, TProblem, TState>()))
            throw new InvalidOperationException("Algorithm or a declared child cannot bind this run signature.");
        return new(new HostBridge<TCandidate, TState>(algorithm), (TProblem)problem, random);
    }
}
