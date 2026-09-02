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
/// </remarks>
public class NoviceFrictionSpecs
{
    /// <summary>
    /// The best case costs nothing. When the problem declares its own operator defaults, every type argument is
    /// inferred and a first program names no generic type at all.
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
    /// The best case is also the rare case. Only a problem that declares operator defaults can be handed to
    /// <c>For(problem)</c>, and only one problem in the library does.
    /// </summary>
    /// <remarks>
    /// A newcomer's own problem will not declare them, so the zero argument path is unavailable exactly when a
    /// newcomer needs it most. The failure is an inference error naming all thirteen factory parameters, not a
    /// message pointing at the missing declaration.
    /// </remarks>
    [Fact]
    public void TheZeroArgumentFactory_IsAvailableOnlyToProblemsThatDeclareTheirDefaults()
    {
        var problemsDeclaringDefaults = CoreAssembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(type => type.GetInterfaces().Any(role =>
                role.IsGenericType && role.GetGenericTypeDefinition() == typeof(IProblemDefaultCreator<,,>)))
            .Select(type => type.Name)
            .Order()
            .ToArray();

        problemsDeclaringDefaults.ShouldBe([nameof(TravelingSalesmanProblem)]);

        // Does not compile, although it is the first thing the getting started guide teaches for a different problem.
        // TestFunctionProblem does not declare defaults, so nothing fixes TCandidate and TSearchSpace:
        //
        // var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));
        // var algorithm = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);
        //
        // CS0411: the type arguments for method
        //   'GeneticAlgorithm.For<TProblem, TCandidate, TSearchSpace>(IProblem<TProblem, TCandidate,
        //   TSearchSpace>, ICreator<...>?, ICrossover<...>?, IMutator<...>?, ISelector<...>?, IEvaluator<...>?,
        //   IRefiner<...>?, ITerminator<...>?, IInterceptor<...>?, int, int?, double, int)'
        //   cannot be inferred from the usage. Try specifying the type arguments explicitly.
    }

