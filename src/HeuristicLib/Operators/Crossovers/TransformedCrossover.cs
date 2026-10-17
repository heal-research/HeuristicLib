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
public record TransformedCrossover<TCandidate>(ICrossover<TCandidate> SourceCrossover, IOperator<Mutation, TCandidate> TransformationMutator)
    : ICrossover<TCandidate>
{
    public ExecutorFactory<ICrossoverExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> => scope =>
    {
        var typed = scope.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return new Executor<TRunSearchSpace, TRunProblem>(typed.Resolve(SourceCrossover), typed.Resolve(TransformationMutator));
    };

    private sealed class Executor<TSearchSpace, TProblem>(ICrossoverExecutor<TCandidate, TSearchSpace, TProblem> crossover, IMutationExecutor<TCandidate, TSearchSpace, TProblem> mutator)
        : CrossoverExecutor<TCandidate, TSearchSpace, TProblem>
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
    public static TransformedCrossover<TCandidate> Create<TCandidate>(ICrossover<TCandidate> crossover, IOperator<Mutation, TCandidate> mutator) =>
        new(crossover, mutator);
}

public static class TransformedCrossoverExtensions
{
    extension<TCandidate>(ICrossover<TCandidate> crossover)
    {
        public TransformedCrossover<TCandidate> TransformWith(IOperator<Mutation, TCandidate> mutator) =>
            TransformedCrossover.Create(crossover, mutator);
    }
}
