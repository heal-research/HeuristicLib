using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Crossovers;

public record SelectFirstParentCrossover<TCandidate>
  : SingleSolutionCrossover<TCandidate>
{
    public static readonly SelectFirstParentCrossover<TCandidate> Instance = new();

    public override TCandidate Cross(IParents<TCandidate> parents, IRandomNumberGenerator random) => SelectFirstParentCrossover.Cross(parents, random);
}

public static class SelectFirstParentCrossover
{
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
    public static TCandidate Cross<TCandidate>(IParents<TCandidate> parents, IRandomNumberGenerator random)
    {
        return parents.Parent2;
    }
}
