//using HEAL.HeuristicLib.Operators;
//using HEAL.HeuristicLib.Optimization;
//using HEAL.HeuristicLib.Problems.Partial;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.SearchSpaces;

//namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

//public class IncrementalObjectiveTreeSearchState<T, TS, TP, TM, TN>
//    : WrappingGenotypeAwareTreeSearchState<T, TS, TP, TM, TN>
//    where TP : class, IPartialSolutionProblem<T, TS>
//    where TS : class, ISearchSpace<T>
//    where TN : IIncrementalObjectiveNeighborhoodInstance<T, TS, TP, TM>
//{
//    private readonly T? parent;
//    private readonly TM? move;

//    public IncrementalObjectiveTreeSearchState(GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner) : base(inner) { }

//    public IncrementalObjectiveTreeSearchState(GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner, T? parent, TM? move) : base(inner)
//    {
//        this.parent = parent;
//        this.move = move;
//    }

//    protected IncrementalObjectiveTreeSearchState(IncrementalObjectiveTreeSearchState<T, TS, TP, TM, TN> other) : base(other)
//    {
//        parent = other.parent;
//        move = other.move;
//    }

//    public override ObjectiveVector? Quality()
//    {
//        if (parent is null)
//            return Inner.Quality();
//        return Neighborhood.EvaluateIncrement(
//            parent!,
//            move!,
//            RandomHelpers.NoRandom,
//            Problem.SearchSpace,
//            Problem);
//    }

//    public override IncrementalObjectiveTreeSearchState<T, TS, TP, TM, TN> Copy() => new(this);

//    public override IncrementalObjectiveTreeSearchState<T, TS, TP, TM, TN> Branch(TM move) => new(Inner.Branch(move), Genotype, move);
//}


