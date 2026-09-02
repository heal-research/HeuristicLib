using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

/// <remarks>
/// <typeparamref name="TSelf"/> is the problem's own type, named so that a single problem argument yields the problem,
/// the candidate and the search space by inference. A plain <c>TProblem problem</c> parameter leaves the latter two in
/// constraint position, where C# inference does not reach, so the run and factory methods anchor on this base instead.
/// It is the same device <see cref="HEAL.HeuristicLib.Algorithms.IAlgorithm{TCandidate}"/> keeps off its contract and
/// on its authoring base: an aid to authors, not something the interface has to carry.
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
