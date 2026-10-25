using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Creates a candidate batch and then always applies one mutator to the created batch.
/// </summary>
public record TransformedCreator<TCandidate>(IOperator<Creation, TCandidate> SourceCreator, IOperator<Mutation, TCandidate> TransformationMutator)
    : Operator<Creation, TCandidate>
{
    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        var sourceCreator = scope.Resolve(SourceCreator);
        var transformationMutator = scope.Resolve(TransformationMutator);
        return new Executor<TRunSearchSpace, TRunProblem>(sourceCreator, transformationMutator);
    }

    private sealed class Executor<TSearchSpace, TProblem>(ICreationExecutor<TCandidate, TSearchSpace, TProblem> creator, IMutationExecutor<TCandidate, TSearchSpace, TProblem> mutator)
        : CreationExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var candidates = creator.Create(count, random, searchSpace, problem);
            return mutator.Mutate(candidates, random, searchSpace, problem);
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
