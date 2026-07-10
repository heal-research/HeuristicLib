//using HEAL.HeuristicLib.Operators;
//using HEAL.HeuristicLib.Problems.Partial;
//using HEAL.HeuristicLib.SearchSpaces;

//namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

//public static class TreeSearchLibExtensions
//{
//    public static IncrementalBoundTreeSearchState<T, TS, TP, TM, TN> WithIncrementalBound<T, TS, TP, TM, TN>(this GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner)
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//        where TN : IIncrementalBoundNeighborhoodInstance<T, TS, TP, TM>
//    {
//        return new IncrementalBoundTreeSearchState<T, TS, TP, TM, TN>(inner);
//    }

//    public static IncrementalObjectiveTreeSearchState<T, TS, TP, TM, TN> WithIncrementalObjective<T, TS, TP, TM, TN>(this GenotypeAwareTreeSearchState<T, TS, TP, TM, TN> inner)
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//        where TN : IIncrementalObjectiveNeighborhoodInstance<T, TS, TP, TM>
//    {
//        return new IncrementalObjectiveTreeSearchState<T, TS, TP, TM, TN>(inner);
//    }

//    public static LazyTreeSearchState<TN> WithLazyEvaluation<TN>(this TreeSearchState<TN> inner)
//    {
//        return new LazyTreeSearchState<TN>(inner);
//    }

//    public static GenotypeTreeSearchState<T, TS, TP, TM, TN> AsSearchState<T, TS, TP, TM, TN>(this TP problem, TN neighborhood, T startingPoint, TS searchSpace, TM move)
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//        where TN : INeighborhoodInstance<T, TS, TP, TM>
//    {
//        return new GenotypeTreeSearchState<T, TS, TP, TM, TN>(startingPoint, problem, neighborhood);
//    }

//    //TODO add custom starting point support
//    public static StackedTreeSearchState<T, TS, TP, TM> AsMutableSearchState<T, TS, TP, TM, TN>(this TP problem, TN neighborhood, IDecisionResolver<T, TM> resolver)
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//        where TN : INeighborhoodInstance<T, TS, TP, TM>
//    {
//        return new StackedTreeSearchState<T, TS, TP, TM>(problem, neighborhood, resolver);
//    }

//    public static TreeSearchStateFactory<T, TS, TP> ToTreeSearch<T, TS, TP>(this TP problem)
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//    {
//        return new TreeSearchStateFactory<T, TS, TP>(problem);
//    }

//    public readonly struct TreeSearchStateFactory<T, TS, TP>
//        where TP : class, IPartialSolutionProblem<T, TS>
//        where TS : class, ISearchSpace<T>
//    {
//        private readonly TP problem;

//        public TreeSearchStateFactory(TP problem)
//        {
//            this.problem = problem;
//        }

//        public GenotypeTreeSearchState<T, TS, TP, TM, TN>
//            From<TM, TN>(TN neighborhood, T startingPoint)
//            where TN : INeighborhoodInstance<T, TS, TP, TM>
//        {
//            return new GenotypeTreeSearchState<T, TS, TP, TM, TN>(
//                startingPoint,
//                problem,
//                neighborhood);
//        }
//    }
//}


