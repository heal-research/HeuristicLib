using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Creates a candidate batch and then always applies one mutator to the created batch.
/// </summary>
public record TransformedCreator<TCandidate>(ICreator<TCandidate> SourceCreator, IMutator<TCandidate> TransformationMutator)
    : ICreator<TCandidate>
{
    public ICreatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return new Instance<TRunSearchSpace, TRunProblem>(resolver.Resolve(SourceCreator), resolver.Resolve(TransformationMutator));
    }

    private sealed class Instance<TSearchSpace, TProblem>(ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator, IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator)
        : CreatorInstance<TCandidate, TSearchSpace, TProblem>
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
    public static TransformedCreator<TCandidate> Create<TCandidate>(ICreator<TCandidate> creator, IMutator<TCandidate> mutator) =>
        new(creator, mutator);
}

public static class TransformedCreatorExtensions
{
    extension<TCandidate>(ICreator<TCandidate> creator)
    {
        public TransformedCreator<TCandidate> TransformWith(IMutator<TCandidate> mutator) =>
            TransformedCreator.Create(creator, mutator);
    }
}
