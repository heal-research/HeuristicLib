namespace HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;

using Optimization;
using Problems.Partial;
using Random;
using SearchSpaces;

public class CandidateTreeSearchState<T, TS, TP, TM> : GenotypeAwareTreeSearchState<T, TS, TP, TM>
    where TP : class, IPartialSolutionProblem<T, TS>
    where TS : class, ISearchSpace<T>
{
    public CandidateTreeSearchState(T genotype, TreeSearchContext context) : base(context)
    {
        Genotype = genotype;
        Bound = BoundsEvaluator.Evaluate(genotype, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);
        var t = IsTerminal = Problem.IsTerminal(genotype, RandomHelpers.NoRandom);
        if (!t)
            return;

        Quality = Problem.Evaluate([genotype], RandomHelpers.NoRandom)[0];
    }

    public CandidateTreeSearchState(CandidateTreeSearchState<T, TS, TP, TM> parent, TM move) : base(parent.Context)
    {
        var g = Genotype = Applier.Apply(parent.Genotype, move, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);
        Bound = BoundsEvaluator.Evaluate(parent.Bound, parent.Genotype, move, RandomHelpers.NoRandom,
            Problem.SearchSpace, Problem);
        var t = IsTerminal = Problem.IsTerminal(g, RandomHelpers.NoRandom);
        if (!t)
            return;

        if (parent.Quality != null)
            Quality = Evaluator.Evaluate(parent.Quality, parent.Genotype, move, RandomHelpers.NoRandom,
                Problem.SearchSpace, Problem);
        else
            Quality = Problem.Evaluate([g], RandomHelpers.NoRandom)[0];
    }

    protected CandidateTreeSearchState(CandidateTreeSearchState<T, TS, TP, TM> other) : base(other)
    {
        Genotype = other.Genotype;
        Bound = other.Bound;
        IsTerminal = other.IsTerminal;
        Quality = other.Quality;
    }

    protected override ObjectiveVector? Quality { get; }
    protected override ObjectiveVector Bound { get; }
    protected override bool IsTerminal { get; }
    protected override T Genotype { get; }

    protected override CandidateTreeSearchState<T, TS, TP, TM> Copy() => new(this);

    protected override IEnumerable<TM> Branches() =>
        Creator.Moves(Genotype, RandomHelpers.NoRandom, Problem.SearchSpace, Problem);

    protected override CandidateTreeSearchState<T, TS, TP, TM> Branch(TM move) => new(this, move);

    protected override ObjectiveDirections Objective => Problem.Objective;
}
