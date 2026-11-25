using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
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
        IRandomNumberGenerator random) =>
        candidates.Select(candidate => (ObjectiveVector)(candidate[0] * candidate[0])).ToArray();
}

public sealed record CountingMutation : Mutator<RealVector>
{
    protected override IMutationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, TRunSearchState> scope) =>
            new Executor();

    private sealed class Executor : HEAL.HeuristicLib.Operators.Mutators.MutationExecutor<RealVector>
    {
        private int invocations;

        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random)
        {
            invocations++;
            return parents.Select(_ => new RealVector((double)invocations)).ToArray();
        }
    }
}

public sealed record ContextualMutation(IOperator<Mutation, RealVector> ChildOperator)
    : Mutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    protected override IMutationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope)
    {
        var childOperator = scope.Resolve(ChildOperator);
        return new Executor(childOperator);
    }

    private sealed class Executor(IMutationExecutor<RealVector> child)
        : HEAL.HeuristicLib.Operators.Mutators.MutationExecutor<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            child.Mutate(parents, random);
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
    // Each executor owns its live value. The log records construction.
    public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        Log.Preparations++;
        if (Log.FailPreparation)
            throw new InvalidOperationException("Requested preparation failure.");
        Log.Constructions++;
        if (Log.FailConstruction)
            throw new InvalidOperationException("Requested construction failure.");
        return new Executor();
    }

    private sealed class Executor : ILiveValue,
        IChecksumExecutor,
        IMutationExecutor<RealVector>,
        IEvaluationExecutor<RealVector>,
        ITerminationExecutor<ISearchState>
    {
        public int Value { get; set; } = 3;

        public int Read(IRandomNumberGenerator random) => Value;
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            parents.Select(_ => new RealVector((double)Value)).ToArray();
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) =>
            candidates.Select(_ => (ObjectiveVector)(double)Value).ToArray();
        public bool IsTerminalState(ISearchState state, IRandomNumberGenerator random) => Value >= 10;
    }

}

public sealed class ProbeState : ISearchState;
public sealed class OtherState : ISearchState;

public sealed record IdentityInterceptor(ProbeLog Log) : IOperator<Interception<ProbeState>, RealVector>
{
    IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
    {
        Log.Preparations++;
        Log.Constructions++;
        return new Executor();
    }

    private sealed class Executor : InterceptionExecutor<ProbeState>
    {
        public override ProbeState Transform(ProbeState currentState, ProbeState? previousState, IRandomNumberGenerator random) => currentState;
    }

}

// An external test probe numbers constructions without adding shared operator data.
public sealed record NodeTokenMutation(ProbeLog Log) : Mutator<RealVector>
{
    protected override IMutationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, TRunSearchState> scope) =>
        new Executor(++Log.Constructions);

    private sealed class Executor(int token) : HEAL.HeuristicLib.Operators.Mutators.MutationExecutor<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            parents.Select(_ => new RealVector((double)token)).ToArray();
    }
}
