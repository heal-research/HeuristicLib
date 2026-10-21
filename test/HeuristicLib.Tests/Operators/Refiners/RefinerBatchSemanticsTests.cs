using HEAL.HeuristicLib.Inspection;
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

        foreach (var (name, topology, advice) in PassThroughTopologies())
        {
            var refined = ResolutionScope.Create(advice is null ? [] : [advice]).Resolve<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(topology).Refine(population, RandomNumberGenerator.Create(42), Problem.SearchSpace, Problem);

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
        var execution = ResolutionScope.Create().Resolve<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(PipelineRefiner.Create(new DropLastRefiner(), new AddOffsetRefiner(1)));

        Refine(execution, 1, 2, 3).Select(individual => individual.Value).ShouldBe([2, 3]);
    }

    [Fact]
    public void IteratedRefiner_ResizesThePopulationOncePerIteration()
    {
        var execution = ResolutionScope.Create().Resolve<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(new DropLastRefiner().AsIterated(2));

        Refine(execution, 1, 2, 3, 4).Select(individual => individual.Value).ShouldBe([1, 2]);
    }

    [Fact]
    public void InspectionRefiners_ReportThePopulationTheRefinerActuallyReturned()
    {
        var counter = new CountAccumulator();
        var observed = new DropLastRefiner();
        var execution = ResolutionScope.Create(Aspect.CountCandidates(observed, counter)).Resolve<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(observed);

        Refine(execution, 1, 2, 3);

        counter.CurrentCount.ShouldBe(2);
    }

    // This topology assigns each candidate to a child and restores input order, which needs one result per assigned
    // candidate.
    [Fact]
    public void ChoosePerCandidateRefinement_RequiresOneResultPerCandidateAssignedToAChild()
    {
        var execution = ResolutionScope.Create().Resolve<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>>(ChoosePerCandidate.Create(new DropLastRefiner()));

        Should.Throw<InvalidOperationException>(() => Refine(execution, 1, 2, 3));
    }

    private static IEnumerable<(string Name, IOperator<Refinement, Individual> Topology, Aspect? Advice)> PassThroughTopologies()
    {
        var noChange = NoChangeRefiner<Individual>.Instance;

        yield return ("no change", noChange, null);
        yield return ("pipeline", PipelineRefiner.Create(noChange, noChange), null);
        yield return ("empty pipeline", PipelineRefiner.Create<Individual>(), null);
        yield return ("iterated", noChange.AsIterated(3), null);
        yield return ("choose one", ChoosePerCandidate.Create(noChange), null);
        yield return ("rate limited", new AddOffsetRefiner(1).AppliedAtRate(0.0), null);
        yield return ("counting", noChange, Aspect.CountCandidates(noChange, new CountAccumulator()));
        yield return ("duration measuring", noChange, Aspect.MeasureDuration(noChange, new DurationAccumulator()));
        yield return ("improvement checking", noChange.CheckedForImprovement(), null);
        yield return ("single candidate", new IdentityRefiner(), null);
    }

    private static IReadOnlyList<Individual> Refine(IRefinementExecutor<Individual, DummySearchSpace<Individual>, FuncProblem<Individual, DummySearchSpace<Individual>>> executor, params int[] values) =>
        executor.Refine([.. values.Select(value => new Individual(value))], RandomNumberGenerator.Create(42), Problem.SearchSpace, Problem);

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
