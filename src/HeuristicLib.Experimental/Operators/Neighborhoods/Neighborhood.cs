namespace HEAL.HeuristicLib.Operators.Neighborhoods;

using MoveAppliers;
using MoveCreators;
using MoveEvaluators;
using Optimization;
using Problems;
using Random;
using SearchSpaces;

public abstract record Neighborhood<TGenotype, TSearchSpace, TProblem, TMove> :
    INeighborhood<TGenotype, TSearchSpace, TProblem, TMove>, IDirectNeighborhood<TGenotype, TSearchSpace, TProblem, TMove>
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public IMoveCreator<TGenotype, TSearchSpace, TProblem, TMove> MoveCreator => new NeighborhoodCreator<TGenotype, TSearchSpace, TProblem, TMove>(this);
    public IMoveApplier<TGenotype, TSearchSpace, TProblem, TMove> MoveApplier => new NeighborhoodApplier<TGenotype, TSearchSpace, TProblem, TMove>(this);
    public IMoveEvaluator<TGenotype, TSearchSpace, TProblem, TMove> MoveEvaluator => new NeighborhoodEvaluator<TGenotype, TSearchSpace, TProblem, TMove>(this);

    public abstract IEnumerable<TMove> Moves(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
    public abstract TGenotype Apply(TGenotype genotype, TMove move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    // this is a trivial implementation that applies and evaluates,
    // override this if you support partial evaluation (which may not require creating a new solution)
    public virtual ObjectiveVector Evaluate(TGenotype before, TMove? move, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
        var d = move is null ? before : Apply(before, move, random, searchSpace, problem);
        return problem.Evaluate([d], random)[0];
    }
}
