using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public record SelectFirstParentCrossover<TCandidate>
  : SingleSolutionCrossover<TCandidate>
{
    public static readonly SelectFirstParentCrossover<TCandidate> Instance = new();

    public override TCandidate Cross(IParents<TCandidate> parents, IRandomNumberGenerator random) => SelectFirstParentCrossover.Cross(parents, random);
}

public static class SelectFirstParentCrossover
{
    public static SelectFirstParentCrossover<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => SelectFirstParentCrossover<TCandidate>.Instance;

    public static TCandidate Cross<TCandidate>(IParents<TCandidate> parents, IRandomNumberGenerator random)
    {
        return parents.Parent1;
    }
}

public record SelectSecondParentCrossover<TCandidate>
  : SingleSolutionCrossover<TCandidate>
{
    public static readonly SelectSecondParentCrossover<TCandidate> Instance = new();

    public override TCandidate Cross(IParents<TCandidate> parents, IRandomNumberGenerator random) => SelectSecondParentCrossover.Cross(parents, random);
}

public static class SelectSecondParentCrossover
{
    public static SelectSecondParentCrossover<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem)
        where TSearchSpace : class, ISearchSpace<TCandidate> => SelectSecondParentCrossover<TCandidate>.Instance;

    public static TCandidate Cross<TCandidate>(IParents<TCandidate> parents, IRandomNumberGenerator random)
    {
        return parents.Parent2;
    }
}
