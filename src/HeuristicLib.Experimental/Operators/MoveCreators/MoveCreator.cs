namespace HEAL.HeuristicLib.Operators.MoveCreators;

using Execution;
using Problems;
using Random;
using SearchSpaces;

public abstract record MoveCreator<TGenotype, TSearchSpace, TProblem, TMove, TState>
    : IMoveCreator<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    private sealed record Instance(MoveCreator<TGenotype, TSearchSpace, TProblem, TMove, TState> MoveCreator, TState State) : IMoveCreatorInstance<TGenotype, TSearchSpace, TProblem, TMove>
    {
        public IEnumerable<TMove> Moves(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => MoveCreator.Moves(genotype, State, searchSpace, problem, random);
    }

    protected abstract IEnumerable<TMove> Moves(TGenotype genotype, TState state, TSearchSpace searchSpace, TProblem problem, IRandomNumberGenerator random);

    public virtual IMoveCreatorInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new Instance(this, InitialState());
    protected abstract TState InitialState();
}
