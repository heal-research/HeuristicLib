using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures.Authoring;

public abstract record LeafOperator<TCandidate, TOperation, TSearchSpace, TProblem, TExecutor>
    : IOperator<TCandidate, TOperation>
    where TOperation : IOperationDescriptor<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecutor : class, IExecutor
{
    // Nonvirtual public owner entry: explicit authors implement only the protected construction hook.
    public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.FromFactory(CreateFactory());
    protected abstract ExecutorFactory<TExecutor> CreateFactory();

    public bool Fits(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));
}

public abstract record Mutator<TCandidate, TSearchSpace, TProblem>
    : LeafOperator<TCandidate, Mutation, TSearchSpace, TProblem, IMutationExecutor<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Mutator<TCandidate>
    : Mutator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract record StatelessMutator<TCandidate> : Mutator<TCandidate>,
    IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected sealed override ExecutorFactory<IMutationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateFactory() => _ => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> IMutationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>>.Mutate(IReadOnlyList<TCandidate> parents,
        IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => Mutate(parents, random);
}

public abstract record StatefulMutator<TCandidate, TState> : Mutator<TCandidate> where TState : class
{
    protected abstract TState CreateInitialState();
    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state,
        IRandomNumberGenerator random);

    protected sealed override ExecutorFactory<IMutationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulMutator<TCandidate, TState> source, TState state)
        : IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
            ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
            source.Mutate(parents, state, random);
    }
}

public abstract record StatelessEvaluator<TCandidate> : LeafOperator<TCandidate, Evaluation,
    ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>,
    IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>>,
    IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected sealed override ExecutorFactory<IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateFactory() => _ => this;

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates,
        IRandomNumberGenerator random, IProblem<TCandidate, ISearchSpace<TCandidate>> problem);

    IReadOnlyList<ObjectiveVector> IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>>.Evaluate(IReadOnlyList<TCandidate> candidates,
        IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => Evaluate(candidates, random, problem);
}
