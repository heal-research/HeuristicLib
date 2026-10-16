using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using RoleSlice;

namespace SliceTests;

public static class Check
{
    public static int Assertions;
    public static void That(bool condition, string message = "assertion failed") { Assertions++; if (!condition) throw new Exception(message); }
    public static void Equal<TResult>(TResult expected, TResult actual) => That(EqualityComparer<TResult>.Default.Equals(expected, actual), $"Expected {expected}, got {actual}.");
    public static void Sequence<TResult>(IEnumerable<TResult> expected, IEnumerable<TResult> actual) => That(expected.SequenceEqual(actual), $"Sequences differ: [{string.Join(", ", expected)}] versus [{string.Join(", ", actual)}].");
    public static void NearSequence(IReadOnlyList<double> expected, IReadOnlyList<double> actual) => That(expected.Count == actual.Count && expected.Zip(actual).All(pair => Math.Abs(pair.First - pair.Second) < 1e-12));
    public static void Throws<TResult>(Action action) where TResult : Exception
    {
        try
        { action(); }
        catch (TResult) { Assertions++; return; }
        throw new Exception($"Expected {typeof(TResult).Name}.");
    }
    public static async Task ThrowsAsync<TResult>(Func<Task> action) where TResult : Exception
    {
        try
        { await action(); }
        catch (TResult) { Assertions++; return; }
        throw new Exception($"Expected {typeof(TResult).Name}.");
    }
}
public sealed class ManualTime : TimeProvider
{
    private long timestamp;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => timestamp;
    public void Advance(int milliseconds) => timestamp += TimeSpan.FromMilliseconds(milliseconds).Ticks;
}
public sealed class Outcomes { public int Successes, Faults, Finals; }
public readonly struct OutcomeHooks(Outcomes outcomes) : ICallHooks<Unit>
{
    public Unit Enter() => default;
    public void Success(ref Unit token, int candidateCount) => outcomes.Successes++;
    public void Fault(ref Unit token, Exception exception) => outcomes.Faults++;
    public void Finally(ref Unit token) => outcomes.Finals++;
}
public sealed record ThrowingMutator : RoleSlice.SingleCandidateMutator<RealVector>
{
    public override RealVector MutateCandidate(RealVector parent, IRandomNumberGenerator random) => throw new InvalidOperationException("probe");
}
public sealed record StepState(int Step) : SearchState;
public sealed class StreamProbe(ManualTime time)
{
    public ManualTime Time { get; } = time;
    public int Disposals;
}
public sealed record TimedAlgorithm(StreamProbe Probe) : RoleSlice.Algorithm<RealVector, StepState>
{
    public bool FailAtEnd { get; init; }
    public override ExecutionFactory<IAlgorithmExecution<RealVector, TSearchSpace, TProblem, StepState>> CreateExecutionFactory<TSearchSpace, TProblem>() => _ => new Execution<TSearchSpace, TProblem>(Probe, FailAtEnd);
    private sealed class Execution<TSearchSpace, TProblem>(StreamProbe probe, bool failAtEnd) : IAlgorithmExecution<RealVector, TSearchSpace, TProblem, StepState>
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace>
    {
        public async IAsyncEnumerable<StepState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, StepState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            try
            {
                ct.ThrowIfCancellationRequested();
                probe.Time.Advance(5);
                yield return new(1);
                ct.ThrowIfCancellationRequested();
                probe.Time.Advance(7);
                yield return new(2);
                ct.ThrowIfCancellationRequested();
                probe.Time.Advance(3);
                if (failAtEnd)
                    throw new InvalidOperationException("stream failure");
            }
            finally { probe.Disposals++; }
        }
    }
}
public sealed record MutationProbeAlgorithm(IOperator<RealVector, MutationRole> Mutator) : RoleSlice.Algorithm<RealVector, PopulationState<RealVector>>
{
    public override ExecutionFactory<IAlgorithmExecution<RealVector, TSearchSpace, TProblem, PopulationState<RealVector>>> CreateExecutionFactory<TSearchSpace, TProblem>() => scope =>
        new Execution<TSearchSpace, TProblem>(new TypedScope<RealVector, TSearchSpace, TProblem>(scope).Resolve(Mutator));
    private sealed class Execution<TSearchSpace, TProblem>(IMutatorExecution<RealVector, TSearchSpace, TProblem> mutator) : IAlgorithmExecution<RealVector, TSearchSpace, TProblem, PopulationState<RealVector>>
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace>
    {
        public async IAsyncEnumerable<PopulationState<RealVector>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, PopulationState<RealVector>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.CompletedTask;
            ct.ThrowIfCancellationRequested();
            var result = mutator.Mutate([new RealVector(4.0)], random, problem.SearchSpace, problem);
            yield return Population.From(result.ToEvaluated(problem.Evaluate(result, random))).ToPopulationState();
        }
    }
}
public sealed class OtherSpace : ISearchSpace<RealVector> { public bool Contains(RealVector candidate) => true; }
public sealed class OtherProblem : IProblem<RealVector, OtherSpace>
{
    public OtherSpace SearchSpace { get; } = new();
    public ObjectiveDirections Objective => SingleObjective.Create(ObjectiveDirection.Minimize);
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) => candidates.Select(_ => (ObjectiveVector)0.0).ToArray();
}
public sealed class CyclicMutation : IOperator<RealVector, MutationRole>
{
    public PreparedFactory Prepare<TSearchSpace, TProblem>(RoleBinding<RealVector, MutationRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace> => binding.Forward(this);
}
public sealed record ContractProbeMutator : RoleSlice.StatelessMutator<RealVector>, IOperatorContract<RealVector>
{
    public bool? Ensures(ICandidateInvariant<RealVector> invariant) => invariant is RealVectorBounds ? false : null;
    public IReadOnlyList<ICandidateInvariant<RealVector>> Requires => [new RealVectorLength(2)];
    public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) => parents;
}
public sealed class DishonestMutation : IOperator<RealVector, MutationRole>
{
    public PreparedFactory Prepare<TSearchSpace, TProblem>(RoleBinding<RealVector, MutationRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace> => PreparedFactory.Raw<IEvaluatorExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>>(_ => new RoleSlice.ProblemEvaluator<RealVector>());
}
public sealed class DualRoleOperator : IOperator<RealVector, MutationRole>, IOperator<RealVector, EvaluationRole>
{
    PreparedFactory IOperator<RealVector, MutationRole>.Prepare<TSearchSpace, TProblem>(RoleBinding<RealVector, MutationRole, TSearchSpace, TProblem> binding) =>
        PreparedFactory.Raw<IMutatorExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>>(_ => new IdentityMutator<RealVector>());
    PreparedFactory IOperator<RealVector, EvaluationRole>.Prepare<TSearchSpace, TProblem>(RoleBinding<RealVector, EvaluationRole, TSearchSpace, TProblem> binding) =>
        PreparedFactory.Raw<IEvaluatorExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>>(_ => new RoleSlice.ProblemEvaluator<RealVector>());
}

public static class Scenarios
{
    private static TestFunctionProblem Problem(int dimension = 3) => new(new SphereFunction(dimension));
    private static IRandomNumberGenerator Rng(int seed = 17) => RandomNumberGenerator.Create(seed);
    private static TypedScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Typed(ResolutionScope scope) => new(scope);
    private static AlgorithmBinding<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TState> AlgorithmBinding<TState>() where TState : class, ISearchState => new();
    private static async Task<List<TResult>> Collect<TResult>(IAsyncEnumerable<TResult> stream) { var result = new List<TResult>(); await foreach (var item in stream) result.Add(item); return result; }
    private static double First(PopulationState<RealVector> state) => state.Population.EvaluatedCandidates[0].Candidate[0];
    private static RoleSlice.GeneticAlgorithm<RealVector> GA(IOperator<RealVector, MutationRole>? mutation = null, IOperator<RealVector, EvaluationRole>? evaluation = null) =>
        RoleSlice.GeneticAlgorithm.Create(new UniformCreator(), new FirstParentCrossover<RealVector>(), mutation ?? new RoleSlice.GaussianMutator(1, .3)) with
        {
            Evaluator = evaluation ?? new RoleSlice.ProblemEvaluator<RealVector>(),
            PopulationSize = 8,
            MaximumGenerations = 5,
            Elites = 1
        };

