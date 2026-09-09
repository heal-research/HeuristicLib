using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Usage;

/// <summary>
/// Validating a configuration before a run starts. The walk reaches every operator a run would reach, including
/// operators nested inside composed ones, and reports each incompatibility with the path that leads to it.
/// </summary>
/// <remarks>
/// Creating a run validates by default, so a configuration reported here does not start. Passing
/// <c>validate: false</c> opts out, for an author deliberately running a configuration whose declared contracts do not
/// hold yet. Calling <c>Validate</c> directly is still worthwhile when the answer is wanted without a run, or wanted
/// as a report rather than as an exception.
/// </remarks>
public class PreflightValidationSpecs
{
    private static FixedCardinalityBoolVectorSearchSpace Constrained => new(length: 4, cardinality: 2);

    private static BoolVectorSearchSpace Unconstrained => new(Length: 4);

    /// <summary>
    /// An operator nested inside a composition is found, and the diagnostic says which one and why.
    /// </summary>
    [Fact]
    public void ValidatingAComposedOperator_ReportsTheNestedOperatorThatCannotBeUsed()
    {
        var pipeline = PipelineMutator.Create(new BitSwapMutator(), new FlipOneBitMutator());

        var report = SearchConfigurationValidation.Validate(pipeline, Constrained);

        report.IsValid.ShouldBeFalse();
        var diagnostic = report.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Path.ShouldContain("ChildMutators[1]");
        diagnostic.Message.ShouldContain(nameof(FlipOneBitMutator));
        diagnostic.Message.ShouldContain("Cardinality(2)");
    }

    /// <summary>
    /// The same configuration is valid over the wider space, because there the invariant it fails to preserve is not
    /// required of members.
    /// </summary>
    [Fact]
    public void TheSameConfiguration_IsValidOverTheWiderSpace()
    {
        var pipeline = PipelineMutator.Create(new BitSwapMutator(), new FlipOneBitMutator());

        SearchConfigurationValidation.Validate(pipeline, Unconstrained).IsValid.ShouldBeTrue();
    }

    /// <summary>
    /// Every failure is reported, not only the first, so one validation pass tells a user everything to fix.
    /// </summary>
    [Fact]
    public void ValidationReportsEveryFailure_AndThrowingListsThemAll()
    {
        var pipeline = PipelineMutator.Create(new FlipOneBitMutator(), new SwapSecondTrueMutator());

        var report = SearchConfigurationValidation.Validate(pipeline, Unconstrained);

        var diagnostic = report.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Path.ShouldEndWith("ChildMutators[1]");
        diagnostic.Message.ShouldContain(nameof(SwapSecondTrueMutator));

        var exception = Should.Throw<InvalidOperationException>(report.ThrowIfInvalid);
        exception.Message.ShouldContain("AtLeastSet(2)");
    }

    /// <summary>
    /// A search space that states no invariants checks nothing, so every configuration written before invariants
    /// existed validates unchanged. Adopting invariant checking is a per space decision.
    /// </summary>
    [Fact]
    public void AnAlgorithmOverASpaceWithoutInvariants_ValidatesUnchanged()
    {
        var problem = new TravelingSalesmanProblem();
        var algorithm = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        algorithm.Validate(problem).IsValid.ShouldBeTrue();
        Should.NotThrow(() => algorithm.ValidateAndThrow(problem));
    }

    /// <summary>
    /// What a caller writes: validate the configuration against the problem, then run it. One call answers for the
    /// whole algorithm, and a clean report is the go ahead.
    /// </summary>
    [Fact]
    public void ValidatingBeforeARun_IsOneCallAgainstTheProblem()
    {
        var problem = new TravelingSalesmanProblem();
        var algorithm = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        var report = algorithm.Validate(problem);

        report.IsValid.ShouldBeTrue();
        report.Diagnostics.ShouldBeEmpty();

        var result = algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);
        result.Population.EvaluatedCandidates.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Resolution itself does not validate, which is what the check at run creation is protecting against: an operator
    /// that does not preserve a search space's invariant builds and runs perfectly well, and produces candidates the
    /// space would reject.
    /// </summary>
    /// <remarks>
    /// Reaching the execution instance directly, as here, bypasses run creation and therefore the check. That is the
    /// behavior a caller opts into with <c>validate: false</c>, stated as a mechanism rather than as a default.
    /// </remarks>
    [Fact]
    public void ResolvingDoesNotValidate_SoAnUncheckedConfigurationLeavesTheSearchSpace()
    {
        var mutator = new FlipOneBitMutator();
        SearchConfigurationValidation.Validate(mutator, Constrained).IsValid.ShouldBeFalse();

        var instance = new ExecutionInstanceRegistry()
            .Resolve<BoolVector, FixedCardinalityBoolVectorSearchSpace, IProblem<BoolVector, FixedCardinalityBoolVectorSearchSpace>>(mutator);
        var inSpace = BoolVector.Create(true, true, false, false);
        Constrained.Contains(inSpace).ShouldBeTrue();

        var mutated = instance.Mutate([inSpace], RandomNumberGenerator.Create(1), Constrained, null!)[0];

        // Nothing checked or complained at this level; the candidate simply left the space.
        Constrained.Contains(mutated).ShouldBeFalse();
    }

