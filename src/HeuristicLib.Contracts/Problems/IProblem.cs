using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public interface IProblem<TGenotype, out TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TGenotype>
{
    TSearchSpace SearchSpace { get; }
    Objective Objective { get; }

    IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<TGenotype> genotypes,
        IRandomNumberGenerator random);
}
