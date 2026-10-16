using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.OperatorRolePrototype;
using HEAL.HeuristicLib.OperatorRolePrototype.Authoring;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace OperatorRoleConsumer;

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

public sealed record CountingMutation : StatefulMutator<RealVector, CountingMutation.ExecutionState>
{
    public sealed class ExecutionState
    {
        public int Calls { get; set; }
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, ExecutionState state,
        IRandomNumberGenerator random)
    {
        state.Calls++;
        return parents.Select(_ => new RealVector((double)state.Calls)).ToArray();
    }
}

public sealed record ContextualMutation(IOperator<RealVector, Mutation> ChildOperator)
    : Mutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    protected override ExecutionFactory<IMutatorExecution<RealVector, BoundedRealVectorSearchSpace,
        TestFunctionProblem>> CreateFactory() =>
        scope => new Execution(scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(ChildOperator));

    private sealed class Execution(IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> child)
        : IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            child.Mutate(parents, random, searchSpace, problem);
    }
}

public sealed class ProbeLog
{
    public int Preparations { get; set; }
    public int Bindings { get; set; }
    public int Constructions { get; set; }
    public bool FailPreparation { get; set; }
    public bool FailBinding { get; set; }
    public bool FailConstruction { get; set; }
}

public interface ILiveValue
{
    int Value { get; set; }
}

public sealed record SharedOperations(ProbeLog Log) : IOperator<RealVector, Mutation>,
    IOperator<RealVector, Evaluation>, IOperator<RealVector, Termination>, IOperator<RealVector, Checksum>
{
    public PreparedOperator Prepare()
    {
        Log.Preparations++;
        if (Log.FailPreparation)
            throw new InvalidOperationException("Requested preparation failure.");
        var execution = new Execution();
        return new Prepared(Log, PreparedFactory.Raw<Execution>(_ =>
        {
            Log.Constructions++;
            if (Log.FailConstruction)
                throw new InvalidOperationException("Requested construction failure.");
            return execution;
        }));
    }

    private sealed class Prepared(ProbeLog log, PreparedFactory factory) : PreparedOperator
    {
        public override PreparedFactory Bind<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
            OperationBinding<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> binding)
        {
            log.Bindings++;
            if (log.FailBinding)
                throw new InvalidOperationException("Requested binding failure.");
            return factory;
        }
    }

    private sealed class Execution : ILiveValue, IChecksumExecution,
        IMutatorExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
        IEvaluatorExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>,
        ITerminatorExecution<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, ISearchState>
    {
        public int Value { get; set; } = 3;
        public int Read() => Value;
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
            ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
            parents.Select(_ => new RealVector((double)Value)).ToArray();
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random,
            ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
            candidates.Select(_ => (ObjectiveVector)(double)Value).ToArray();
        public bool IsTerminalState(ISearchState state, ISearchSpace<RealVector> searchSpace,
            IProblem<RealVector, ISearchSpace<RealVector>> problem) => Value >= 10;
    }
}

public sealed class ProbeState : ISearchState;
public sealed class OtherState : ISearchState;

public sealed record IdentityInterceptor(ProbeLog Log) : IOperator<RealVector, Interception>
{
    public PreparedOperator Prepare()
    {
        Log.Preparations++;
        return PreparedOperator.Raw<Execution>(_ =>
        {
            Log.Constructions++;
            return new Execution();
        });
    }

    private sealed class Execution : IInterceptorExecution<RealVector, ISearchSpace<RealVector>,
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

    protected override ExecutionFactory<IMutatorExecution<RealVector, ISearchSpace<RealVector>,
        IProblem<RealVector, ISearchSpace<RealVector>>>> CreateFactory()
    {
        var sequence = new Sequence();
        return _ => new Execution(++sequence.Value);
    }

    private sealed class Execution(int token) : IMutatorExecution<RealVector, ISearchSpace<RealVector>,
        IProblem<RealVector, ISearchSpace<RealVector>>>
    {
        public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
            ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
            parents.Select(_ => new RealVector((double)token)).ToArray();
    }
}
