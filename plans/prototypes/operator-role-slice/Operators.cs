using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;

namespace RoleSlice;

public sealed record IdentityMutator<TCandidate> : StatelessMutator<TCandidate>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random) => parents;
}
public sealed record ScaleMutator(double Factor) : SingleCandidateMutator<RealVector>
{
    public override RealVector MutateCandidate(RealVector parent, IRandomNumberGenerator random) => RealVector.FromOwnedArray(parent.Select(value => value * Factor).ToArray());
}
public sealed class InvocationState { public int Calls; }
public sealed record ProgressiveScaleMutator : StatefulMutator<RealVector, InvocationState>
{
    protected override InvocationState CreateInitialState() => new();
    protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, InvocationState state, IRandomNumberGenerator random)
    {
        var factor = 1.0 / (++state.Calls + 1);
        return parents.Select(parent => RealVector.FromOwnedArray(parent.Select(value => value * factor).ToArray())).ToArray();
    }
}
public sealed class FailureState(int remaining) { public int Remaining = remaining; }
public sealed record FlakyMutator<TCandidate>(int Failures) : StatefulMutator<TCandidate, FailureState>
{
    protected override FailureState CreateInitialState() => new(Failures);
    protected override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, FailureState state, IRandomNumberGenerator random)
    {
        if (state.Remaining-- > 0)
            throw new RetryableFailure();
        return parents;
    }
}

public sealed record GaussianMutator(double MutationRate, double MutationStrength) : Mutator<RealVector, BoundedRealVectorSearchSpace>, IOperatorContract<RealVector>, IExecutionConcurrencyConfiguration
{
    public ExecutionConcurrency Concurrency { get; init; } = ExecutionConcurrency.Sequential();
    public bool? Ensures(ICandidateInvariant<RealVector> invariant) => invariant is RealVectorLength or RealVectorBounds ? true : null;
    public override ExecutionFactory<IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>> CreateExecutionFactory()
    {
        var state = new StrengthState(MutationStrength);
        return _ => new Execution(MutationRate, Concurrency, state);
    }
    private sealed class StrengthState(double strength) { public double Strength = strength; }
    private sealed class Execution(double rate, ExecutionConcurrency concurrency, StrengthState state)
        : IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>, IMutationStrengthControl
    {
        public double CurrentMutationStrength { get => state.Strength; set => state.Strength = value; }
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace space, IProblem<RealVector, BoundedRealVectorSearchSpace> problem) =>
            BatchExecution.Execute(parents, (space, rate, strength: state.Strength),
                static (parent, itemRandom, arguments) => HEAL.HeuristicLib.Encodings.RealVectors.GaussianMutator.Mutate(parent, itemRandom, arguments.space, arguments.rate, arguments.strength), random, concurrency);
    }
}
public sealed record UniformCreator : SingleCandidateCreator<RealVector, BoundedRealVectorSearchSpace>, IOperatorContract<RealVector>
{
    public bool? Ensures(ICandidateInvariant<RealVector> invariant) => invariant is RealVectorLength or RealVectorBounds ? true : null;
    public override RealVector CreateCandidate(IRandomNumberGenerator random, BoundedRealVectorSearchSpace space) =>
        UniformDistributedCreator.Create(random, space);
}
public sealed record ProblemEvaluator<TCandidate> : StatelessEvaluator<TCandidate>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => problem.Evaluate(candidates, random);
}
public sealed record FirstParentCrossover<TCandidate> : SingleCandidateCrossover<TCandidate>
{
    public override TCandidate CrossParents(Parents<TCandidate> parents, IRandomNumberGenerator random) => parents.Parent1;
}
public sealed record TournamentSelector<TCandidate>(int TournamentSize) : StatelessSelector<TCandidate>
{
    public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
        HEAL.HeuristicLib.Operators.TournamentSelector.Select(population, objective, count, random, TournamentSize);
}

public sealed record TransformedCreator<TCandidate>(IOperator<TCandidate, CreationRole> SourceCreator, IOperator<TCandidate, MutationRole> TransformationMutator) : OperatorConfiguration, IOperator<TCandidate, CreationRole>
{
    public bool Fits(ExecutionSignature signature) => signature.Fits(SourceCreator, TransformationMutator);
    public PreparedFactory Prepare<TSearchSpace, TProblem>(RoleBinding<TCandidate, CreationRole, TSearchSpace, TProblem> binding)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => PreparedFactory.Raw<ICreatorExecution<TCandidate, TSearchSpace, TProblem>>(scope =>
        {
            var typed = new TypedScope<TCandidate, TSearchSpace, TProblem>(scope);
            return new Execution<TSearchSpace, TProblem>(typed.Resolve(SourceCreator), typed.Resolve(TransformationMutator));
        });
    private sealed class Execution<TSearchSpace, TProblem>(ICreatorExecution<TCandidate, TSearchSpace, TProblem> creator, IMutatorExecution<TCandidate, TSearchSpace, TProblem> mutator) : ICreatorExecution<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace space, TProblem problem) => mutator.Mutate(creator.Create(count, random, space, problem), random, space, problem);
    }
}
public sealed record PipelineMutator<TCandidate> : MultiOperator<TCandidate, MutationRole>
{
    public PipelineMutator(IReadOnlyList<IOperator<TCandidate, MutationRole>> children) : base(children) { }
    protected override PreparedComposite CreateCompositeFactory<TSearchSpace, TProblem>(RoleBinding<TCandidate, MutationRole, TSearchSpace, TProblem> binding) =>
        PreparedComposite.From<IMutatorExecution<TCandidate, TSearchSpace, TProblem>>(children => new(new Execution<TSearchSpace, TProblem>([.. children.Select(child => child.Operation)]), NoControls.Instance));
    private sealed class Execution<TSearchSpace, TProblem>(ImmutableArray<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> children) : IMutatorExecution<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace space, TProblem problem)
        {
            var result = parents;
            foreach (var child in children)
                result = child.Mutate(result, random, space, problem);
            return result;
        }
    }
}
public sealed record ChoosePerCandidateMutator<TCandidate> : MultiOperator<TCandidate, MutationRole>
{
    public ChoosePerCandidateMutator(IReadOnlyList<IOperator<TCandidate, MutationRole>> children) : base(children) { }
    protected override PreparedComposite CreateCompositeFactory<TSearchSpace, TProblem>(RoleBinding<TCandidate, MutationRole, TSearchSpace, TProblem> binding)
    {
        if (ChildOperators.Count == 0)
            throw new InvalidOperationException("At least one child is required.");
        return PreparedComposite.From<IMutatorExecution<TCandidate, TSearchSpace, TProblem>>(children => new(new Execution<TSearchSpace, TProblem>(new WeightedRouter<IMutatorExecution<TCandidate, TSearchSpace, TProblem>>([.. children.Select(child => child.Operation)])), NoControls.Instance));
    }
    private sealed class Execution<TSearchSpace, TProblem>(WeightedRouter<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> router) : IMutatorExecution<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace space, TProblem problem) =>
            router.Route(parents, random, (child, batch) => child.Mutate(batch, random, space, problem));
    }
}
public static class OperatorComposition
{
    public static TransformedCreator<TCandidate> TransformWith<TCandidate>(this IOperator<TCandidate, CreationRole> creator, IOperator<TCandidate, MutationRole> mutator) => new(creator, mutator);
    public static PipelineMutator<TCandidate> Then<TCandidate>(this IOperator<TCandidate, MutationRole> first, IOperator<TCandidate, MutationRole> second) => new([first, second]);
}
