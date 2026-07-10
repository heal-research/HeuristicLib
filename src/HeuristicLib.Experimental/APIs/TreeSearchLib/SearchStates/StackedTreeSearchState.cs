//using HEAL.HeuristicLib.Operators;
//using HEAL.HeuristicLib.Optimization;
//using HEAL.HeuristicLib.Problems.Partial;
//using HEAL.HeuristicLib.Random;
//using HEAL.HeuristicLib.SearchSpaces;
//using TreesearchLib;

//namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

//public class StackedTreeSearchState<T, TS, TP, TM> : TreeSearchState<TM>,
//                                                     IMutableState<StackedTreeSearchState<T, TS, TP, TM>, TM, ObjectiveVectorQuality>
//    where TP : class, IPartialSolutionProblem<T, TS>
//    where TS : class, ISearchSpace<T>
//{
//    private readonly Stack<TM> decisions;
//    private bool hasGenotype;
//    private T? genotype;
//    private readonly TP problem;
//    private readonly INeighborhoodInstance<T, TS, TP, TM> neighborhood;
//    private readonly IDecisionResolver<T, TM> resolver;

//    public StackedTreeSearchState(TP problem, INeighborhoodInstance<T, TS, TP, TM> neighborhood, IDecisionResolver<T, TM> resolver)
//    {
//        this.problem = problem;
//        this.neighborhood = neighborhood;
//        this.resolver = resolver;
//        decisions = [];
//    }

//    protected StackedTreeSearchState(StackedTreeSearchState<T, TS, TP, TM> other)
//    {
//        genotype = other.genotype;
//        problem = other.problem;
//        neighborhood = other.neighborhood;
//        hasGenotype = other.hasGenotype;
//        decisions = new Stack<TM>(other.decisions.Reverse());
//        resolver = other.resolver;
//    }

//    public override TreeSearchState<TM> Copy() => new StackedTreeSearchState<T, TS, TP, TM>(this);

//    private T Genotype()
//    {
//        if (hasGenotype)
//            return genotype!;
//        genotype = resolver.Resolve(decisions.Reverse());
//        hasGenotype = true;
//        return genotype;
//    }

//    public override ObjectiveVector? Quality() => problem.EvaluatePartial(Genotype(), RandomHelpers.NoRandom);

//    public override ObjectiveVector Bound() => problem.Objective.Best; // no state-specific bound available

//    public override bool IsTerminal() => problem.IsTerminal(Genotype(), RandomHelpers.NoRandom);

//    public override IEnumerable<TM> Branches() => neighborhood.Moves(Genotype(), RandomHelpers.NoRandom, problem.SearchSpace, problem);

//    public override TreeSearchState<TM> Branch(TM move)
//    {
//        var copy = new StackedTreeSearchState<T, TS, TP, TM>(this);
//        ((IMutableState<StackedTreeSearchState<T, TS, TP, TM>, TM, ObjectiveVectorQuality>)copy).Apply(move);
//        return copy;
//    }

//    public override Objective Objective() => problem.Objective;
//    IEnumerable<TM> IMutableState<StackedTreeSearchState<T, TS, TP, TM>, TM, ObjectiveVectorQuality>.GetChoices() => Branches();

//    void IMutableState<StackedTreeSearchState<T, TS, TP, TM>, TM, ObjectiveVectorQuality>.Apply(TM choice)
//    {
//        decisions.Push(choice);
//        hasGenotype = false;
//        genotype = default;
//    }

//    void IMutableState<StackedTreeSearchState<T, TS, TP, TM>, TM, ObjectiveVectorQuality>.UndoLast()
//    {
//        decisions.Pop();
//        hasGenotype = false;
//        genotype = default;
//    }
//}


