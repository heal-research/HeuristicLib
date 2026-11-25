using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

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
    public static Parameter<TransformedCrossover<TCandidate>, IOperator<Crossover, TCandidate>> SourceCrossoverParameter { get; } =
        new(nameof(SourceCrossover), static crossover => crossover.SourceCrossover, static (crossover, value) => crossover with { SourceCrossover = value });

    public static Parameter<TransformedCrossover<TCandidate>, IOperator<Mutation, TCandidate>> TransformationMutatorParameter { get; } =
        new(nameof(TransformationMutator), static crossover => crossover.TransformationMutator, static (crossover, value) => crossover with { TransformationMutator = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [SourceCrossoverParameter, TransformationMutatorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        var sourceCrossover = scope.Resolve(SourceCrossover);
        var transformationMutator = scope.Resolve(TransformationMutator);
        return new Executor(sourceCrossover, transformationMutator);
    }

    private sealed class Executor(ICrossoverExecutor<TCandidate> crossover, IMutationExecutor<TCandidate> mutator)
        : CrossoverExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random)
        {
            var candidates = crossover.Cross(parents, random);
            return mutator.Mutate(candidates, random);
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
