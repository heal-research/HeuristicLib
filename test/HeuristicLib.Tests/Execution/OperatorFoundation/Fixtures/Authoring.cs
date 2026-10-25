using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures.Authoring;

public abstract record LeafOperator<TOperation, TCandidate, TSearchSpace, TProblem, TExecutor>
    : IOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecutor : class, IExecutor
{
    // Explicit owner entry: authors implement only the protected construction hook.
    IExecutor IOperator<TOperation, TCandidate>.CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) => CreateExecutor(scope);
    protected abstract TExecutor CreateExecutor(ResolutionScope scope);

    public bool Fits(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));
}

public abstract record Mutator<TCandidate, TSearchSpace, TProblem>
    : LeafOperator<Mutation, TCandidate, TSearchSpace, TProblem, IMutationExecutor<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Mutator<TCandidate>
    : Mutator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract record StatelessMutator<TCandidate> : Mutator<TCandidate>,
    IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected sealed override IMutationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) => this;

    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> IMutationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>>.Mutate(IReadOnlyList<TCandidate> parents,
        IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => Mutate(parents, random);
}

public abstract record StatefulMutator<TCandidate, TState> : Mutator<TCandidate> where TState : class
{
    protected abstract TState CreateScopedState();
    protected abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, TState state,
        IRandomNumberGenerator random);

    protected sealed override IMutationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetScopedState(this, CreateScopedState));

    private sealed class Executor(StatefulMutator<TCandidate, TState> source, TState state)
        : IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
    {
        public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
            ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
            source.Mutate(parents, state, random);
    }
}

public abstract record StatelessEvaluator<TCandidate> : LeafOperator<Evaluation, TCandidate,
    ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>,
    IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>>,
    IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    protected sealed override IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) => this;

    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates,
        IRandomNumberGenerator random, IProblem<TCandidate, ISearchSpace<TCandidate>> problem);

    IReadOnlyList<ObjectiveVector> IEvaluationExecutor<TCandidate, ISearchSpace<TCandidate>,
        IProblem<TCandidate, ISearchSpace<TCandidate>>>.Evaluate(IReadOnlyList<TCandidate> candidates,
        IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem) => Evaluate(candidates, random, problem);
}
