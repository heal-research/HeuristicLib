using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems;

public abstract class SingleSolutionProblem<TSolution, TSearchSpace> : Problem<TSolution, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TSolution>
{
    protected SingleSolutionProblem(Objective objective, TSearchSpace searchSpace) : base(objective, searchSpace)
    { }

    public int MaxDegreeOfParallelism { get; init; } = -1;

    public abstract ObjectiveVector Evaluate(TSolution genotype, IRandomNumberGenerator random);

    public override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<TSolution> genotypes,
        IRandomNumberGenerator random) =>
        BatchExecution.Parallel(genotypes, Evaluate, random, MaxDegreeOfParallelism);
}