    private static void FluentAndProductionLeaves()
    {
        IOperator<RealVector, MutationRole> fluent = new RoleSlice.GaussianMutator(1, .5).Retry(2).Named("variation");
        IOperator<RealVector, EvaluationRole> evaluation = new RoleSlice.ProblemEvaluator<RealVector>().Retry(2).Named("fitness");
        Check.That(GA(fluent, evaluation).Mutator == fluent);
        var problem = Problem();
        var parents = Enumerable.Range(0, 32).Select(i => new RealVector(i / 10.0, -1.0, 2.0)).ToArray();
        var before = parents.Select(parent => parent.ToArray()).ToArray();
        foreach (var rate in new[] { -1.0, 0.0, .3, 1.0, double.NaN })
            foreach (var seed in new[] { 2, 17, 101 })
            {
                var source = new RoleSlice.GaussianMutator(rate, .5);
                var execution = Typed(ResolutionScope.Create()).Resolve(source);
                var actual = execution.Mutate(parents, Rng(seed), problem.SearchSpace, problem);
                var oldSource = new HEAL.HeuristicLib.Encodings.RealVectors.GaussianMutator(rate, .5);
                var old = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(oldSource);
                Check.Sequence(old.Mutate(parents, Rng(seed), problem.SearchSpace, problem), actual);
                Check.That(actual.All(problem.SearchSpace.Contains));
            }
        for (var i = 0; i < parents.Length; i++)
            Check.Sequence(before[i], parents[i]);
    }
    private static void Batching()
    {
        var problem = Problem();
        var parents = Enumerable.Range(0, 50).Select(_ => new RealVector(1.0, 2.0, 3.0)).ToArray();
        var source = new RoleSlice.GaussianMutator(1, .3);
        var sequential = Typed(ResolutionScope.Create()).Resolve(source).Mutate(parents, Rng(), problem.SearchSpace, problem);
        var concurrent = Typed(ResolutionScope.Create()).Resolve(source with { Concurrency = ExecutionConcurrency.Concurrent(3) }).Mutate(parents, Rng(), problem.SearchSpace, problem);
        Check.Sequence(sequential, concurrent);
        var throwing = new ThrowingMutator();
        Check.Throws<InvalidOperationException>(() => Typed(ResolutionScope.Create()).Resolve(throwing).Mutate([parents[0]], Rng(), problem.SearchSpace, problem));
        Check.Throws<AggregateException>(() => Typed(ResolutionScope.Create()).Resolve(throwing with { Concurrency = ExecutionConcurrency.Concurrent(2) }).Mutate([parents[0]], Rng(), problem.SearchSpace, problem));
    }
    private static void StateAndControls()
    {
        var problem = Problem(1);
        var source = new ProgressiveScaleMutator();
        var scope = ResolutionScope.Create();
        double Invoke(IOperator<RealVector, MutationRole> configuration, ResolutionScope current) => Typed(current).Resolve(configuration).Mutate([new RealVector(4.0)], Rng(), problem.SearchSpace, problem)[0][0];
        Check.Equal(2.0, Invoke(source, scope));
        Check.Equal(4.0 / 3, Invoke(source.Named("same state"), scope));
        Check.Equal(1.0, Invoke(source, scope.With(source.CountCalls(out var calls))));
        Check.Equal(1, calls.CurrentCount);
        Check.Equal(2.0, Invoke(source with { }, scope));
        Check.Equal(2.0, Invoke(source, ResolutionScope.Create()));
        var parent = ResolutionScope.Create();
        var child = parent.CreateChildScope();
        Check.Equal(2.0, Invoke(source, child));
        Check.Equal(2.0, Invoke(source, parent));
        Check.Equal(4.0 / 3, Invoke(source, child));

        var gaussian = new RoleSlice.GaussianMutator(1, 2);
        var named = gaussian.Retry(2).Named("controlled");
        var controlledScope = ResolutionScope.Create(gaussian.CountCalls(out var innerCalls), named.CountCalls(out var outerCalls));
        var execution = Typed(controlledScope).Resolve<IMutationStrengthControl>(named, out var control);
        Check.That(control is not null);
        control!.CurrentMutationStrength = 0;
        RealVector candidate = new(1.0);
        Check.Equal(candidate, execution.Mutate([candidate], Rng(), problem.SearchSpace, problem)[0]);
        Typed(controlledScope).Resolve<IMutationStrengthControl>(gaussian, out var directControl);
        Check.Equal(0.0, directControl!.CurrentMutationStrength);
        Check.Equal(1, innerCalls.CurrentCount);
        Check.Equal(1, outerCalls.CurrentCount);
        Check.That(ReferenceEquals(((IOperator<RealVector, MutationRole>)gaussian).CandidateContract, ((IOperator<RealVector, MutationRole>)named).CandidateContract));
        Check.Equal(true, ((IOperator<RealVector, MutationRole>)named).CandidateContract!.Ensures(new RealVectorLength(1)));
        Typed(ResolutionScope.Create()).Resolve<IMutationStrengthControl>(gaussian.OrCall(new RoleSlice.GaussianMutator(1, 4)), out var ambiguous);
        Check.That(ambiguous is null);
        var validChoice = gaussian.OrCall(new RoleSlice.GaussianMutator(1, 4));
        Check.Equal(true, ((IOperator<RealVector, MutationRole>)validChoice).CandidateContract!.Ensures(new RealVectorLength(1)));
        var broken = new ContractProbeMutator().Retry(2).Named("contract survives");
        var problems = CandidateCompatibility.Check(broken, problem.SearchSpace);
        Check.Equal(2, problems.Count);
        Check.That(problems.Any(issue => issue.Reason == IncompatibilityReason.InputMayNotBeAccepted));
        Check.That(problems.Any(issue => issue.Reason == IncompatibilityReason.OutputMayLeaveTheSearchSpace));
        var mixed = gaussian.OrCall(broken);
        Check.Equal(false, ((IOperator<RealVector, MutationRole>)mixed).CandidateContract!.Ensures(new RealVectorBounds(problem.SearchSpace.Minimum, problem.SearchSpace.Maximum)));
    }
    private static void RetryAndAdvice()
    {
        var problem = Problem(1);
        var source = new FlakyMutator<RealVector>(1);
        var wrapped = source.Retry(2).Named("retry");
        var outcomes = new Outcomes();
        var outcomeAspect = Aspect.Create(Pointcut.Reference<IOperator<RealVector, MutationRole>>(source), new HookAdvice<RealVector, MutationRole, OutcomeHooks, Unit>(new(outcomes)));
        var scope = ResolutionScope.Create(outcomeAspect, source.CountCalls(out var childCalls), wrapped.CountCalls(out var calls));
        var result = Typed(scope).Resolve(wrapped).Mutate([new RealVector(1.0)], Rng(), problem.SearchSpace, problem);
        Check.Equal(1, result.Count);
        Check.Equal(1, outcomes.Successes);
        Check.Equal(1, outcomes.Faults);
        Check.Equal(2, outcomes.Finals);
        Check.Equal(1, childCalls.CurrentCount);
        Check.Equal(1, calls.CurrentCount);
        Check.Throws<RetryableFailure>(() => Typed(ResolutionScope.Create()).Resolve(new FlakyMutator<RealVector>(2).Retry(1)).Mutate([], Rng(), problem.SearchSpace, problem));
        Check.Throws<InvalidOperationException>(() => Typed(ResolutionScope.Create()).Resolve(source.Retry(0)));
        var counted = new IdentityMutator<RealVector>();
        var aspect = counted.CountCalls(out var deduplicated);
        Typed(ResolutionScope.Create(aspect, aspect)).Resolve(counted).Mutate([], Rng(), problem.SearchSpace, problem);
        Check.Equal(1, deduplicated.CurrentCount);
    }
    private static void FiveRolesAndRouting()
    {
        var problem = Problem(1);
        var creator = new UniformCreator();
        var mutation = new ScaleMutator(.5);
        var evaluator = new RoleSlice.ProblemEvaluator<RealVector>();
        var crossover = new FirstParentCrossover<RealVector>();
        var selector = new RoleSlice.TournamentSelector<RealVector>(2);
        var createClock = new CandidateClock<RealVector, CreationRole>(creator);
        var mutateClock = new CandidateClock<RealVector, MutationRole>(mutation);
        var evalClock = new CandidateClock<RealVector, EvaluationRole>(evaluator);
        var crossClock = new CandidateClock<RealVector, CrossoverRole>(crossover);
        var selectClock = new CandidateClock<RealVector, SelectionRole>(selector);
        var typed = Typed(ResolutionScope.Create(createClock, mutateClock, evalClock, crossClock, selectClock));
        var candidates = typed.Resolve(creator).Create(4, Rng(), problem.SearchSpace, problem);
        var crossed = typed.Resolve(crossover).Cross(candidates.Select(value => new Parents<RealVector>(value, value)).ToArray(), Rng(), problem.SearchSpace, problem);
        var mutated = typed.Resolve(mutation).Mutate(crossed, Rng(), problem.SearchSpace, problem);
        var evaluated = mutated.ToEvaluated(typed.Resolve(evaluator).Evaluate(mutated, Rng(), problem.SearchSpace, problem));
        typed.Resolve(selector).Select(evaluated, problem.Objective, 3, Rng(), problem.SearchSpace, problem);
        Check.Sequence(new[] { 4, 4, 4, 4, 3 }, new[] { createClock.Counter.CurrentCount, mutateClock.Counter.CurrentCount, evalClock.Counter.CurrentCount, crossClock.Counter.CurrentCount, selectClock.Counter.CurrentCount });

        var parents = Enumerable.Range(0, 64).Select(_ => new RealVector(2.0)).ToArray();
        IOperator<RealVector, MutationRole> zero = new ScaleMutator(0), keep = new ScaleMutator(1);
        var whole = Typed(ResolutionScope.Create()).Resolve(zero.OrCall(keep)).Mutate(parents, Rng(123), problem.SearchSpace, problem);
        var perItem = Typed(ResolutionScope.Create()).Resolve(new ChoosePerCandidateMutator<RealVector>([zero, keep])).Mutate(parents, Rng(123), problem.SearchSpace, problem);
        Check.Equal(1, whole.Select(candidate => candidate[0]).Distinct().Count());
        Check.Equal(2, perItem.Select(candidate => candidate[0]).Distinct().Count());
        Check.Equal(.25, Typed(ResolutionScope.Create()).Resolve(new ScaleMutator(.5).Then(new ScaleMutator(.5))).Mutate([new RealVector(1.0)], Rng(), problem.SearchSpace, problem)[0][0]);
        var transformed = creator.TransformWith(mutation).Named("created and transformed");
        var transformedResult = Typed(ResolutionScope.Create()).Resolve(transformed).Create(8, Rng(), problem.SearchSpace, problem);
        Check.That(transformedResult.All(candidate => Math.Abs(candidate[0]) <= 2.56));
        var input = new List<IOperator<RealVector, MutationRole>> { zero, keep };
        var snapshot = new ChooseCall<RealVector, MutationRole>(input);
        input.Clear();
        Check.Equal(2, snapshot.ChildOperators.Count);
    }
    private static void TypedObservationAndMismatch()
    {
        var problem = Problem(1);
        var evaluator = new RoleSlice.ProblemEvaluator<RealVector>().Named("fitness");
        var counter = new CountAccumulator();
        EvaluationObservation<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>? captured = null;
        var observed = evaluator.ObserveEvaluations((EvaluationObservation<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> observation) => { captured = observation; Check.Equal(2, counter.CurrentCount); });
        var source = Typed(ResolutionScope.Create(evaluator.CountCandidates(counter), observed)).Resolve(evaluator);
        RealVector[] candidates = [new(1.0), new(2.0)];
        source.Evaluate(candidates, Rng(), problem.SearchSpace, problem);
        Check.That(ReferenceEquals(evaluator, captured!.Source));
        Check.That(ReferenceEquals(problem, captured.Problem));
        Check.That(ReferenceEquals(candidates, captured.Candidates));
        Check.Equal(4.0, captured.Objectives[1][0]);
        var otherBinding = new EvaluationBinding<RealVector, OtherSpace, OtherProblem>();
        Check.Throws<InvalidOperationException>(() => otherBinding.Resolve(ResolutionScope.Create(observed), evaluator));
        var gaussian = new RoleSlice.GaussianMutator(1, 1);
        Check.That(!gaussian.Fits(ExecutionSignature.For<OtherSpace, OtherProblem, PopulationState<RealVector>>()));
        Check.Throws<InvalidOperationException>(() => new MutationBinding<RealVector, OtherSpace, OtherProblem>().Resolve(ResolutionScope.Create(), gaussian));
        Check.Throws<InvalidOperationException>(() => Typed(ResolutionScope.Create()).Resolve(new CyclicMutation()));
        Check.Throws<InvalidOperationException>(() => Typed(ResolutionScope.Create()).Resolve(new DishonestMutation()));
    }
    private static async Task GeneticAlgorithmAndAnalysis()
    {
        var problem = Problem();
        var evaluator = new RoleSlice.ProblemEvaluator<RealVector>().Named("fitness");
        var mutation = new RoleSlice.GaussianMutator(1, .3).Retry(2).Named("variation");
        var algorithm = GA(mutation, evaluator).Named("search");
        var clock = new CandidateClock<RealVector, EvaluationRole>(evaluator);
        var trace = algorithm.TraceBestQuality(clock);
        var secondTrace = algorithm.TraceBestQuality(clock);
        var run = algorithm.PrepareRun(problem, Rng()).AddAspect(trace).AddAspect(secondTrace);
        var actual = await Collect(run.Stream());
        Check.Equal(RunLifecycleState.Completed, run.LifecycleState);
        Check.Equal(5, actual.Count);
        Check.Equal(5, trace.SampleCount);
        Check.Equal(40, clock.Counter.CurrentCount);
        Check.Sequence(new[] { 8, 16, 24, 32, 40 }, trace.By(clock).Select(point => point.Time));
        var values = trace.Snapshot().Values.Select(objective => objective[0]).ToArray();
        Check.That(values.Zip(values.Skip(1)).All(pair => pair.Second <= pair.First));

        var oldAlgorithm = new HEAL.HeuristicLib.Algorithms.GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = new SelectFirstParentCrossover<RealVector>(),
            Mutator = new HEAL.HeuristicLib.Encodings.RealVectors.GaussianMutator(1, .3),
            Selector = new HEAL.HeuristicLib.Operators.TournamentSelector<RealVector>(2),
            PopulationSize = 8,
            MaximumGenerations = 5,
            Elites = 1,
            MutationRate = 1
        };
        var oldRun = new AlgorithmRun<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(oldAlgorithm, problem, Rng());
        var expected = await Collect(oldRun.Stream());
        for (var i = 0; i < actual.Count; i++)
            Check.Sequence(expected[i].Population.EvaluatedCandidates, actual[i].Population.EvaluatedCandidates);
        var disabled = GA() with { MutationRate = 0 };
        var mutationCalls = disabled.Mutator.CountCalls(out var count);
        await disabled.PrepareRun(problem, Rng()).AddAspect(mutationCalls).CompleteAsync();
        Check.Equal(0, count.CurrentCount);
    }
    private static async Task MetaAlgorithmsAndRetainedContexts()
    {
        var problem = Problem(1);
        foreach (var policy in new[] { ChildScopePolicy.Fresh, ChildScopePolicy.Retained, ChildScopePolicy.Shared })
        {
            var probe = new MutationProbeAlgorithm(new ProgressiveScaleMutator());
            var cycle = new Cycle<RealVector, PopulationState<RealVector>>([probe]) { MaximumCycles = 2, ScopePolicy = policy };
            var execution = AlgorithmBinding<PopulationState<RealVector>>().Resolve(ResolutionScope.Create(), cycle).Operation;
            var states = await Collect(execution.RunStreamingAsync(problem, Rng()));
            Check.Sequence(policy == ChildScopePolicy.Fresh ? new[] { 2.0, 2.0 } : new[] { 2.0, 4.0 / 3 }, states.Select(First));
        }
        var source = new ProgressiveScaleMutator();
        var child = new MutationProbeAlgorithm(source);
        var retained = new Cycle<RealVector, PopulationState<RealVector>>([child]) { MaximumCycles = 2, ScopePolicy = ChildScopePolicy.Retained };
        var root = ResolutionScope.Create();
        var binding = AlgorithmBinding<PopulationState<RealVector>>();
        Check.Sequence(new[] { 2.0, 4.0 / 3 }, (await Collect(binding.Resolve(root, retained).Operation.RunStreamingAsync(problem, Rng()))).Select(First));
        var changed = root.With(source.CountCalls(out var contextualCalls));
        Check.NearSequence(new[] { 1.0, .8 }, (await Collect(binding.Resolve(changed, retained).Operation.RunStreamingAsync(problem, Rng()))).Select(First).ToArray());
        Check.Equal(2, contextualCalls.CurrentCount);
        var ancestor = ResolutionScope.Create();
        Typed(ancestor).Resolve(source).Mutate([new RealVector(4.0)], Rng(), problem.SearchSpace, problem);
        var fresh = retained with { ScopePolicy = ChildScopePolicy.Fresh };
        Check.Sequence(new[] { 4.0 / 3, 1.0 }, (await Collect(binding.Resolve(ancestor, fresh).Operation.RunStreamingAsync(problem, Rng()))).Select(First));

        var evaluation = new RoleSlice.ProblemEvaluator<RealVector>();
        var ga = GA(evaluation: evaluation) with { PopulationSize = 4, MaximumGenerations = 3 };
        var hill = new RoleSlice.HillClimber<RealVector> { Creator = new UniformCreator(), Mutator = new ScaleMutator(.5), Evaluator = evaluation };
        var hybrid = ga.Named("ga").CycleWith(hill.Named("hill")) with { MaximumCycles = 2, ScopePolicy = ChildScopePolicy.Retained };
        var clock = new CandidateClock<RealVector, EvaluationRole>(evaluation);
        var trace = hybrid.TraceBestQuality(clock);
        var all = await Collect(hybrid.PrepareRun(problem, Rng()).AddAspect(trace).Stream());
        Check.Equal(12, all.Count);
        Check.Equal(30, clock.Counter.CurrentCount);
        Check.Equal(12, trace.SampleCount);
        var budgetSource = GA(evaluation: evaluation);
        var budgeted = budgetSource.WithEvaluationBudget(evaluation, 16).Named("budgeted");
        var budgetClock = new CandidateClock<RealVector, EvaluationRole>(evaluation);
        var budgetRun = budgeted.PrepareRun(problem, Rng()).AddAspect(budgetClock);
        Check.Equal(2, (await Collect(budgetRun.Stream())).Count);
        Check.Equal(16, budgetClock.Counter.CurrentCount);
        Check.Equal(RunLifecycleState.Completed, budgetRun.LifecycleState);
        var overshootClock = new CandidateClock<RealVector, EvaluationRole>(evaluation);
        Check.Equal(2, (await Collect(budgetSource.WithEvaluationBudget(evaluation, 10).PrepareRun(problem, Rng()).AddAspect(overshootClock).Stream())).Count);
        Check.Equal(16, overshootClock.Counter.CurrentCount);
        Check.Equal(0, (await Collect(budgetSource.WithEvaluationBudget(evaluation, 0).PrepareRun(problem, Rng()).Stream())).Count);
    }
    private static async Task ActualHostPauseResume()
    {
        var problem = Problem();
        var evaluator = new RoleSlice.ProblemEvaluator<RealVector>();
        var algorithm = GA(evaluation: evaluator).Named("paused");
        var clock = new CandidateClock<RealVector, EvaluationRole>(evaluator);
        var trace = algorithm.TraceBestQuality(clock);
        var run = algorithm.PrepareRun(problem, Rng()).AddAspect(trace);
        await using (var iterator = run.Stream().GetAsyncEnumerator())
            Check.That(await iterator.MoveNextAsync());
        Check.Equal(RunLifecycleState.Paused, run.LifecycleState);
        Check.Equal(1, trace.SampleCount);
        Check.Equal(4, (await Collect(run.Stream())).Count);
        Check.Equal(5, trace.SampleCount);
        Check.Equal(40, clock.Counter.CurrentCount);
        Check.Equal(RunLifecycleState.Completed, run.LifecycleState);
        Check.Equal(0, (await Collect(run.Stream())).Count);
    }
    private static async Task StreamingSuccessFailureCancellationAndDisposal()
    {
        var problem = Problem(1);
        var time = new ManualTime();
        var probe = new StreamProbe(time);
        var source = new TimedAlgorithm(probe).Named("stream");
        var duration = new DurationAccumulator();
        var observations = new List<AlgorithmObservation<RealVector, StepState>>();
        var scope = ResolutionScope.Create(source.MeasureIterations(duration, time), source.ObserveStates(observations.Add));
        var binding = AlgorithmBinding<StepState>();
        var execution = binding.Resolve(scope, source).Operation;
        var enumerable = execution.RunStreamingAsync(problem, Rng());
        Check.Equal(0, observations.Count);
        Check.Equal(TimeSpan.Zero, duration.CurrentDuration);
        await using (var iterator = enumerable.GetAsyncEnumerator())
        {
            Check.That(await iterator.MoveNextAsync());
            Check.Equal(TimeSpan.FromMilliseconds(5), duration.CurrentDuration);
            time.Advance(1000);
            Check.That(await iterator.MoveNextAsync());
            Check.Equal(TimeSpan.FromMilliseconds(12), duration.CurrentDuration);
            time.Advance(1000);
            Check.That(!await iterator.MoveNextAsync());
        }
        Check.Equal(TimeSpan.FromMilliseconds(15), duration.CurrentDuration);
        Check.Equal(1, probe.Disposals);
        Check.Sequence(new[] { 1, 2 }, observations.Select(item => item.Sequence));
        Check.That(observations[0].PreviousState is null);
        await Collect(binding.Resolve(scope.With(), source).Operation.RunStreamingAsync(problem, Rng()));
        Check.Sequence(new[] { 1, 2, 3, 4 }, observations.Select(item => item.Sequence));
        Check.That(observations[2].PreviousState is null);

        var failureTime = new ManualTime();
        var failureProbe = new StreamProbe(failureTime);
        var failing = new TimedAlgorithm(failureProbe) { FailAtEnd = true };
        var failureDuration = new DurationAccumulator();
        var count = 0;
        var failure = binding.Resolve(ResolutionScope.Create(failing.MeasureIterations(failureDuration, failureTime), failing.ObserveStates(_ => count++)), failing).Operation;
        await Check.ThrowsAsync<InvalidOperationException>(async () => { await Collect(failure.RunStreamingAsync(problem, Rng())); });
        Check.Equal(2, count);
        Check.Equal(TimeSpan.FromMilliseconds(15), failureDuration.CurrentDuration);
        Check.Equal(1, failureProbe.Disposals);

        var cancelTime = new ManualTime();
        var cancelProbe = new StreamProbe(cancelTime);
        var cancelling = new TimedAlgorithm(cancelProbe);
        using var cancellation = new CancellationTokenSource();
        await using (var iterator = binding.Resolve(ResolutionScope.Create(), cancelling).Operation.RunStreamingAsync(problem, Rng(), ct: cancellation.Token).GetAsyncEnumerator())
        {
            Check.That(await iterator.MoveNextAsync());
            cancellation.Cancel();
            await Check.ThrowsAsync<OperationCanceledException>(async () => { await iterator.MoveNextAsync(); });
        }
        Check.Equal(1, cancelProbe.Disposals);
        var earlyProbe = new StreamProbe(new ManualTime());
        var early = new TimedAlgorithm(earlyProbe);
        await using (var iterator = binding.Resolve(ResolutionScope.Create(), early).Operation.RunStreamingAsync(problem, Rng()).GetAsyncEnumerator())
            Check.That(await iterator.MoveNextAsync());
        Check.Equal(1, earlyProbe.Disposals);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] MakeCollectibleGraph()
    {
        var source = new RoleSlice.GaussianMutator(1, .3);
        var aspect = source.CountCalls(out _);
        var scope = ResolutionScope.Create(aspect);
        Typed(scope).Resolve(source.Named("temporary").Retry(2));
        return [new WeakReference(source), new WeakReference(aspect), new WeakReference(scope)];
    }
    private static void OwnershipLimitsAndCollectibility()
    {
        var dual = new DualRoleOperator();
        IOperator<RealVector, MutationRole> mutation = dual;
        IOperator<RealVector, EvaluationRole> evaluation = dual;
        var scope = ResolutionScope.Create();
        Typed(scope).Resolve(mutation);
        Check.Throws<InvalidOperationException>(() => Typed(scope).Resolve(evaluation));
        Check.That(Typed(ResolutionScope.Create()).Resolve(evaluation) is not null);
        var references = MakeCollectibleGraph();
        for (var i = 0; i < 3; i++)
        { GC.Collect(); GC.WaitForPendingFinalizers(); }
        Check.That(references.All(reference => !reference.IsAlive), "Stateless binding caches must not retain source, aspect or scope.");
    }
    public static async Task Main()
    {
        var tests = new (string Name, Func<Task> Run)[] {
            ("fluent API and bit-identical production Gaussian leaves", () => { FluentAndProductionLeaves(); return Task.CompletedTask; }),
            ("deterministic batching and concurrent one-item failure semantics", () => { Batching(); return Task.CompletedTask; }),
            ("configuration-reference state ownership, control and contract preservation", () => { StateAndControls(); return Task.CompletedTask; }),
            ("retry, success/failure hooks and aspect deduplication", () => { RetryAndAdvice(); return Task.CompletedTask; }),
            ("five roles, whole-call/per-candidate routing, composites and snapshots", () => { FiveRolesAndRouting(); return Task.CompletedTask; }),
            ("typed observations, clocks, mismatches and construction cycles", () => { TypedObservationAndMismatch(); return Task.CompletedTask; }),
            ("production-equivalent GA and real shared-clock trace analysis", GeneticAlgorithmAndAnalysis),
            ("GA/hill meta-algorithm and fresh/retained/shared activation contexts", MetaAlgorithmsAndRetainedContexts),
            ("actual AlgorithmRun pause/resume and completion", ActualHostPauseResume),
            ("stream enumeration, timing, failure, cancellation and disposal", StreamingSuccessFailureCancellationAndDisposal),
            ("multi-role ownership limit and temporary graph collectibility", () => { OwnershipLimitsAndCollectibility(); return Task.CompletedTask; })
        };
        foreach (var test in tests)
        { await test.Run(); Console.WriteLine($"PASS {test.Name}"); }
        Console.WriteLine($"SCENARIOS: {tests.Length} groups; {Check.Assertions} assertions");
    }
}
