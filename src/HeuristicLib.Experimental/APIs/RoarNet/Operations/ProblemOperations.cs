//using HEAL.HeuristicLib.Operators.Creators;
//using HEAL.HeuristicLib.Operators.Mutators;
//using HEAL.HeuristicLib.Problems;
//using HEAL.HeuristicLib.Problems.MetaOptimization;
//using HEAL.HeuristicLib.Problems.Partial;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.SearchSpaces;

//namespace HEAL.HeuristicLib.APIs.RoarNet;

//public abstract record ProblemOperations<TCandidate, TSearchSpace, TProblem, TM>(
//    StatelessCreator<TCandidate, TSearchSpace, TProblem> Creator,
//    INeighborhood<TCandidate,TSearchSpace,TProblem, TM> LocalNeighborhood,
//    TSearchSpace SearchSpace,
//    TProblem Problem,
//    IRandomNumberGenerator rng)
//    : BaseOperations<LazySolution<TCandidate>, MutationMove, MutationNeighborhood, TProblem>, IEvaluationContext<TCandidate>
//    where TSearchSpace : class, ISearchSpace<TCandidate> where TProblem : class, IProblem<TCandidate, TSearchSpace>, Problem
//{
//    public override LazySolution<TCandidate> apply_move(MutationMove move, LazySolution<TCandidate> solution)
//        => new(CompositeSearchSpace<,,,>.Mutator.Mutate([solution.Candidate], rng.Fork(move.forkKey), SearchSpace, Problem)[0], this);

//    public override LazySolution<TCandidate> copy_solution(LazySolution<TCandidate> solution) => solution.Copy();

//    public override LazySolution<TCandidate> heuristic_solution(TProblem problem) => new(Creator.Create(1, rng, SearchSpace, Problem)[0], this);

//    public override MutationNeighborhood local_neighbourhood(TProblem problem) => new();

//    public override double? lower_bound(LazySolution<TCandidate> solution) => solution.LowerBound();

//    public override double? lower_bound_increment(MutationMove move, LazySolution<TCandidate> solution) => apply_move(move, solution).LowerBound() - solution.LowerBound();

//#pragma warning disable S2190
//    public override IEnumerable<MutationMove> moves(MutationNeighborhood neighbourhood, LazySolution<TCandidate> solution)
//#pragma warning restore S2190
//    {
//        while (true)
//        {
//            yield return new MutationMove(rng.NextInt());
//        }
//    }

//    public override double? objective_value(LazySolution<TCandidate> solution) => solution.Quality();

//    public override double? objective_value_increment(MutationMove move, LazySolution<TCandidate> solution) => apply_move(move, solution).Quality() - solution.Quality();

//    public override MutationMove random_move(MutationNeighborhood neighbourhood, LazySolution<TCandidate> solution) => new(rng.NextInt());

//    public override LazySolution<TCandidate> random_solution(TProblem problem) => new(Creator.Create(1, rng, SearchSpace, Problem)[0], this);

//    public double? Evaluate(TCandidate input, out bool bounded, out double? bound)
//    {
//        if (!Problem.SearchSpace.Contains(input))
//            throw new NotImplementedException();
//        bound = Problem.Evaluate([input], rng)[0][0];
//        bounded = true;
//        return bound;
//    }

//    public double? LowerBound(TCandidate input, out bool evaluated, out double? quality) => Evaluate(input, out evaluated, out quality);

//    /// <summary>
//    /// this generator slows down as larger samples are drawn
//    /// algorithms expecting finite neighborhoods will run into issues
//    /// </summary>
//    /// <param name="neighbourhood"></param>
//    /// <param name="solution"></param>
//    /// <returns></returns>
//#pragma warning disable S2190
//    public override IEnumerable<MutationMove> random_moves_without_replacement(MutationNeighborhood neighbourhood, LazySolution<TCandidate> solution)
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

//    public override LazySolution<TCandidate> revert_move(MutationMove move, LazySolution<TCandidate> solution) => throw new NotSupportedException();
//}
