using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.RoarNet;

public interface IRoarNetOperationsProblem<TG, out TS, out TP> : IRoarNetProblem<TG>
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
{
    TP Problem { get; }
    TS SearchSpace { get; }
}
