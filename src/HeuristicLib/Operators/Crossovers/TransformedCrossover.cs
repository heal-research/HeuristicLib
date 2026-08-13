using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

/// <summary>
/// Crosses a parent batch and then always applies one mutator to the resulting candidate batch.
/// </summary>
/// <remarks>
/// The mutator is invoked for every crossover result batch. A rate controlled mutator may be supplied when conditional mutation is explicitly desired.
/// </remarks>
public record TransformedCrossover<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> Crossover, IMutator<TCandidate, TSearchSpace, TProblem> Mutator)
    : Crossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override CrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ExecutionInstanceRegistry registry) =>
        new Instance(registry.Resolve(Crossover), registry.Resolve(Mutator));

    private sealed class Instance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover, IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator)
        : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var candidates = crossover.Cross(parents, random, searchSpace, problem);
            return mutator.Mutate(candidates, random, searchSpace, problem);
        }
    }
}

public static class TransformedCrossover
{
    public static TransformedCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover, IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(crossover, mutator);
}

public static class TransformedCrossoverExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public TransformedCrossover<TCandidate, TSearchSpace, TProblem> TransformWith(IMutator<TCandidate, TSearchSpace, TProblem> mutator) =>
            TransformedCrossover.Create(crossover, mutator);
    }
}
