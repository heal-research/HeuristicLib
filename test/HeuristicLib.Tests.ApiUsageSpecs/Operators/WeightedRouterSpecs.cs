using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Operators;

public class WeightedRouterSpecs
{
    [Fact]
    public void AlgorithmAuthors_CanApplyAMutationRateToAResolvedMutator()
    {
        var problem = FuncProblem.Create(static (int candidate) => candidate, new IntegerSearchSpace(), SingleObjective.Minimize);
        var mutator = ResolutionScope.Create()
            .For<int, IntegerSearchSpace, IProblem<int, IntegerSearchSpace>>()
            .Resolve(new DecrementMutator());
        var mutationRouter = WeightedRouter.AtRate(mutator, 0.5);
        IReadOnlyList<int> offspring = [10, 20, 30, 40];
        var random = RandomNumberGenerator.Create(7);

        var result = mutationRouter.Route(offspring, random, (selected, batch) =>
            selected is null ? batch : selected.Mutate(batch, random, problem.SearchSpace, problem));

        result.Count.ShouldBe(offspring.Count);
        for (var i = 0; i < result.Count; i++)
            result[i].ShouldBeInRange(offspring[i] - 1, offspring[i]);
    }

    [Fact]
    public void CompositeAuthors_CanRouteInputsUniformlyAcrossChoices()
    {
        var router = WeightedRouter.Create(1, 10, 100);
        IReadOnlyList<int> inputs = [1, 2, 3, 4];

        var shifted = router.Route(inputs, RandomNumberGenerator.Create(7), static (offset, batch) => batch.Select(input => input + offset).ToArray());

        shifted.Count.ShouldBe(inputs.Count);
    }

    [Fact]
    public void CompositeAuthors_CanSplitARequestedCountAcrossWeightedChoices()
    {
        var router = WeightedRouter.Create(["common", "rare"], [9.0, 1.0]);

        var created = router.Route(5, RandomNumberGenerator.Create(7), static (kind, batchCount) => Enumerable.Repeat(kind, batchCount).ToArray());

        created.Count.ShouldBe(5);
        created.ShouldAllBe(kind => kind == "common" || kind == "rare");
    }

    private sealed record IntegerSearchSpace : ISearchSpace<int>
    {
        public bool Contains(int candidate) => true;
    }

    private sealed record DecrementMutator : SingleCandidateMutator<int>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random) => parent - 1;
    }
}
