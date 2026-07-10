namespace HEAL.HeuristicLib.Operators.MoveCreators;

using Execution;
using Problems;
using Random;
using SearchSpaces;

public abstract record StatelessMoveCreator<TGenotype, TSearchSpace, TProblem, TMove>
    : IMoveCreator<TGenotype, TSearchSpace, TProblem, TMove>, IMoveCreatorInstance<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public virtual IMoveCreatorInstance<TGenotype, TSearchSpace, TProblem, TMove> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
    public abstract IEnumerable<TMove> Moves(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}
