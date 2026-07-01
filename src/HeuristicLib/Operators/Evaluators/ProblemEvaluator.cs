using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public record ProblemEvaluator<TGenotype>
    : StatelessEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
    public override IReadOnlyList<Solution<TGenotype>> Evaluate(
        IReadOnlyList<TGenotype> genotypes,
        IRandomNumberGenerator random,
        ISearchSpace<TGenotype> searchSpace,
        IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
        problem.Evaluate(genotypes, random)
            .Select((objectiveVector, index) => Solution.From(genotypes[index], objectiveVector))
            .ToArray();
}

public static class ProblemEvaluatorExtensions
{
    public static ProblemEvaluator<TGenotype> CreateEvaluator<TGenotype>(
        this IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
        new();
}
