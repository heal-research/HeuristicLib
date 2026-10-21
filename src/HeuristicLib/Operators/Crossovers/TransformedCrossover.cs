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
public record TransformedCrossover<TCandidate>(IOperator<Crossover, TCandidate> SourceCrossover, IOperator<Mutation, TCandidate> TransformationMutator)
    : Operator<Crossover, TCandidate>
{
    protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

    private sealed class Factory(TransformedCrossover<TCandidate> source) : OperatorExecutorFactory<Crossover, TCandidate>
    {
        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
            new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(source.SourceCrossover), scope.Resolve(source.TransformationMutator));
    }

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
    public static TransformedCrossover<TCandidate> Create<TCandidate>(IOperator<Crossover, TCandidate> crossover, IOperator<Mutation, TCandidate> mutator) =>
        new(crossover, mutator);
}

public static class TransformedCrossoverExtensions
{
    extension<TCandidate>(IOperator<Crossover, TCandidate> crossover)
    {
        public TransformedCrossover<TCandidate> TransformWith(IOperator<Mutation, TCandidate> mutator) =>
            TransformedCrossover.Create(crossover, mutator);
    }
}
