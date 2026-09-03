using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

/// <remarks>
/// <typeparamref name="TSelf"/> lets a single problem argument yield the problem, the candidate and the search space
/// by inference, which run and factory methods rely on. Name the deriving type, as
/// <c>TestFunctionProblem : RealVectorProblem&lt;TestFunctionProblem&gt;</c> does.
/// </remarks>
public abstract class Problem<TSelf, TCandidate, TSearchSpace> : IProblem<TCandidate, TSearchSpace>
    where TSelf : Problem<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected Problem(ObjectiveDirections objective, TSearchSpace searchSpace)
    {
        Objective = objective;
        SearchSpace = searchSpace;
    }

    public ObjectiveDirections Objective { get; }
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    public TSearchSpace SearchSpace { get; }
}
