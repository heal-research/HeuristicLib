using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record PipelineRefiner<TCandidate>
    : MultiRefiner<TCandidate>
{
    public PipelineRefiner(IReadOnlyList<IRefiner<TCandidate>> childRefiners)
        : base(childRefiners)
    {
    }

    protected override IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IRefinerInstance<TCandidate, TRunSearchSpace, TRunProblem>> childRefiners) =>
        new Instance<TRunSearchSpace, TRunProblem>(childRefiners);

    private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<IRefinerInstance<TCandidate, TSearchSpace, TProblem>> childRefiners)
        : MultiRefinerInstance<TCandidate, TSearchSpace, TProblem>(childRefiners)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static PipelineRefiner<TCandidate> Create<TCandidate>(params IReadOnlyList<IRefiner<TCandidate>> childRefiners) => new(childRefiners);
}

public static class PipelineRefinerExtensions
{
    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public PipelineRefiner<TCandidate> Then(params IReadOnlyList<IRefiner<TCandidate>> followingRefiners) =>
            PipelineRefiner.Create([refiner, .. followingRefiners]);
    }
}