    /// <summary>
    /// Creating a run checks the whole configuration against the problem's search space, so an operator that declares
    /// it cannot be used there stops the run before it starts instead of quietly leaving the space.
    /// </summary>
    [Fact]
    public void CreatingARun_RefusesAnOperatorTheSearchSpaceRulesOut()
    {
        var problem = ConstrainedProblem();
        var algorithm = new HillClimber<BoolVector>
        {
            Creator = new RandomBoolVectorCreator(),
            Mutator = new FlipOneBitMutator()
        };

        var failure = Should.Throw<InvalidOperationException>(
            () => algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 1)));

        failure.Message.ShouldContain(nameof(FlipOneBitMutator));
        failure.Message.ShouldContain("Cardinality(2)");
    }

    /// <summary>
    /// The check is a default, not a rule. <c>validate: false</c> runs the same configuration, which is what an author
    /// needs while an operator's declared contract is still being worked out.
    /// </summary>
    [Fact]
    public void CreatingARun_SkipsTheCheckWhenAskedTo()
    {
        var problem = ConstrainedProblem();
        var algorithm = new HillClimber<BoolVector>
        {
            Creator = new RandomBoolVectorCreator(),
            Mutator = new FlipOneBitMutator()
        };

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 1), validate: false);

        run.ShouldNotBeNull();
    }

    /// <summary>
    /// A configuration that satisfies the space starts without anything to opt out of, so the default costs a correct
    /// caller nothing but the walk.
    /// </summary>
    [Fact]
    public void CreatingARun_PassesForAConfigurationTheSpaceAccepts()
    {
        var problem = ConstrainedProblem();
        var algorithm = new HillClimber<BoolVector>
        {
            Creator = new RandomBoolVectorCreator(),
            Mutator = new BitSwapMutator()
        };

        Should.NotThrow(() => algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 1)));
    }

    private static FuncProblem<BoolVector, FixedCardinalityBoolVectorSearchSpace> ConstrainedProblem() =>
        FuncProblem.Create(
            (BoolVector candidate) => candidate.Count,
            Constrained,
            SingleObjective.Minimize);

    /// <summary>
    /// The same call also answers whether every operator can be built for this run at all, which is where problem
    /// compatibility is decided: an operator written for one problem is refused over another.
    /// </summary>
    [Fact]
    public void Validating_AlsoReportsAnOperatorThatCannotBeBuiltForTheProblem()
    {
        // A permutation problem the crossover was not written for. Over a TravelingSalesmanProblem it would bind.
        var problem = FuncProblem.Create(
            (Permutation candidate) => candidate[0],
            new PermutationSearchSpace(5),
            SingleObjective.Minimize);
        var algorithm = new GeneticAlgorithm<Permutation>
        {
            Creator = new RandomPermutationCreator(),
            Crossover = new TravellingSalesmanSpecificCrossover(),
            Mutator = new InversionMutator(),
            PopulationSize = 8,
            MaximumGenerations = 2
        };

        var report = algorithm.Validate(problem);

        report.IsValid.ShouldBeFalse();
        report.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains(nameof(TravellingSalesmanSpecificCrossover)));
    }

    /// <summary>An operator that reads its problem, so it only fits a run over that problem.</summary>
    private sealed record TravellingSalesmanSpecificCrossover
        : SingleCandidateCrossover<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>
    {
        public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace, TravelingSalesmanProblem problem) =>
            parents.Parent1;
    }
}
