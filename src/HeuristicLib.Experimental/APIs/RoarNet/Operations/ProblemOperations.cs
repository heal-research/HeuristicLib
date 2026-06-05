//using HEAL.HeuristicLib.Operators.Creators;
//using HEAL.HeuristicLib.Operators.Mutators;
//using HEAL.HeuristicLib.Problems;
//using HEAL.HeuristicLib.Problems.MetaOptimization;
//using HEAL.HeuristicLib.Problems.Partial;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.SearchSpaces;

//namespace HEAL.HeuristicLib.APIs.RoarNet;

//public abstract record ProblemOperations<T, TS, TP, TM>(
//    StatelessCreator<T, TS, TP> Creator,
//    INeighborhood<T,TS,TP, TM> LocalNeighborhood,
//    TS SearchSpace,
//    TP Problem,
//    IRandomNumberGenerator rng)
//    : BaseOperations<LazySolution<T>, MutationMove, MutationNeighborhood, TP>, IEvaluationContext<T>
//    where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS>, Problem
//{
//    public override LazySolution<T> apply_move(MutationMove move, LazySolution<T> solution)
//        => new(CompositeSearchSpace<,,,>.Mutator.Mutate([solution.Genotype], rng.Fork(move.forkKey), SearchSpace, Problem)[0], this);

//    public override LazySolution<T> copy_solution(LazySolution<T> solution) => solution.Copy();

//    public override LazySolution<T> heuristic_solution(TP problem) => new(Creator.Create(1, rng, SearchSpace, Problem)[0], this);

//    public override MutationNeighborhood local_neighbourhood(TP problem) => new();

//    public override double? lower_bound(LazySolution<T> solution) => solution.LowerBound();

//    public override double? lower_bound_increment(MutationMove move, LazySolution<T> solution) => apply_move(move, solution).LowerBound() - solution.LowerBound();

//#pragma warning disable S2190
//    public override IEnumerable<MutationMove> moves(MutationNeighborhood neighbourhood, LazySolution<T> solution)
//#pragma warning restore S2190
//    {
//        while (true)
//        {
//            yield return new MutationMove(rng.NextInt());
//        }
//    }

//    public override double? objective_value(LazySolution<T> solution) => solution.Quality();

//    public override double? objective_value_increment(MutationMove move, LazySolution<T> solution) => apply_move(move, solution).Quality() - solution.Quality();

//    public override MutationMove random_move(MutationNeighborhood neighbourhood, LazySolution<T> solution) => new(rng.NextInt());

//    public override LazySolution<T> random_solution(TP problem) => new(Creator.Create(1, rng, SearchSpace, Problem)[0], this);

//    public double? Evaluate(T input, out bool bounded, out double? bound)
//    {
//        if (!Problem.SearchSpace.Contains(input))
//            throw new NotImplementedException();
//        bound = Problem.Evaluate([input], rng)[0][0];
//        bounded = true;
//        return bound;
//    }

//    public double? LowerBound(T input, out bool evaluated, out double? quality) => Evaluate(input, out evaluated, out quality);

//    /// <summary>
//    /// this generator slows down as larger samples are drawn
//    /// algorithms expecting finite neighborhoods will run into issues
//    /// </summary>
//    /// <param name="neighbourhood"></param>
//    /// <param name="solution"></param>
//    /// <returns></returns>
//#pragma warning disable S2190
//    public override IEnumerable<MutationMove> random_moves_without_replacement(MutationNeighborhood neighbourhood, LazySolution<T> solution)
//    {
//        var set = new HashSet<int>();
//        while (true)
//        {
//            var m = random_move(neighbourhood, solution);
//            if (set.Add(m.forkKey))
//                yield return m;
//        }
//        // ReSharper disable once IteratorNeverReturns
//    }
//#pragma warning restore S2190

//    public override LazySolution<T> revert_move(MutationMove move, LazySolution<T> solution) => throw new NotSupportedException();
//}
