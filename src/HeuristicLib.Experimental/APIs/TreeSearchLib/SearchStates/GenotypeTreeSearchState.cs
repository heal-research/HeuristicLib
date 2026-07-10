namespace HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;

using Operators.MoveEvaluators;
using Optimization;
using Problems.Partial;
using Random;
using SearchSpaces;

public class GenotypeTreeSearchState<T, TS, TP, TM> : GenotypeAwareTreeSearchState<T, TS, TP, TM>
    where TP : class, IPartialSolutionProblem<T, TS>
    where TS : class, ISearchSpace<T>
{
    public GenotypeTreeSearchState(T genotype, TreeSearchContext<T, TS, TP, TM> context) : base(genotype, context)
    {
        BoundsEvaluator = Evaluator;

        bounds = BoundsEvaluator.Evaluate(Genotype, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);
    }

    protected GenotypeTreeSearchState(GenotypeTreeSearchState<T, TS, TP, TM> other) : base(other.Genotype, other.Context)
    {
        bounds = other.bounds;
    }

    public override ObjectiveVector? Quality() => IsTerminal() ? Problem.Evaluate([Genotype], RandomHelpers.NoRandom)[0] : null; //Problem.EvaluatePartial(Genotype, RandomHelpers.NoRandom);

    public override ObjectiveVector Bound() => bounds; // no state-specific bound available

    protected ObjectiveVector bounds;

    public IMoveEvaluatorInstance<T, TS, TP, TM> BoundsEvaluator { get; } = null!;

    public override bool IsTerminal() => Problem.IsTerminal(Genotype, RandomHelpers.NoRandom);

    public override GenotypeTreeSearchState<T, TS, TP, TM> Copy() => new(this);

    public override IEnumerable<TM> Branches() => Creator.Moves(Genotype, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);

    public override GenotypeTreeSearchState<T, TS, TP, TM> Branch(TM move)
    {
        var newGenotype = Applier.Apply(Genotype, move, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);
        return new GenotypeTreeSearchState<T, TS, TP, TM>(newGenotype, Context) { bounds = BoundsEvaluator.Evaluate(bounds, Genotype, move, RandomHelpers.NoRandom, Problem.SearchSpace, Problem) };
    }

    public override Objective Objective() => Problem.Objective;
}
