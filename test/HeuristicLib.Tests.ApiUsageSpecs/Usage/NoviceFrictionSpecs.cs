using System.Reflection;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Usage;

/// <summary>
/// Records where generic arity actually reaches a newcomer, so the entry barrier is discussed against observed
/// friction rather than against intuition. Each spec states one scenario and the arity it costs today.
/// </summary>
/// <remarks>
/// These specs describe current state. A spec that documents friction is not asserting that the friction is correct;
/// it pins the behavior so a later change to the type model produces a visible diff here. Code a newcomer would
/// plausibly write but that does not compile is kept as a commented block with the compiler's objection, because an
/// executable spec cannot hold it.
/// <para>
/// The arity reduction has landed, so several specs here now record a result rather than a cost, and each says which
/// friction it used to record. What survives is friction that arity was never the cause of, such as an invariant no
/// type argument could have expressed.
/// </para>
/// </remarks>
public class NoviceFrictionSpecs
{
    /// <summary>
    /// The best case costs nothing. The problem and search space recommendations supply the operators, every type
    /// argument is inferred and a first program names no generic type at all.
    /// </summary>
    [Fact]
    public async Task BuildingAndRunningAnAlgorithm_NamesNoTypeArgument()
    {
        var problem = new TravelingSalesmanProblem();

        var algorithm = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        var finalState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(seed: 42),
            ct: TestContext.Current.CancellationToken);

