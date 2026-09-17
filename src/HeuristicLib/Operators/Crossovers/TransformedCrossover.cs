using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Crosses a parent batch and then always applies one mutator to the resulting candidate batch.
/// </summary>
/// <remarks>
/// The mutator is invoked for every crossover result batch. A rate controlled mutator may be supplied when conditional mutation is explicitly desired.
/// </remarks>
public record TransformedCrossover<TCandidate>(ICrossover<TCandidate> SourceCrossover, IMutator<TCandidate> TransformationMutator)
    : ICrossover<TCandidate>
{
    public ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return new Instance<TRunSearchSpace, TRunProblem>(resolver.Resolve(SourceCrossover), resolver.Resolve(TransformationMutator));
    }

    private sealed class Instance<TSearchSpace, TProblem>(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> crossover, IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator)
        : CrossoverInstance<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static TransformedCrossover<TCandidate> Create<TCandidate>(ICrossover<TCandidate> crossover, IMutator<TCandidate> mutator) =>
        new(crossover, mutator);
}

public static class TransformedCrossoverExtensions
{
    extension<TCandidate>(ICrossover<TCandidate> crossover)
    {
        public TransformedCrossover<TCandidate> TransformWith(IMutator<TCandidate> mutator) =>
            TransformedCrossover.Create(crossover, mutator);
    }
}