    /// <summary>
    /// Naming the algorithm is the barrier. The moment a configuration outlives a single expression it needs a
    /// declared type, and <c>var</c> cannot help a field, a parameter or a return type.
    /// </summary>
    /// <remarks>
    /// The candidate type is named three times in one declaration, because the inferred problem argument is itself
    /// generic over the candidate and the search space.
    /// </remarks>
    [Fact]
    public void StoringOrPassingAnAlgorithm_RequiresEveryTypeArgument()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));

        GeneticAlgorithm<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>> declared =
            CreateRastriginAlgorithm(problem);

        var widened = WidenPopulation(declared, populationSize: 40);
        var configured =
            new List<GeneticAlgorithm<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>>
            {
                declared,
                widened
            };

        configured.Count.ShouldBe(2);
        widened.PopulationSize.ShouldBe(40);
    }

    /// <summary>
    /// The arity spreads. A helper that takes and returns an algorithm repeats every argument twice, so the cost is
    /// paid again in each calling layer rather than once at construction.
    /// </summary>
    private static GeneticAlgorithm<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>
        WidenPopulation(
            GeneticAlgorithm<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>> algorithm,
            int populationSize) =>
        algorithm with { PopulationSize = populationSize };

    /// <summary>
    /// Two algorithms over the same encoding but built through different entry points do not share a type. Holding
    /// them together names the interface, which costs a fourth type argument and exposes the search state.
    /// </summary>
    [Fact]
    public void HoldingTwoAlgorithmsTogether_ExposesTheSearchStateArgumentAsWell()
    {
        var problem = new TravelingSalesmanProblem();

        var problemAnchored = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);
        var encodingAnchored = GeneticAlgorithm.For(problem.SearchSpace, populationSize: 20, maximumGenerations: 5);

        // The two locals differ in their third type argument, so the common type has to be spelled out in full.
        List<IAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, PopulationState<Permutation>>>
            both = [problemAnchored, encodingAnchored];

        both.Count.ShouldBe(2);
    }

    /// <summary>
    /// The friction this suite was written to measure, and its result: there is no operator slot left that shows it.
    /// </summary>
    /// <remarks>
    /// The measurement used to be stated over whichever role had not migrated yet — creator, then selector, then
    /// terminator, then interceptor. All nine have migrated, so every operator slot on the one argument algorithm now
    /// names the candidate, and the search space and problem are supplied by the run rather than by the slot. What
    /// remains typed at the triple are the algorithm's own type arguments, which the algorithm package addresses.
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
    /// Preserving an invariant is a postcondition on the operator's output, and a postcondition is not a type. This
    /// bounds how much the search space argument can be expected to protect: it is not the mechanism that keeps a
    /// candidate inside a constrained space, so removing it would not forfeit a guarantee that exists. The full
    /// compatibility table lives in <see cref="SearchSpaceCompatibilitySpecs"/>.
    /// </remarks>
    [Fact]
    public void TheSearchSpaceArgument_DoesNotProtectAConstrainedSubspace()
    {
        var constrained = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);
        bool[] twoOfFour = [true, true, false, false];
        var candidate = BoolVector.Create(twoOfFour);

        // Accepted, and should not be. The operator declares no search space, so contravariance lets it fill every
        // space slot, and a single flip takes the candidate straight out of the constrained space.
        IMutator<BoolVector> constrainedSlot = new FlipOneBitMutator();

        // Rejected, and should not be. BitSwapMutator preserves cardinality, which is stronger than the
        // unconstrained space requires, so it is valid there; but it names the narrower space and contravariance
        // runs the other way:
        //
        // IMutator<BoolVector> wideSlot =
        //     new BitSwapMutator();
        //
        // CS0266: cannot implicitly convert type 'BitSwapMutator' to
        //   'IMutator<BoolVector>'

        var flipped = new FlipOneBitMutator().MutateCandidate(candidate, RandomNumberGenerator.Create(seed: 1));

        constrainedSlot.ShouldNotBeNull();
        constrained.Contains(candidate).ShouldBeTrue();
        constrained.Contains(flipped).ShouldBeFalse();
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
    public void ComposingAlgorithms_AddsTwoFurtherTypeArguments()
    {
        var problem = new TravelingSalesmanProblem();
        var stage = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        var cycled = stage.CycleWith(stage with { PopulationSize = 40 }, maximumCycles: 2);

        cycled.GetType().GetGenericArguments().Length.ShouldBe(5);
        cycled.Algorithms.Count.ShouldBe(2);
    }

    /// <summary>
    /// Direct construction is the only way to reach an algorithm typed at a concrete problem that does not declare
    /// its defaults, and it is the spelling that requires every argument. It is also what most existing examples in
    /// this repository show, so a newcomer copying one meets the full arity before meeting a factory.
    /// </summary>
    [Fact]
    public void DirectConstruction_IsTheOnlyRouteToAProblemBoundAlgorithmWithoutDeclaredDefaults()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));

        var constructed = new GeneticAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 20,
            MaximumGenerations = 5,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover(),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1),
            Selector = TournamentSelector.For(problem, tournamentSize: 2)
        };

        var inferred = CreateRastriginAlgorithm(problem);

        constructed.GetType().GetGenericArguments()[2].ShouldBe(typeof(TestFunctionProblem));
        inferred.GetType().GetGenericArguments()[2]
            .ShouldBe(typeof(IProblem<RealVector, ISearchSpace<RealVector>>));
    }

    /// <summary>
    /// The route a real vector problem has to take today: supply the operators so the factory can infer from them.
    /// The problem argument only reaches the search space, so the resulting algorithm is typed at the problem
    /// interface rather than at the problem.
    /// <para>
    /// Since the creator, crossover and mutator migrated, none of them carries a search space, so the factory infers
    /// the widest one it can rather than <see cref="BoundedRealVectorSearchSpace"/>. The algorithm this produces is
    /// weaker than the one the same call produced before, which is the cost recorded in
    /// <see cref="CreateFactories_NoLongerInferTheSearchSpaceFromTheirOperators"/>.
    /// </para>
    /// </summary>
    private static GeneticAlgorithm<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>
        CreateRastriginAlgorithm(TestFunctionProblem problem) =>
        GeneticAlgorithm.Create<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>(
            new UniformDistributedCreator(problem.SearchSpace),
            new AlphaBetaBlendCrossover(),
            new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1),
            selector: TournamentSelector.For(problem, tournamentSize: 2),
            populationSize: 20,
            maximumGenerations: 5);

    private static Assembly CoreAssembly => typeof(GeneticAlgorithm<,,>).Assembly;
}
