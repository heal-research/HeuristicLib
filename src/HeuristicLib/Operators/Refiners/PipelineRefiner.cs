using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record PipelineRefiner<TCandidate>
    : MultiOperator<Refinement, TCandidate>
{
    public PipelineRefiner(IReadOnlyList<IOperator<Refinement, TCandidate>> childRefiners)
        : base(childRefiners)
    {
    }

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        ImmutableArray<IRefinementExecutor<TCandidate, TRunSearchSpace, TRunProblem>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
        return new Executor<TRunSearchSpace, TRunProblem>(children);
    }

    private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IRefinementExecutor<TCandidate, TSearchSpace, TProblem>> childRefiners)
        : IRefinementExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var current = candidates;
            foreach (var refiner in childRefiners)
            {
                current = refiner.Refine(current, random, searchSpace, problem);
            }

            return current;
        }
    }
}

public static class PipelineRefiner
{
    public static PipelineRefiner<TCandidate> Create<TCandidate>(params IReadOnlyList<IOperator<Refinement, TCandidate>> childRefiners) => new(childRefiners);
}

public static class PipelineRefinerExtensions
{
    extension<TCandidate>(IOperator<Refinement, TCandidate> refiner)
    {
        public PipelineRefiner<TCandidate> Then(params IReadOnlyList<IOperator<Refinement, TCandidate>> followingRefiners) =>
            PipelineRefiner.Create([refiner, .. followingRefiners]);
    }
}
