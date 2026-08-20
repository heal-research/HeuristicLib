//namespace HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;

//using Optimization;
//using Problems.Partial;
//using SearchSpaces;

//public abstract class WrappingGenotypeAwareTreeSearchState<T, TS, TP, TM, TN>
//    : GenotypeAwareTreeSearchState<T, TS, TP, TM, TN>
//    where TP : class, IPartialSolutionProblem<T, TS>
//    where TS : class, ISearchSpace<T>
//    where TN : INeighborhoodInstance<T, TS, TP, TM>
//{
//    protected readonly GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> Inner;
//    public override T Genotype => Inner.Genotype;
//    public override TP Problem => Inner.Problem;
//    public override TN Neighborhood => Inner.Neighborhood;

//    protected WrappingGenotypeAwareTreeSearchState(GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner)
//    {
//        Inner = inner;
//    }

//    protected WrappingGenotypeAwareTreeSearchState(WrappingGenotypeAwareTreeSearchState<T, TS, TP, TM, TN> other)
//    {
//        Inner = other.Inner.Copy();
//    }

//    public override ObjectiveVector? Quality() => Inner.Quality();

//    public override ObjectiveVector Bound() => Inner.Bound();

//    public override bool IsTerminal() => Inner.IsTerminal();

//    public abstract override WrappingGenotypeAwareTreeSearchState<T, TS, TP, TM, TN> Copy();

//    public override IEnumerable<TM> Branches() => Inner.Branches();

//    public abstract override WrappingGenotypeAwareTreeSearchState<T, TS, TP, TM, TN> Branch(TM move);

//    public override Objective Objective() => Inner.Objective();
//}
