using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.MoveEvaluators;

public abstract record MoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove, TState>
    : IMoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    private sealed record Instance(
        MoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove, TState> MoveEvaluator,
        TState State)
        : IMoveEvaluatorInstance<TGenotype, TSearchSpace, TProblem, TMove>
    {
        public ObjectiveVector Evaluate(
            ObjectiveVector objective,
            TGenotype genotype,
            TMove move,
            IRandomNumberGenerator random,
            TSearchSpace searchSpace,
            TProblem problem)
            => MoveEvaluator.Apply(genotype, move, State, searchSpace, problem, random);

        public ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => throw new NotImplementedException();
    }

    protected abstract ObjectiveVector Apply(
        TGenotype genotype,
        TMove move,
        TState state,
        TSearchSpace searchSpace,
        TProblem problem,
        IRandomNumberGenerator random);

    public virtual IMoveEvaluatorInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(
        ExecutionInstanceRegistry instanceRegistry)
        => new Instance(this, InitialState());

    protected abstract TState InitialState();
}
