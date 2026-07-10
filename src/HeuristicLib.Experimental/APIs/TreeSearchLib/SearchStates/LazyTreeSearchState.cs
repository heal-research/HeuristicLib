//using HEAL.HeuristicLib.Optimization;

//namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

//public class LazyTreeSearchState<T> : TreeSearchState<T>
//{
//    private bool evaluated;
//    private bool bounded;
//    private bool terminal;
//    private ObjectiveVector? quality;
//    private ObjectiveVector? lowerBound;
//    private bool isTerminal;
//    private readonly TreeSearchState<T> treeSearchState;

//    public LazyTreeSearchState(TreeSearchState<T> treeSearchState)
//    {
//        this.treeSearchState = treeSearchState;
//    }

//    protected LazyTreeSearchState(LazyTreeSearchState<T> other)
//    {
//        evaluated = other.evaluated;
//        quality = other.quality;
//        terminal = other.terminal;
//        isTerminal = other.isTerminal;
//        bounded = other.bounded;
//        lowerBound = other.lowerBound;
//        treeSearchState = other.treeSearchState.Copy();
//    }

//    public override ObjectiveVector? Quality()
//    {
//        if (evaluated)
//            return quality;
//        quality = treeSearchState.Quality();
//        evaluated = true;
//        return quality;
//    }

//    public override ObjectiveVector Bound()
//    {
//        if (bounded)
//            return lowerBound!;
//        lowerBound = treeSearchState.Bound();
//        bounded = true;
//        return lowerBound;
//    }

//    public override bool IsTerminal()
//    {
//        if (terminal)
//            return isTerminal;
//        isTerminal = treeSearchState.IsTerminal();
//        terminal = true;
//        return isTerminal;
//    }

//    public override TreeSearchState<T> Copy() => new LazyTreeSearchState<T>(this);

//    public override IEnumerable<T> Branches() => treeSearchState.Branches();

//    public override TreeSearchState<T> Branch(T move) => new LazyTreeSearchState<T>(treeSearchState.Branch(move));

//    public override Objective Objective() => treeSearchState.Objective();
//}