        finalState.Population.EvaluatedCandidates.Count.ShouldBe(20);
    }

    /// <summary>
    /// <c>For(problem)</c> is available to every problem. If supplied values and recommendations leave required
    /// parameters unresolved, the factory reports all of them at once.
    /// </summary>
    /// <remarks>
    /// This used to be a compile error caused by six constraints. The constraints required both sources to declare
    /// every role even though the intended rule was supplied value, problem recommendation or search space
    /// recommendation for each parameter.
    /// </remarks>
    [Fact]
    public void TheZeroArgumentFactory_ReportsEveryMissingRecommendation()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));

        var exception = Should.Throw<InvalidOperationException>(() => GeneticAlgorithm.For(problem));

        exception.Message.ShouldBe(
            "Cannot create GeneticAlgorithm. No value or recommendation was found for required parameters: " +
            "creator, crossover, mutator. Consulted TestFunctionProblem and BoundedRealVectorSearchSpace.");

        GeneticAlgorithm<RealVector> explicitlyConfigured = GeneticAlgorithm.For(
            problem,
            creator: new UniformDistributedCreator(problem.SearchSpace),
            crossover: new SinglePointCrossover(),
            mutator: new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1));

        explicitlyConfigured.Creator.ShouldBeOfType<UniformDistributedCreator>();
    }

    /// <summary>
    /// Naming the algorithm is still the moment its type becomes visible — <c>var</c> cannot help a field, a
    /// parameter or a return type — but what it forces is now one type argument, and that argument is the one thing
    /// the newcomer already knows they are optimizing.
    /// </summary>
    /// <remarks>
    /// This spec was written when the declaration named the candidate three times, because the search space and the
    /// problem had to be spelled alongside it. Both have left the configuration layer, so the suite's former headline
    /// friction is now a single word.
    /// </remarks>
    [Fact]
    public void StoringOrPassingAnAlgorithm_NamesOnlyTheCandidate()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));

        GeneticAlgorithm<RealVector> declared = CreateRastriginAlgorithm(problem);

        var widened = WidenPopulation(declared, populationSize: 40);
        var configured = new List<GeneticAlgorithm<RealVector>> { declared, widened };

        // One argument on the declared type, and it is the candidate.
        typeof(GeneticAlgorithm<RealVector>).GetGenericArguments().ShouldHaveSingleItem().ShouldBe(typeof(RealVector));
        configured.Count.ShouldBe(2);
        widened.PopulationSize.ShouldBe(40);
    }

    /// <summary>
    /// The arity used to spread. A helper taking and returning an algorithm repeated every argument twice, so each
    /// calling layer paid the cost again rather than paying it once at construction. It still repeats, but one
    /// argument twice fits on one line, which is the difference between a signature that gets read and one that gets
    /// skipped.
    /// </summary>
    private static GeneticAlgorithm<RealVector> WidenPopulation(GeneticAlgorithm<RealVector> algorithm, int populationSize) =>
        algorithm with { PopulationSize = populationSize };

    /// <summary>
    /// Two algorithms over the same encoding but built through different entry points now share a type outright, so
    /// holding them together names nothing at all.
    /// </summary>
    /// <remarks>
    /// This spec recorded the opposite. The problem anchored and encoding anchored factories differed in the search
    /// space baked into the returned type, so a common type had to be reached for through the interface, which cost a
    /// further argument and exposed the search state to someone who had not asked about it. With the search space out
    /// of the configuration both factories return <c>GeneticAlgorithm&lt;Permutation&gt;</c> and <c>var</c> covers the
    /// collection. The interface form is kept below to show it now stops at the candidate.
    /// </remarks>
    [Fact]
    public void HoldingTwoAlgorithmsTogether_NamesNothing()
    {
        var problem = new TravelingSalesmanProblem();

        var problemAnchored = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);
        var encodingAnchored = GeneticAlgorithm.For(problem.SearchSpace, populationSize: 20, maximumGenerations: 5);

        // Same type, so this needs no type argument anywhere.
        var both = new[] { problemAnchored, encodingAnchored };

        // And where the interface is genuinely wanted, it names the candidate and stops.
        List<IAlgorithm<Permutation>> viaInterface = [problemAnchored, encodingAnchored];

        both.GetType().GetElementType().ShouldBe(typeof(GeneticAlgorithm<Permutation>));
        viaInterface.Count.ShouldBe(2);
    }

    /// <summary>
    /// The friction this suite was written to measure, and its result: there is no operator slot left that shows it.
    /// </summary>
    /// <remarks>
    /// Every operator slot names the candidate; the search space, problem and search state are supplied by the run.
    /// </remarks>
    [Fact]
    public void EveryOperatorSlot_NamesOnlyWhatTheOperatorIsWrittenAbout()
    {
        var algorithm = typeof(GeneticAlgorithm<RealVector>);

        // Every slot, terminators and interceptors included, names the candidate and nothing else.
        string[] slots =
        [
            nameof(GeneticAlgorithm<RealVector>.Creator), nameof(GeneticAlgorithm<RealVector>.Crossover),
            nameof(GeneticAlgorithm<RealVector>.Mutator), nameof(GeneticAlgorithm<RealVector>.Selector),
            nameof(GeneticAlgorithm<RealVector>.Terminator), nameof(GeneticAlgorithm<RealVector>.Interceptor)
        ];

        foreach (var slot in slots)
        {
            algorithm.GetProperty(slot)!.PropertyType.GetGenericArguments().ShouldHaveSingleItem();
        }
    }

    /// <summary>
    /// The same measurement on the migrated role, which is what the arity reduction buys. A mutator that reads
    /// <see cref="BoundedRealVectorSearchSpace"/> fills the one argument algorithm's slot, because the slot no longer
    /// names a search space at all: the run supplies it when the execution instance is created, and a mismatch is
    /// reported by the pre-flight check rather than by a conversion the author has to talk the compiler out of.
    /// </summary>
    [Fact]
    public void TheOneArgumentAlgorithm_TakesAnOperatorThatReadsTheSearchSpace()
    {
        IMutator<RealVector> encodingBoundMutator =
            new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1);

        var mutatorSlot = typeof(GeneticAlgorithm<RealVector>)
            .GetProperty(nameof(GeneticAlgorithm<RealVector>.Mutator))!;

        mutatorSlot.PropertyType.ShouldBe(typeof(IMutator<RealVector>));
        mutatorSlot.PropertyType.IsInstanceOfType(encodingBoundMutator).ShouldBeTrue();
    }

    /// <summary>
    /// Measures how much the search space type argument buys as a static check. It is determined by the candidate
    /// type for every encoding except bool vectors, where a fixed cardinality space constrains membership and the
    /// argument therefore carries information the candidate type does not.
    /// </summary>
    /// <remarks>
    /// The 1:1 mapping is a property of the current library, not an invariant, and it is deliberately no longer
    /// total. Any further candidate type gaining a second search space fails this spec, which is the intended way to
    /// reopen the question of whether the argument can leave the configuration layer.
    /// </remarks>
    [Fact]
    public void SearchSpacesAreDeterminedByTheirCandidateType_ExceptWhereMembershipDiffers()
    {
        var searchSpacesByCandidate = CoreAssembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(role => role.IsGenericType && role.GetGenericTypeDefinition() == typeof(ISearchSpace<>))
                .Select(role => (Candidate: role.GetGenericArguments()[0], SearchSpace: type)))
            .GroupBy(entry => entry.Candidate)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.SearchSpace).Distinct().ToArray());

        var candidatesWithSeveralSearchSpaces = searchSpacesByCandidate
            .Where(entry => entry.Value.Length > 1)
            .Select(entry => entry.Key.Name)
            .Order()
            .ToArray();

        candidatesWithSeveralSearchSpaces.ShouldBe([nameof(BoolVector)]);

        searchSpacesByCandidate[typeof(BoolVector)].Order(TypeNameComparer).ShouldBe(
            [typeof(BoolVectorSearchSpace), typeof(FixedCardinalityBoolVectorSearchSpace)]);
        searchSpacesByCandidate[typeof(RealVector)].Single().ShouldBe(typeof(BoundedRealVectorSearchSpace));
        searchSpacesByCandidate[typeof(Permutation)].Single().ShouldBe(typeof(PermutationSearchSpace));
    }

    /// <summary>
    /// A single element flip keeps a candidate in <see cref="BoolVectorSearchSpace"/> and takes it out of
    /// <see cref="FixedCardinalityBoolVectorSearchSpace"/>, so the two spaces need different operators over one
    /// candidate representation. This is the case the search space type argument exists for, and no run-time value
    /// carried by the candidate could substitute for it.
    /// </summary>
    [Fact]
    public void OneRepresentationWithTwoSearchSpaces_NeedsDifferentOperators()
    {
        var unconstrained = new BoolVectorSearchSpace(Length: 4);
        var constrained = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);
        bool[] twoOfFour = [true, true, false, false];
        bool[] oneOfFour = [false, true, false, false];
        var candidate = BoolVector.Create(twoOfFour);

        var flipped = BoolVector.Create(oneOfFour);
        var swapped = BitSwapMutator.Mutate(candidate, RandomNumberGenerator.Create(seed: 5), constrained);

        unconstrained.Contains(candidate).ShouldBeTrue();
        constrained.Contains(candidate).ShouldBeTrue();

        unconstrained.Contains(flipped).ShouldBeTrue();
        constrained.Contains(flipped).ShouldBeFalse();

        constrained.Contains(swapped).ShouldBeTrue();
    }

    /// <summary>
    /// The search space type argument states which search space an operator <em>reads</em>, not which invariant it
    /// <em>preserves</em>. Those are different properties, and for a constrained subspace C# answers both the wrong
    /// way round.
    /// </summary>
    /// <remarks>
    /// This spec recorded two failures, and the arity reduction removed one. <see cref="BitSwapMutator"/> preserves
    /// cardinality, which is stronger than the unconstrained space requires, so it is valid there; but it named the
    /// narrower space and contravariance ran the other way, so the slot refused it. The slot names no search space
    /// now, and the assignment is simply accepted. That failure was pure arity cost and it is paid off.
    /// <para>
    /// The other failure remains, and it is not an arity question. Preserving an invariant is a postcondition on the
    /// operator's output, and a postcondition is not a type, so no type argument was ever going to answer it — which
    /// is why removing the argument forfeits no guarantee that existed. The invariant contract is what answers it.
    /// The full compatibility table lives in <see cref="SearchSpaceCompatibilitySpecs"/>.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheSearchSpaceArgument_DoesNotProtectAConstrainedSubspace()
    {
        var constrained = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);
        bool[] twoOfFour = [true, true, false, false];
        var candidate = BoolVector.Create(twoOfFour);

        // Accepted, and should not be. A single flip takes the candidate straight out of the constrained space, and
        // no arrangement of type arguments would have caught it, because it is a fact about the output.
        IMutator<BoolVector> constrainedSlot = new FlipOneBitMutator();

        // Accepted, and now rightly so. This was CS0266 before the reduction: the slot named the unconstrained space,
        // BitSwapMutator named the narrower one, and contravariance ran the wrong way. The slot names neither now.
        IMutator<BoolVector> wideSlot = new BitSwapMutator();

        var flipped = new FlipOneBitMutator().MutateCandidate(candidate, RandomNumberGenerator.Create(seed: 1));

        constrainedSlot.ShouldNotBeNull();
        wideSlot.ShouldNotBeNull();
        constrained.Contains(candidate).ShouldBeTrue();
        constrained.Contains(flipped).ShouldBeFalse();

        // The slot accepts both, so it was never what distinguished them. This is, and it is a value the operator
        // states about itself rather than a position in a type argument list.
        new BitSwapMutator().Ensures(new BoolVectorCardinality(Cardinality: 2)).ShouldBe(true);
        new FlipOneBitMutator().Ensures(new BoolVectorCardinality(Cardinality: 2)).ShouldNotBe(true);
    }

    private static readonly Comparer<Type> TypeNameComparer =
        Comparer<Type>.Create((left, right) => string.CompareOrdinal(left.Name, right.Name));

    /// <summary>
    /// A problem bound operator still works, and still reports a mismatch, after the roles stopped naming the problem.
    /// </summary>
    /// <remarks>
    /// The measurement this replaces counted how many operators constrained the role's problem argument; the roles no
    /// longer have one, so the count is not the question any more. The capability is: an operator that must read a
    /// specific problem states it on its own type, runs when the run supplies that problem, and is refused otherwise.
    /// The operator below is declared here rather than in the library, so it also stands for one a consumer writes.
    /// </remarks>
    [Fact]
    public void AnOperatorThatReadsItsProblem_RunsOverThatProblemAndIsRefusedOverAnother()
    {
        var problemBound = new DimensionCountingMutator();

        // A registry serves one run, so each triple is asked in its own.
        new ExecutionInstanceRegistry()
            .TryResolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problemBound, out var instance, out var reason)
            .ShouldBeTrue();
        instance.ShouldNotBeNull();
        reason.ShouldBeNull();

        new ExecutionInstanceRegistry()
            .TryResolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(problemBound, out var wrong, out var refusal)
            .ShouldBeFalse();
        wrong.ShouldBeNull();
        refusal.ShouldNotBeNull();
        refusal.ShouldContain(nameof(TestFunctionProblem));
    }

    /// <summary>An operator that genuinely needs its problem, of the kind the library keeps working for.</summary>
    private sealed record DimensionCountingMutator : SingleCandidateMutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector MutateCandidate(RealVector parent, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            RealVector.Repeat(problem.TestFunction.Dimension, parent.Count);
    }

    /// <summary>
    /// Authoring cost tracks what the operator reads, which is the part of the design that already works. An
    /// operator that ignores the search space is written at one type argument.
    /// </summary>
    private sealed record AdjacentSwapMutator : SingleCandidateMutator<Permutation>
    {
        public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
            parent.Count < 2 ? parent : parent.Swap(0, 1);
    }

    /// <summary>
    /// An operator that reads the search space pays for the second argument, and repeats the pair in its own
    /// declaration, in the base it derives from and in every slot that holds it.
    /// </summary>
    private sealed record ClampingMutator : SingleCandidateMutator<RealVector, BoundedRealVectorSearchSpace>
    {
        public override RealVector MutateCandidate(
            RealVector parent,
            IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace) =>
            RealVector.Clamp(parent, searchSpace.Minimum, searchSpace.Maximum);
    }

    [Fact]
    public void AuthoringAnOperator_CostsOnlyTheArgumentsItReads()
    {
        var permutationProblem = new TravelingSalesmanProblem();
        var permutationAlgorithm =
            GeneticAlgorithm.For(permutationProblem, populationSize: 20, maximumGenerations: 5) with
            {
                Mutator = new AdjacentSwapMutator()
            };

        var realVectorProblem = new TestFunctionProblem(new RastriginFunction(dimension: 3));
        var realVectorAlgorithm = CreateRastriginAlgorithm(realVectorProblem) with
        {
            Mutator = new ClampingMutator()
        };

        permutationAlgorithm.Mutator.ShouldBeOfType<AdjacentSwapMutator>();
        realVectorAlgorithm.Mutator.ShouldBeOfType<ClampingMutator>();
    }

    /// <summary>
    /// Composition adds the algorithm self type and the search state on top of the three a stage already carries.
    /// Recorded for later; the simple cases come first.
    /// </summary>
    [Fact]
    public void ComposingAlgorithms_AddsOneFurtherTypeArgument()
    {
        var problem = new TravelingSalesmanProblem();
        var stage = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        var cycled = stage.CycleWith(stage with { PopulationSize = 40 }, maximumCycles: 2);

        // The composed algorithm, the candidate, and the state threaded between stages.
        cycled.GetType().GetGenericArguments().Length.ShouldBe(3);
        cycled.Algorithms.Count.ShouldBe(2);
    }

    /// <summary>
    /// Direct construction used to be the only way to reach an algorithm typed at a concrete problem, and it was the
    /// spelling that required every argument. Both are now the same one-argument type, so a newcomer copying an
    /// example from this repository no longer meets a wider arity than a factory would have given them.
    /// </summary>
    [Fact]
    public void DirectConstructionAndTheFactory_NowProduceTheSameType()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));

        var constructed = new GeneticAlgorithm<RealVector>
        {
            PopulationSize = 20,
            MaximumGenerations = 5,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover(),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1),
            Selector = TournamentSelector.For(problem, tournamentSize: 2)
        };

        var inferred = CreateRastriginAlgorithm(problem);

        // Both spellings produce the same one-argument type, and the run supplies the problem either way.
        constructed.GetType().GetGenericArguments().ShouldHaveSingleItem().ShouldBe(typeof(RealVector));
        inferred.GetType().ShouldBe(constructed.GetType());
    }

    /// <summary>
    /// The operator route: supply the creator, crossover and mutator, and the factory infers the only type it names.
    /// The problem argument reaches the operators that need it; the run supplies the search space and problem.
    /// </summary>
    private static GeneticAlgorithm<RealVector> CreateRastriginAlgorithm(TestFunctionProblem problem) =>
        GeneticAlgorithm.Create(
            new UniformDistributedCreator(problem.SearchSpace),
            new AlphaBetaBlendCrossover(),
            new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1),
            selector: TournamentSelector.For(problem, tournamentSize: 2),
            populationSize: 20,
            maximumGenerations: 5);

    private static Assembly CoreAssembly => typeof(GeneticAlgorithm<>).Assembly;
}
