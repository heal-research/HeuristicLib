using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures.Authoring;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed record IdentityMutation : StatelessMutator<RealVector>
{
    public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents,
        IRandomNumberGenerator random) => parents;
}

public sealed record SphereEvaluation : StatelessEvaluator<RealVector>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates,
        IRandomNumberGenerator random, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
        candidates.Select(candidate => (ObjectiveVector)(candidate[0] * candidate[0])).ToArray();
}

public sealed record CountingMutation : StatefulMutator<RealVector, CountingMutation.State>
{
    public sealed class State
    {
        public int Invocations { get; set; }
    }

    protected override State CreateScopedState() => new();

    protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, State state,
        IRandomNumberGenerator random)
    {
        state.Invocations++;
        return parents.Select(_ => new RealVector((double)state.Invocations)).ToArray();
    }
}

public sealed record ContextualMutation(IOperator<Mutation, RealVector> ChildOperator)
    : Mutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    protected override IMutationExecutor<RealVector, BoundedRealVectorSearchSpace,
        TestFunctionProblem> CreateExecutor(ResolutionScope scope) =>
        new Executor(scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(ChildOperator));

    private sealed class Executor(IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> child)
        : IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            child.Mutate(parents, random, searchSpace, problem);
    }
}

public sealed class ProbeLog
{
    public int Preparations { get; set; }
    public int Constructions { get; set; }
    public bool FailPreparation { get; set; }
    public bool FailConstruction { get; set; }
}

public interface ILiveValue
{
    int Value { get; set; }
}

public sealed record SharedOperations(ProbeLog Log) : IOperator<Mutation, RealVector>,
    IOperator<Evaluation, RealVector>, IOperator<Termination, RealVector>, IOperator<Checksum, RealVector>
{
    // One public method implements every operation's CreateExecutor, whose signature does not name the operation.
    // Every build in one owning scope shares the live value; the log records when the scope creates it.
    public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        var state = scope.GetScopedState(this, () =>
        {
            Log.Preparations++;
            if (Log.FailPreparation)
                throw new InvalidOperationException("Requested preparation failure.");
            return new State();
        });
        Log.Constructions++;
        if (Log.FailConstruction)
            throw new InvalidOperationException("Requested construction failure.");
        return new Executor(state);
    }

    private sealed class State
    {
        public int Value { get; set; } = 3;
    }

    private sealed class Executor(State state) : ILiveValue,
        IChecksumExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
        IMutationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
        IEvaluationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
        ITerminationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, ISearchState>
    {
        public int Value
        {
            get => state.Value;
            set => state.Value = value;
        }

        public int Read(IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace,
            IProblem<RealVector, ISearchSpace<RealVector>> problem) => Value;
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
            ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
            parents.Select(_ => new RealVector((double)Value)).ToArray();
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random,
            ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
            candidates.Select(_ => (ObjectiveVector)(double)Value).ToArray();
        public bool IsTerminalState(ISearchState state, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace,
            IProblem<RealVector, ISearchSpace<RealVector>> problem) => Value >= 10;
    }
}

public sealed class ProbeState : ISearchState;
public sealed class OtherState : ISearchState;

public sealed record IdentityInterceptor(ProbeLog Log) : IOperator<Interception<ProbeState>, RealVector>
{
    IExecutor IOperator<Interception<ProbeState>, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
    {
        scope.GetScopedState(this, () =>
        {
            Log.Preparations++;
            return new Preparation();
        });
        Log.Constructions++;
        return new Executor();
    }

    private sealed class Preparation;

    private sealed class Executor : IInterceptionExecutor<RealVector, ISearchSpace<RealVector>,
        IProblem<RealVector, ISearchSpace<RealVector>>, ProbeState>
    {
        public ProbeState Transform(ProbeState currentState, ProbeState? previousState, IRandomNumberGenerator random,
            ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) => currentState;
    }
}

// Each contextual node has its own token; only the sequence counter is persistent prepared state.
public sealed record NodeTokenMutation : Mutator<RealVector>
{
    private sealed class Sequence
    {
        public int Value { get; set; }
    }

    protected override IMutationExecutor<RealVector, ISearchSpace<RealVector>,
        IProblem<RealVector, ISearchSpace<RealVector>>> CreateExecutor(ResolutionScope scope) =>
        new Executor(++scope.GetScopedState(this, static () => new Sequence()).Value);

    private sealed class Executor(int token) : IMutationExecutor<RealVector, ISearchSpace<RealVector>,
        IProblem<RealVector, ISearchSpace<RealVector>>>
    {
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
            ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
            parents.Select(_ => new RealVector((double)token)).ToArray();
    }
}
