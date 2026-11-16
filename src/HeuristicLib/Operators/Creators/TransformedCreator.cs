using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Creates a candidate batch and then always applies one mutator to the created batch.
/// </summary>
public record TransformedCreator<TCandidate>(IOperator<Creation, TCandidate> SourceCreator, IOperator<Mutation, TCandidate> TransformationMutator)
    : Operator<Creation, TCandidate>
{
    public static Parameter<TransformedCreator<TCandidate>, IOperator<Creation, TCandidate>> SourceCreatorParameter { get; } =
        new(nameof(SourceCreator), static creator => creator.SourceCreator, static (creator, value) => creator with { SourceCreator = value });

    public static Parameter<TransformedCreator<TCandidate>, IOperator<Mutation, TCandidate>> TransformationMutatorParameter { get; } =
        new(nameof(TransformationMutator), static creator => creator.TransformationMutator, static (creator, value) => creator with { TransformationMutator = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [SourceCreatorParameter, TransformationMutatorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        var sourceCreator = scope.Resolve(SourceCreator);
        var transformationMutator = scope.Resolve(TransformationMutator);
        return new Executor(sourceCreator, transformationMutator);
    }

    private sealed class Executor(ICreationExecutor<TCandidate> creator, IMutationExecutor<TCandidate> mutator)
        : CreationExecutor<TCandidate>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random)
        {
            var candidates = creator.Create(count, random);
            return mutator.Mutate(candidates, random);
        }
    }
}

public static class TransformedCreator
{
    public static TransformedCreator<TCandidate> Create<TCandidate>(IOperator<Creation, TCandidate> creator, IOperator<Mutation, TCandidate> mutator) =>
        new(creator, mutator);
}

public static class TransformedCreatorExtensions
{
    extension<TCandidate>(IOperator<Creation, TCandidate> creator)
    {
        public TransformedCreator<TCandidate> TransformWith(IOperator<Mutation, TCandidate> mutator) =>
            TransformedCreator.Create(creator, mutator);
    }
}
