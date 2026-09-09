using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerBatchSemanticsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void EveryTopology_PassesThePopulationThroughWhenItsChildChangesNothing(int populationSize)
    {
        var population = Enumerable.Range(0, populationSize).Select(value => new Individual(value)).ToArray();

        foreach (var (name, topology) in PassThroughTopologies())
        {
            var refined = topology.CreateExecutionInstance<DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(new ExecutionInstanceRegistry()).Refine(population, RandomNumberGenerator.Create(42), Problem.SearchSpace, Problem);

            refined.Count.ShouldBe(population.Length, name);
            for (var index = 0; index < population.Length; index++)
            {
                // Identity rather than equality: elitism, caching keys and improvement checking all compare instances.
                refined[index].ShouldBeSameAs(population[index], name);
            }
        }
    }

    [Fact]
    public void PipelineRefiner_PassesAResizedPopulationToTheNextStage()
    {
        var instance = PipelineRefiner.Create(new DropLastRefiner(), new AddOffsetRefiner(1))
            .CreateExecutionInstance<DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(new ExecutionInstanceRegistry());

        Refine(instance, 1, 2, 3).Select(individual => individual.Value).ShouldBe([2, 3]);
    }

    [Fact]
    public void IteratedRefiner_ResizesThePopulationOncePerIteration()
    {
        var instance = new DropLastRefiner().AsIterated(2).CreateExecutionInstance<DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(new ExecutionInstanceRegistry());

        Refine(instance, 1, 2, 3, 4).Select(individual => individual.Value).ShouldBe([1, 2]);
    }

    [Fact]
    public void InstrumentationRefiners_ReportThePopulationTheRefinerActuallyReturned()
    {
        var counter = new ObservationCounter();
        IReadOnlyList<Individual>? observedRefined = null;
        IReadOnlyList<Individual>? observedCandidates = null;
        var instance = new DropLastRefiner()
            .CountCandidates(counter)
            .ObserveWith<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>((refined, candidates, _, _) =>
            {
                observedRefined = refined;
                observedCandidates = candidates;
            })
            .CreateExecutionInstance<DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(new ExecutionInstanceRegistry());

        Refine(instance, 1, 2, 3);

        counter.CurrentCount.ShouldBe(2);
        observedRefined!.Count.ShouldBe(2);
        observedCandidates!.Count.ShouldBe(3);
    }

    // This topology assigns each candidate to a child and restores input order, which needs one result per assigned
    // candidate.
    [Fact]
    public void ChooseOneRefiner_RequiresOneResultPerCandidateAssignedToAChild()
    {
        var instance = ChooseOneRefiner.Create(new DropLastRefiner())
            .CreateExecutionInstance<DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(new ExecutionInstanceRegistry());

        Should.Throw<InvalidOperationException>(() => Refine(instance, 1, 2, 3));
    }

    private static IEnumerable<(string Name, IRefiner<Individual> Topology)> PassThroughTopologies()
    {
        var noChange = NoChangeRefiner<Individual>.Instance;

        yield return ("no change", noChange);
        yield return ("pipeline", PipelineRefiner.Create(noChange, noChange));
        yield return ("empty pipeline", PipelineRefiner.Create<Individual>());
        yield return ("iterated", noChange.AsIterated(3));
        yield return ("choose one", ChooseOneRefiner.Create(noChange));
        yield return ("rate limited", new AddOffsetRefiner(1).WithRate(0.0));
        yield return ("counting", noChange.CountCandidates(new ObservationCounter()));
        yield return ("duration measuring", noChange.MeasureDuration(new ObservationDuration()));
        yield return ("observable", noChange.ObserveWith(_ => { }));
        yield return ("improvement checking", noChange.WithImprovementCheck());
        yield return ("single candidate", new IdentityRefiner());
    }

    private static IReadOnlyList<Individual> Refine(IRefinerInstance<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>> instance, params int[] values) =>
        instance.Refine([.. values.Select(value => new Individual(value))], RandomNumberGenerator.Create(42), Problem.SearchSpace, Problem);

    private static readonly FuncProblem<Individual, DummySearchSpace<Individual>> Problem =
        FuncProblem.Create(static (Individual individual) => individual.Value, DummySearchSpace<Individual>.Instance, SingleObjective.Minimize);

    private sealed record Individual(int Value);

    private sealed record AddOffsetRefiner(int Offset) : SingleCandidateRefiner<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>
    {
        public override Individual RefineCandidate(Individual candidate, IRandomNumberGenerator random, DummySearchSpace<Individual> searchSpace, FuncProblem<Individual, DummySearchSpace<Individual>> problem) =>
            new(candidate.Value + Offset);
    }

    private sealed record IdentityRefiner : SingleCandidateRefiner<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>
    {
        public override Individual RefineCandidate(Individual candidate, IRandomNumberGenerator random, DummySearchSpace<Individual> searchSpace, FuncProblem<Individual, DummySearchSpace<Individual>> problem) =>
            candidate;
    }

    private sealed record DropLastRefiner : StatelessRefiner<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>
    {
        public override IReadOnlyList<Individual> Refine(IReadOnlyList<Individual> candidates, IRandomNumberGenerator random, DummySearchSpace<Individual> searchSpace, FuncProblem<Individual, DummySearchSpace<Individual>> problem) =>
            [.. candidates.Take(Math.Max(0, candidates.Count - 1))];
    }
}
