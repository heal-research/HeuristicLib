using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <summary>
/// Creates a candidate batch and then always applies one mutator to the created batch.
/// </summary>
[Equatable]
public partial record TransformedCreator<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> Creator, IMutator<TCandidate, TSearchSpace, TProblem> Mutator)
    : Creator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected override CreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(IExecutionInstanceResolver resolver) =>
        new Instance(resolver.Resolve(Creator), resolver.Resolve(Mutator));

    private sealed class Instance(ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator, IMutatorInstance<TCandidate, TSearchSpace, TProblem> mutator)
        : CreatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var candidates = creator.Create(count, random, searchSpace, problem);
            return mutator.Mutate(candidates, random, searchSpace, problem);
        }
    }
}
