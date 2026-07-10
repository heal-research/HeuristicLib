using HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.Partial;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using TreesearchLib;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

public class StackedTreeSearchState<T, TS, TP, TM> : GenotypeAwareTreeSearchState<T, TS, TP, TM>, IMutableState<StackedTreeSearchState<T, TS, TP, TM>, TM, ObjectiveVectorQuality>
    where TP : class, IPartialSolutionProblem<T, TS>
    where TS : class, ISearchSpace<T>
{
    // backing field for Quality (cannot add setter to the override)
    private ObjectiveVector? quality;

    public StackedTreeSearchState(T genotype, TreeSearchContext context) : base(context)
    {
        Genotype = genotype;
        Bound = BoundsEvaluator.Evaluate(genotype, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);
        var t = IsTerminal = Problem.IsTerminal(genotype, RandomHelpers.NoRandom);
        if (!t)
            return;

        quality = Problem.Evaluate([genotype], RandomHelpers.NoRandom)[0];
    }

    public StackedTreeSearchState(StackedTreeSearchState<T, TS, TP, TM> parent, TM move) : base(parent.Context)
    {
        var g = Genotype = Applier.Apply(parent.Genotype, move, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);
        Bound = BoundsEvaluator.Evaluate(parent.Bound, parent.Genotype, move, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);
        var t = IsTerminal = Problem.IsTerminal(g, RandomHelpers.NoRandom);
        if (!t)
            return;

        if (parent.Quality != null)
            quality = Evaluator.Evaluate(parent.Quality, parent.Genotype, move, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);
        else
            quality = Problem.Evaluate([g], RandomHelpers.NoRandom)[0];
    }

    protected StackedTreeSearchState(StackedTreeSearchState<T, TS, TP, TM> other) : base(other)
    {
        Genotype = other.Genotype;
        Bound = other.Bound;
        IsTerminal = other.IsTerminal;
        quality = other.quality;
    }

    protected override ObjectiveVector? Quality => quality;
    protected override ObjectiveVector Bound { get; private set; }
    protected override bool IsTerminal { get; private set; }
    protected override T Genotype { get; private set; }

    protected override StackedTreeSearchState<T, TS, TP, TM> Copy() => new(this);

    protected override IEnumerable<TM> Branches() => Creator.Moves(Genotype, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);

    protected override StackedTreeSearchState<T, TS, TP, TM> Branch(TM move) => new(this, move);

    protected override Objective Objective => Problem.Objective;
    public IEnumerable<TM> GetChoices() => Branches();

    public void Apply(TM choice) => throw new NotImplementedException();

    public void UndoLast() => throw new NotImplementedException();
}
