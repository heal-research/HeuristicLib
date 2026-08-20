using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

public sealed record PipelineRefiner<TCandidate, TSearchSpace, TProblem>
    : MultiRefiner<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public PipelineRefiner(IReadOnlyList<IRefiner<TCandidate, TSearchSpace, TProblem>> childRefiners)
        : base(childRefiners)
    {
    }

    protected override MultiRefinerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IRefinerInstance<TCandidate, TSearchSpace, TProblem>> childRefiners) =>
        new Instance(childRefiners);

    private sealed class Instance(ImmutableArray<IRefinerInstance<TCandidate, TSearchSpace, TProblem>> childRefiners)
        : MultiRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiners)
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var current = candidates;
            foreach (var refiner in ChildRefiners)
            {
                current = refiner.Refine(current, random, searchSpace, problem);
            }

            return current;
        }
    }
}

public static class PipelineRefiner
{
    public static PipelineRefiner<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<IRefiner<TCandidate, TSearchSpace, TProblem>> childRefiners)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(childRefiners);
}

public static class PipelineRefinerExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate, TSearchSpace, TProblem> refiner)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public PipelineRefiner<TCandidate, TSearchSpace, TProblem> Then(params IReadOnlyList<IRefiner<TCandidate, TSearchSpace, TProblem>> followingRefiners) =>
            PipelineRefiner.Create([refiner, .. followingRefiners]);
    }
}
