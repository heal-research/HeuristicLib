//using HEAL.HeuristicLib.Genotypes.Vectors;
//using HEAL.HeuristicLib.Operators;
//using HEAL.HeuristicLib.Optimization;
//using HEAL.HeuristicLib.Problems.Partial;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.SearchSpaces;

//namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

//public static class TreeSearchLibExtensions
//{
//    public static IncrementalBoundTreeSearchState<T, TS, TP, TM, TN> WithIncrementalBound<T, TS, TP, TM, TN>(this GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner)
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//        where TN : IIncrementalBoundNeighborhoodInstance<T, TS, TP, TM>
//        => new(inner);

//    public static IncrementalObjectiveTreeSearchState<T, TS, TP, TM, TN> WithIncrementalObjective<T, TS, TP, TM, TN>(this GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner)
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//        where TN : IIncrementalObjectiveNeighborhoodInstance<T, TS, TP, TM>
//        => new(inner);

//    public static LazyTreeSearchState<TN> WithLazyEvaluation<TN>(this TreeSearchState<TN> inner)
//        => new(inner);

//    public static GenotypeTreeSearchState<T, TS, TP, TM, TN> AsSearchState<T, TS, TP, TM, TN>(this TP problem, TN neighborhood, T startingPoint)
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//        where TN : INeighborhoodInstance<T, TS, TP, TM>
//    {
//        return new GenotypeTreeSearchState<T, TS, TP, TM, TN>(startingPoint, problem, neighborhood);
//    }

//    public static StackedTreeSearchState<T, TS, TP, TM> AsMutableSearchState<T, TS, TP, TM>()
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//    { }
//}

//public class IncrementalBoundTreeSearchState<T, TS, TP, TM, TN>
//    : WrappingGenotypeAwareTreeSearchState<T, TS, TP, TM, TN>
//    where TP : class, IPartialSolutionProblem<T, TS>
//    where TS : class, ISearchSpace<T>
//    where TN : IIncrementalBoundNeighborhoodInstance<T, TS, TP, TM>
//{
//    private readonly T? parent;
//    private readonly TM? move;

//    public IncrementalBoundTreeSearchState(
//        GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner)
//        : base(inner)
//    { }

//    public IncrementalBoundTreeSearchState(
//        GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner,
//        T? parent,
//        TM? move)
//        : base(inner)
//    {
//        this.parent = parent;
//        this.move = move;
//    }

//    protected IncrementalBoundTreeSearchState(
//        IncrementalBoundTreeSearchState<T, TS, TP, TM, TN> other)
//        : base(other)
//    {
//        parent = other.parent;
//        move = other.move;
//    }

//    public override ObjectiveVector Bound()
//    {
//        if (parent is null)
//            return Inner.Bound();

//        return Neighborhood.BoundIncrement(
//            parent!,
//            move!,
//            RandomHelpers.NoRandom,
//            Problem.SearchSpace,
//            Problem) ?? Inner.Bound();
//    }

//    public override IncrementalBoundTreeSearchState<T, TS, TP, TM, TN> Copy() => new(this);

//    public override IncrementalBoundTreeSearchState<T, TS, TP, TM, TN> Branch(TM move) =>
//        new(Inner.Branch(move), Genotype, move);
//}


