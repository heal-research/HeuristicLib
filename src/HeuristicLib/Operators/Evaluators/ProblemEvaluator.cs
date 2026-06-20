using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record ProblemEvaluator<TGenotype>
    : StatelessEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
    public int MaxDegreeOfParallelism { get; init; } = -1;

    public override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<TGenotype> genotypes,
        IRandomNumberGenerator random,
        ISearchSpace<TGenotype> searchSpace,
        IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
        BatchExecution.Parallel(genotypes, problem.Evaluate, random, MaxDegreeOfParallelism);
}

public static class ProblemEvaluatorExtensions
{
    public static ProblemEvaluator<TGenotype> CreateEvaluator<TGenotype>(
        this IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
        new();
}
