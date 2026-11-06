using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Execution;

// A substitution rewrites a configuration graph eagerly. See plans/core-simplification-plan.md, D17, D26, D28, D32, D33.
public class SubstitutionTests
{
    private static readonly IOperator<Creation, int> Creator = new Stub<Creation>("creator");
    private static readonly IOperator<Crossover, int> Crossover = new Stub<Crossover>("crossover");
    private static readonly IOperator<Mutation, int> Mutator = new Stub<Mutation>("mutator");
    private static readonly IOperator<Evaluation, int> Evaluator = new Stub<Evaluation>("evaluator");
    private static readonly IOperator<Selection, int> Selector = new Stub<Selection>("selector");

    private static GeneticAlgorithm<int> Ga(
        IOperator<Mutation, int>? mutator = null, IOperator<Evaluation, int>? evaluator = null, IOperator<Crossover, int>? crossover = null) => new()
        {
            Creator = Creator,
            Crossover = crossover ?? Crossover,
            Mutator = mutator ?? Mutator,
            Evaluator = evaluator ?? Evaluator,
            Selector = Selector
        };

    // The declared type says what a walk does with a parameter.
    [Fact]
    public void Parameter_ChildrenAreWhatTheDeclaredTypeSays()
    {
        var holder = new Holder(3, Mutator, null, static () => 1, new Stub<Evaluation>("in an object"));

        Holder.Parameters.SelectMany(parameter => parameter.GetChildren(holder)).ShouldBe([Mutator]);
        (holder with { Optional = Mutator }).Substitute(Mutator, mutator => mutator.Named("a")).ShouldBe(
            holder with { Child = Mutator.Named("a"), Optional = Mutator.Named("a") });
        holder.Substitute(Mutator, mutator => mutator.Named("a")).Optional.ShouldBeNull();
    }

    [Fact]
    public void Parameter_ANodeInASettingIsNotEntered()
    {
        IOperator<Evaluation, int> hidden = new Stub<Evaluation>("in an object");
        var holder = new Holder(3, Mutator, null, static () => 1, hidden);

        Should.Throw<InvalidOperationException>(() => holder.Substitute(hidden, evaluator => evaluator.Named("a")));
    }

    [Fact]
    public void Parameter_ADelegateIsReportedAsABoundary()
    {
        var holder = new Holder(3, Mutator, null, static () => 1, 0);

        Should.Throw<InvalidOperationException>(() => holder.Substitute(Evaluator, evaluator => evaluator.Named("a"))).Message.ShouldContain("Holder.Build");
    }

    [Fact]
    public void Parameter_ForAListOfConfigurations_NamesTheParameterToUse()
    {
        Should.Throw<ArgumentException>(() => new Parameter<Holder, IReadOnlyList<IOperator<Mutation, int>>>("List", static _ => [], static (holder, _) => holder))
            .Message.ShouldContain(nameof(ListParameter<,>));
    }

    [Fact]
    public void Parameter_ForAListOfConfigurations_NamesTheParameterForData()
    {
        Should.Throw<ArgumentException>(() => new Parameter<Holder, IReadOnlyList<IOperator<Mutation, int>>>("List", static _ => [], static (holder, _) => holder))
            .Message.ShouldContain(nameof(SettingParameter<,>));
    }

    // A candidate can be a configuration, and it is still data to whatever holds it.
    [Fact]
    public void SettingParameter_ANodeItHoldsIsNotEntered()
    {
        var holder = new SettingHolder<IOperator<Mutation, int>>(Mutator);

        SettingHolder<IOperator<Mutation, int>>.TargetParameter.GetChildren(holder).ShouldBeEmpty();
        Should.Throw<InvalidOperationException>(() => holder.Substitute(Mutator, mutator => mutator.Named("a")));
    }

    [Fact]
    public void SettingParameter_ADelegateItHoldsIsNotReportedAsABoundary()
    {
        var hidden = new Stub<Evaluation>("elsewhere");
        var holder = new SettingHolder<Func<int>>(static () => 1);

        Should.Throw<InvalidOperationException>(() => holder.Substitute(hidden, evaluator => evaluator.Named("a")))
            .Message.ShouldNotContain($">.{nameof(SettingHolder<>.Target)}");
    }

    [Fact]
    public void SettingParameter_ReadsAndVariesItsValue()
    {
        var holder = new SettingHolder<int>(1);
        var parameter = SettingHolder<int>.TargetParameter;

        parameter.Get(holder).ShouldBe(1);
        parameter.GetValue(holder).ShouldBe(1);
        parameter.With(holder, 2).Target.ShouldBe(2);
        parameter.ValueType.ShouldBe(typeof(int));
    }

    private sealed record SettingHolder<TValue>(TValue Target) : IConfigurationNode
    {
        public static SettingParameter<SettingHolder<TValue>, TValue> TargetParameter { get; } =
            new(nameof(Target), static holder => holder.Target, static (holder, value) => holder with { Target = value });

        public IReadOnlyList<IParameter> GetParameters() => [TargetParameter];
    }

    private sealed record Holder(int Size, IOperator<Mutation, int> Child, IOperator<Mutation, int>? Optional, Func<int> Build, object Setting) : IConfigurationNode
    {
        public static Parameter<Holder, int> SizeParameter { get; } =
            new(nameof(Size), static holder => holder.Size, static (holder, value) => holder with { Size = value });

        public static Parameter<Holder, IOperator<Mutation, int>> ChildParameter { get; } =
            new(nameof(Child), static holder => holder.Child, static (holder, value) => holder with { Child = value });

        public static Parameter<Holder, IOperator<Mutation, int>?> OptionalParameter { get; } =
            new(nameof(Optional), static holder => holder.Optional, static (holder, value) => holder with { Optional = value });

        public static Parameter<Holder, Func<int>> BuildParameter { get; } =
            new(nameof(Build), static holder => holder.Build, static (holder, value) => holder with { Build = value });

        public static Parameter<Holder, object> SettingParameter { get; } =
            new(nameof(Setting), static holder => holder.Setting, static (holder, value) => holder with { Setting = value });

        public static IReadOnlyList<IParameter> Parameters { get; } = [SizeParameter, ChildParameter, OptionalParameter, BuildParameter, SettingParameter];

        public IReadOnlyList<IParameter> GetParameters() => Parameters;
    }

    [Fact]
    public void Substitution_EqualsTheGraphWrittenByHand()
    {
        var ga = Ga();

        var substituted = ga.Substitute(ga.Mutator, mutator => mutator.Named("a"));

        substituted.ShouldBe(ga with { Mutator = ga.Mutator.Named("a") });
        ga.Mutator.ShouldBeSameAs(Mutator);
    }

    [Fact]
    public void Substitution_CopiesOnlyThePathToWhatChanged()
    {
        var ga = Ga();

        var substituted = ga.Substitute(ga.Mutator, mutator => mutator.Named("a"));

        substituted.Evaluator.ShouldBeSameAs(ga.Evaluator);
        substituted.Selector.ShouldBeSameAs(ga.Selector);
    }

    [Fact]
    public void Predicate_SelectingNothingReturnsTheSameInstance()
    {
        var ga = Ga();

        ga.Substitute((IOperator<Mutation, int> _) => false, mutator => mutator.Named("a")).ShouldBeSameAs(ga);
    }

    [Fact]
    public void Type_SelectsEveryNodeOfThatType()
    {
        var multi = new MultiMutator(new Stub<Mutation>("x"), new Stub<Mutation>("y"));
        var ga = Ga(mutator: multi);

        var substituted = ga.Substitute((IOperator<Mutation, int> mutator) => mutator.Named("a"));

        substituted.Mutator.ShouldBe(new MultiMutator(new Stub<Mutation>("x").Named("a"), new Stub<Mutation>("y").Named("a")).Named("a"));
    }

    [Fact]
    public void Type_AlsoSelectsAWrapperFromAnEarlierSubstitution()
    {
        var ga = Ga();

        var substituted = ga
            .Substitute(ga.Mutator, mutator => mutator.Named("a"))
            .Substitute((Named<Mutation, int> named) => named with { Name = "b" });

        substituted.Mutator.ShouldBe(Mutator.Named("b"));
    }

    [Fact]
    public void Predicate_SeesTheNodeAsTheCallReceivedIt_AndTheReplacementSeesSubstitutedChildren()
    {
        IOperator<Mutation, int> inner = new Stub<Mutation>("inner");
        IOperator<Mutation, int> outer = inner.Named("outer");
        var ga = Ga(mutator: outer);

        var substituted = ga.Substitute(
            Substitution.Of(inner, mutator => mutator.Named("a")),
            Substitution.Of<IOperator<Mutation, int>>(mutator => ReferenceEquals(mutator, outer), mutator => mutator.Named("b")));

        // The second predicate matches the original outer, although the first substitution copied it.
        substituted.Mutator.ShouldBe(inner.Named("a").Named("outer").Named("b"));
    }

    [Fact]
    public void LaterSubstitutionOnTheSameNode_SitsCloserToIt()
    {
        var ga = Ga();

        var substituted = ga.Substitute(
            Substitution.Of(ga.Mutator, mutator => mutator.Named("A")),
            Substitution.Of(ga.Mutator, mutator => mutator.Named("B")));

        substituted.Mutator.ShouldBe(Mutator.Named("B").Named("A"));
    }

    [Fact]
    public void SubstitutionListedTwice_AppliesOnceAtItsLastPosition()
    {
        var ga = Ga();
        var clock = Substitution.Of(ga.Mutator, mutator => mutator.Named("clock"));
        var first = Substitution.Of(ga.Mutator, mutator => mutator.Named("first"));
        var second = Substitution.Of(ga.Mutator, mutator => mutator.Named("second"));

        var substituted = ga.Substitute(first, clock, second, clock);

        substituted.Mutator.ShouldBe(Mutator.Named("clock").Named("second").Named("first"));
    }

    [Fact]
    public void ANodeReplacedCompletely_LeavesNothingForALaterSubstitutionInTheCallToMatch()
    {
        var ga = Ga();
        var replacement = new Stub<Mutation>("replacement");

        var substituted = ga.Substitute(
            Substitution.Of(ga.Mutator, _ => replacement),
            Substitution.Of<IOperator<Mutation, int>>(mutator => ReferenceEquals(mutator, Mutator), mutator => mutator.Named("late")));

        substituted.Mutator.ShouldBeSameAs(replacement);
    }

    [Fact]
    public void AReferenceInTwoSlots_StaysOneReference()
    {
        var shared = new Stub<Mutation>("shared");
        var cycle = CycleAlgorithm.Create<int, PopulationState<int>>(Ga(mutator: shared), Ga(mutator: shared));
        var calls = 0;

        var substituted = cycle.Substitute((IOperator<Mutation, int> mutator) =>
        {
            calls++;
            return mutator.Named("a");
        });

        calls.ShouldBe(1);
        var first = (GeneticAlgorithm<int>)substituted.Algorithms[0];
        var second = (GeneticAlgorithm<int>)substituted.Algorithms[1];
        first.Mutator.ShouldBeSameAs(second.Mutator);
    }

    [Fact]
    public void ASubstitutionDoesNotEnterItsOwnReplacement()
    {
        var ga = Ga();
        var calls = 0;

        var substituted = ga.Substitute((IOperator<Mutation, int> mutator) =>
        {
            calls++;
            return mutator.Named("a");
        });

        calls.ShouldBe(1);
        substituted.Mutator.ShouldBe(Mutator.Named("a"));
    }

    // D10: a slot or receiver that cannot hold the replacement.

    [Fact]
    public void AConcretelyTypedSlot_ThatCannotHoldTheReplacement_IsAnErrorNamingTheSlot()
    {
        var ga = Ga();
        var holder = new HoldingAConcreteAlgorithm(ga);

        var error = Should.Throw<InvalidOperationException>(() =>
            holder.Substitute(Substitution.Of<IAlgorithm<int, PopulationState<int>>>(ga, algorithm => algorithm.Named("a"))));

        error.Message.ShouldContain(nameof(HoldingAConcreteAlgorithm.Inner));
    }

    [Fact]
    public void AConcretelyTypedSlot_ThatCanHoldTheReplacement_KeepsItsType()
    {
        var ga = Ga();
        var holder = new HoldingAConcreteAlgorithm(ga);

        var substituted = holder.Substitute(ga.Mutator, mutator => mutator.Named("a"));

        substituted.Inner.Mutator.ShouldBe(Mutator.Named("a"));
    }

    [Fact]
    public void AReceiverTypedConcretely_CannotHoldAWrapper_WhileOneTypedByTheInterfaceCan()
    {
        var ga = Ga();
        IAlgorithm<int, PopulationState<int>> typedByInterface = ga;

        var error = Should.Throw<InvalidOperationException>(() =>
            ga.Substitute(Substitution.Of<IAlgorithm<int, PopulationState<int>>>(ga, algorithm => algorithm.Named("a"))));

        error.Message.ShouldContain("the receiver");
        typedByInterface.Substitute(typedByInterface, algorithm => algorithm.Named("a")).ShouldBe(ga.Named("a"));
    }

    // D3: a substitution reaches declared children only.

    [Fact]
    public void ChildrenAConsumerDoesNotDeclare_StayOutOfReach()
    {
        var hidden = new Stub<Mutation>("hidden");
        var ga = Ga(mutator: new Hiding(hidden));

        ga.Substitute((Stub<Mutation> stub) => stub with { Id = "changed" }).Mutator.ShouldBe(new Hiding(hidden));
        Should.Throw<InvalidOperationException>(() => ga.Substitute(hidden, stub => stub with { Id = "changed" }));
    }

    [Fact]
    public void TargetBehindADelegate_IsAnErrorNamingTheDelegate()
    {
        var builder = new Building(() => new Stub<Mutation>("built"));

        var error = Should.Throw<InvalidOperationException>(() =>
            builder.Substitute(Mutator, mutator => mutator.Named("a")));

        error.Message.ShouldContain("Build");
    }

    // D26: multi-operation nodes.

    [Fact]
    public void ASlotTypedForAnotherOperation_KeepsItsReference()
    {
        var both = new MutationAndCrossover();
        var ga = Ga(mutator: both, crossover: both);

        var substituted = ga.Substitute((IOperator<Mutation, int>)both, mutator => mutator.Named("a"));

        substituted.Mutator.ShouldBe(((IOperator<Mutation, int>)both).Named("a"));
        substituted.Crossover.ShouldBeSameAs(both);
    }

    [Fact]
    public void ASharingEntry_KeepsNamingTheOriginalWhileTheReplacementContainsIt()
    {
        var both = new MutationAndCrossover();
        var ga = Ga(mutator: both, crossover: both);

        var substituted = ga.Sharing(both).Substitute((IOperator<Mutation, int>)both, mutator => mutator.Named("a"));

        substituted.Shared[0].ShouldBeSameAs(both);
    }

    [Fact]
    public void ASharingEntry_FollowsACompleteReplacement()
    {
        var ga = Ga();
        var replacement = new Stub<Mutation>("replacement");

        var substituted = ga.Sharing(ga.Mutator).Substitute(ga.Mutator, _ => replacement);

        substituted.Shared[0].ShouldBeSameAs(replacement);
    }

    [Fact]
    public void ASharingEntry_FollowsACopyOfItsConfiguration()
    {
        IOperator<Mutation, int> inner = new Stub<Mutation>("inner");
        IOperator<Mutation, int> outer = inner.Named("outer");
        var ga = Ga(mutator: outer);

        var substituted = ga.Sharing(outer).Substitute(inner, _ => new Stub<Mutation>("replacement"));

        substituted.Shared[0].ShouldBeSameAs(((GeneticAlgorithm<int>)substituted.ChildAlgorithm).Mutator);
    }

    [Fact]
    public void ATargetOnlyASharingEntryNames_IsNotAPlacement()
    {
        var ga = Ga();
        IOperator<Mutation, int> other = new Stub<Mutation>("other");

        Should.Throw<InvalidOperationException>(() => ga.Sharing(other).Substitute(other, mutator => mutator.Named("a")));
    }

    // D33: leaving out the comparer means reference equality.

    [Fact]
    public void Target_MatchesByReferenceByDefault()
    {
        var a = Ga(mutator: new Stub<Mutation>("same"));
        var b = Ga(mutator: new Stub<Mutation>("same"));
        var cycle = CycleAlgorithm.Create<int, PopulationState<int>>(a, b);

        var substituted = cycle.Substitute(a.Mutator, mutator => mutator.Named("a"));

        ((GeneticAlgorithm<int>)substituted.Algorithms[0]).Mutator.ShouldBe(a.Mutator.Named("a"));
        ((GeneticAlgorithm<int>)substituted.Algorithms[1]).Mutator.ShouldBeSameAs(b.Mutator);
    }

    [Fact]
    public void Target_WithAComparerSelectsEveryNodeItConsidersEqual()
    {
        var a = Ga(mutator: new Stub<Mutation>("same"));
        var b = Ga(mutator: new Stub<Mutation>("same"));
        var cycle = CycleAlgorithm.Create<int, PopulationState<int>>(a, b);

        var substituted = cycle.Substitute(a.Mutator, mutator => mutator.Named("a"), EqualityComparer<IConfigurationNode>.Default);

        ((GeneticAlgorithm<int>)substituted.Algorithms[0]).Mutator.ShouldBe(a.Mutator.Named("a"));
        ((GeneticAlgorithm<int>)substituted.Algorithms[1]).Mutator.ShouldBe(b.Mutator.Named("a"));
    }

    [Fact]
    public void Target_WithAComparerReportsAnErrorWhenNothingIsEqual()
    {
        var ga = Ga();

        Should.Throw<InvalidOperationException>(() =>
            ga.Substitute(Substitution.Of<IOperator<Mutation, int>>(new Stub<Mutation>("absent"), mutator => mutator.Named("a"), EqualityComparer<IConfigurationNode>.Default)));
    }

    [Fact]
    public void Target_ReferenceEqualityComparerWorksAsItIs()
    {
        var ga = Ga();

        ga.Substitute(ga.Mutator, mutator => mutator.Named("a"), ReferenceEqualityComparer.Instance)
            .ShouldBe(ga.Substitute(ga.Mutator, mutator => mutator.Named("a")));
    }

    // D32: which references an earlier call leaves in place.

    [Fact]
    public void ReferenceOfASibling_StillMatchesAfterAnEarlierCall()
    {
        var ga = Ga();

        var substituted = ga
            .Substitute(ga.Mutator, mutator => mutator.Named("a"))
            .Substitute(ga.Evaluator, evaluator => evaluator.Named("b"));

        substituted.ShouldBe(ga with { Mutator = Mutator.Named("a"), Evaluator = Evaluator.Named("b") });
    }

    [Fact]
    public void ReferenceOfTheSameWrappedNode_StillMatchesAfterAnEarlierCall()
    {
        var ga = Ga();

        var substituted = ga
            .Substitute(ga.Mutator, mutator => mutator.Named("a"))
            .Substitute(ga.Mutator, mutator => mutator.Named("b"));

        substituted.Mutator.ShouldBe(Mutator.Named("b").Named("a"));
    }

    [Fact]
    public void ReferenceInsideAWrappedNode_StillMatchesAfterAnEarlierCall()
    {
        IOperator<Evaluation, int> inner = new Stub<Evaluation>("inner");
        IOperator<Evaluation, int> outer = inner.Named("outer");
        var ga = Ga(evaluator: outer);

        var substituted = ga
            .Substitute(outer, evaluator => evaluator.Named("a"))
            .Substitute(inner, _ => new Stub<Evaluation>("replaced"));

        substituted.Evaluator.ShouldBe(new Stub<Evaluation>("replaced").Named("outer").Named("a"));
    }

    [Fact]
    public void ReferenceOfAnAncestor_IsAnErrorNamingTheRoutes()
    {
        var ga = Ga();
        IAlgorithm<int, PopulationState<int>> receiver = ga;

        var error = Should.Throw<InvalidOperationException>(() => receiver
            .Substitute(ga.Mutator, mutator => mutator.Named("a"))
            .Substitute(receiver, algorithm => algorithm.Named("b")));

        ShouldNameTheRoutes(error);
    }

    [Fact]
    public void ReferenceOfASlotContainingTheFirstTarget_IsAnErrorNamingTheRoutes()
    {
        IOperator<Evaluation, int> inner = new Stub<Evaluation>("inner");
        IOperator<Evaluation, int> outer = inner.Named("outer");
        var ga = Ga(evaluator: outer);

        var error = Should.Throw<InvalidOperationException>(() => ga
            .Substitute(inner, _ => new Stub<Evaluation>("replaced"))
            .Substitute(outer, evaluator => evaluator.Named("a")));

        ShouldNameTheRoutes(error);
    }

    [Fact]
    public void ReferenceOfAReplacedNode_IsAnErrorNamingTheRoutes()
    {
        var ga = Ga();

        var error = Should.Throw<InvalidOperationException>(() => ga
            .Substitute(ga.Mutator, _ => new Stub<Mutation>("replacement"))
            .Substitute(ga.Mutator, mutator => mutator.Named("a")));

        ShouldNameTheRoutes(error);
    }

    [Fact]
    public void Routes_OneCallResolvesEveryTargetAgainstTheSameInput()
    {
        IOperator<Evaluation, int> inner = new Stub<Evaluation>("inner");
        IOperator<Evaluation, int> outer = inner.Named("outer");
        var ga = Ga(evaluator: outer);

        var substituted = ga.Substitute(
            Substitution.Of(inner, _ => new Stub<Evaluation>("replaced")),
            Substitution.Of(outer, evaluator => evaluator.Named("a")));

        substituted.Evaluator.ShouldBe(new Stub<Evaluation>("replaced").Named("outer").Named("a"));
    }

    [Fact]
    public void Routes_AncestorsFirstBuildsTheGraphOfOneCall()
    {
        IOperator<Evaluation, int> inner = new Stub<Evaluation>("inner");
        IOperator<Evaluation, int> outer = inner.Named("outer");
        var ga = Ga(evaluator: outer);

        var ancestorsFirst = ga
            .Substitute(outer, evaluator => evaluator.Named("a"))
            .Substitute(inner, evaluator => evaluator.Named("b"));
        var oneCall = ga.Substitute(
            Substitution.Of(inner, evaluator => evaluator.Named("b")),
            Substitution.Of(outer, evaluator => evaluator.Named("a")));

        ancestorsFirst.ShouldBe(oneCall);
    }

    [Fact]
    public void Routes_NamingTheTargetFromTheCurrentConfiguration()
    {
        IOperator<Evaluation, int> inner = new Stub<Evaluation>("inner");
        IOperator<Evaluation, int> outer = inner.Named("outer");
        var ga = Ga(evaluator: outer);

        var changed = ga.Substitute(inner, _ => new Stub<Evaluation>("replaced"));
        var substituted = changed.Substitute(changed.Evaluator, evaluator => evaluator.Named("a"));

        substituted.Evaluator.ShouldBe(new Stub<Evaluation>("replaced").Named("outer").Named("a"));
    }

    // Regressions and further cases from the review of the first version.

    [Fact]
    public void ASlotTypedForAnotherOperation_HoldsTheCopyTheWrapperInTheMatchingSlotHolds()
    {
        IOperator<Mutation, int> child = new Stub<Mutation>("child");
        var both = new MutationAndCrossoverWithChild(child);
        var ga = Ga(mutator: both, crossover: both);

        var substituted = ga.Substitute((IOperator<Mutation, int> mutator) => mutator.Named("a"));

        // Both operations still reach one configuration, so the scope's table still gives them one executor.
        var wrapper = substituted.Mutator.ShouldBeOfType<Named<Mutation, int>>();
        wrapper.ChildOperator.ShouldBeSameAs(substituted.Crossover);
        substituted.Crossover.ShouldBe(new MutationAndCrossoverWithChild(child.Named("a")));
    }

    [Fact]
    public void ATargetBelowASharingEntryOnly_IsNotAPlacement()
    {
        var ga = Ga();
        IOperator<Mutation, int> hidden = new Stub<Mutation>("hidden");

        Should.Throw<InvalidOperationException>(() =>
            ga.Sharing(hidden.Named("outer")).Substitute(hidden, mutator => mutator.Named("a")));
        Should.Throw<InvalidOperationException>(() =>
            ga.Sharing(hidden.Named("outer")).Sharing(hidden.Named("other")).Substitute(hidden, mutator => mutator.Named("a")));
    }

    [Fact]
    public void ATargetASharingEntryVisitsFirst_IsStillAPlacementWhereASlotHoldsIt()
    {
        IOperator<Mutation, int> target = new Stub<Mutation>("target");
        var named = target.Named("outer");
        var first = Ga().Sharing(named);
        var second = Ga(mutator: named);
        var cycle = CycleAlgorithm.Create<int, PopulationState<int>>(first, second);

        var substituted = cycle.Substitute(target, mutator => mutator.Named("a"));

        var expected = target.Named("a").Named("outer");
        ((GeneticAlgorithm<int>)substituted.Algorithms[1]).Mutator.ShouldBe(expected);
        ((SharingAlgorithm<int, PopulationState<int>>)substituted.Algorithms[0]).Shared[0]
            .ShouldBeSameAs(((GeneticAlgorithm<int>)substituted.Algorithms[1]).Mutator);
    }

    [Fact]
    public void ASharingEntryFollowingAReplacement_SearchesRepeatedReferencesOnce()
    {
        var ga = Ga();
        IOperator<Mutation, int> replacement = new Stub<Mutation>("leaf");
        for (var level = 0; level < 40; level++)
            replacement = new MultiMutator(replacement, replacement);

        var substituted = ga.Sharing(ga.Mutator).Substitute(ga.Mutator, _ => replacement);

        substituted.Shared[0].ShouldBeSameAs(replacement);
    }

    [Fact]
    public void Type_SelectsAWrapperAnEarlierSubstitutionOfTheCallIntroduced()
    {
        var ga = Ga();

        var substituted = ga.Substitute(
            Substitution.Of(ga.Mutator, mutator => mutator.Named("a")),
            Substitution.Of<Named<Mutation, int>>(named => named with { Name = "b" }));

        substituted.Mutator.ShouldBe(Mutator.Named("b"));
    }

    [Fact]
    public void ACopyStaysItselfForMatchingAcrossSeveralSubstitutionsOfTheCall()
    {
        IOperator<Mutation, int> inner = new Stub<Mutation>("inner");
        IOperator<Mutation, int> outer = inner.Named("outer");
        var ga = Ga(mutator: outer);

        var substituted = ga.Substitute(
            Substitution.Of(inner, mutator => mutator.Named("first")),
            Substitution.Of(inner, mutator => mutator.Named("second")),
            Substitution.Of(outer, mutator => mutator.Named("third")));

        substituted.Mutator.ShouldBe(inner.Named("second").Named("first").Named("outer").Named("third"));
    }

    [Fact]
    public void AReplacementReturningItsInput_ChangesNothing()
    {
        var ga = Ga();

        ga.Substitute(ga.Mutator, mutator => mutator).ShouldBeSameAs(ga);
    }

    [Fact]
    public void AReplacementAlreadyInTheGraph_IsNotEntered()
    {
        IOperator<Mutation, int> replaced = new Stub<Mutation>("replaced");
        IOperator<Mutation, int> present = new Stub<Mutation>("present");
        var cycle = CycleAlgorithm.Create<int, PopulationState<int>>(Ga(mutator: replaced), Ga(mutator: present));

        var substituted = cycle.Substitute((IOperator<Mutation, int> mutator) =>
            ReferenceEquals(mutator, replaced) ? present : mutator.Named("a"));

        ((GeneticAlgorithm<int>)substituted.Algorithms[0]).Mutator.ShouldBeSameAs(present);
        ((GeneticAlgorithm<int>)substituted.Algorithms[1]).Mutator.ShouldBe(present.Named("a"));
    }

    [Fact]
    public void ValueEqualNodes_AreSeparateNodes()
    {
        var calls = 0;
        var cycle = CycleAlgorithm.Create<int, PopulationState<int>>(Ga(mutator: new Stub<Mutation>("same")), Ga(mutator: new Stub<Mutation>("same")));

        var substituted = cycle.Substitute((IOperator<Mutation, int> mutator) =>
        {
            calls++;
            return mutator.Named("a");
        });

        calls.ShouldBe(2);
        ((GeneticAlgorithm<int>)substituted.Algorithms[0]).Mutator
            .ShouldNotBeSameAs(((GeneticAlgorithm<int>)substituted.Algorithms[1]).Mutator);
    }

    private static void ShouldNameTheRoutes(InvalidOperationException error)
    {
        error.Message.ShouldContain("one Substitute call");
        error.Message.ShouldContain("ancestors first");
        error.Message.ShouldContain("current configuration");
    }

    private sealed record Stub<TOperation>(string Id) : Operator<TOperation, int>
        where TOperation : IOperation<TOperation>
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
            throw new NotSupportedException();
    }

    private sealed record MultiMutator : MultiOperator<Mutation, int>
    {
        public MultiMutator(params IReadOnlyList<IOperator<Mutation, int>> children) : base(children) { }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
            throw new NotSupportedException();
    }

    private sealed record Hiding(IOperator<Mutation, int> Hidden) : Operator<Mutation, int>
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
            throw new NotSupportedException();
    }

    private sealed record HoldingAConcreteAlgorithm(GeneticAlgorithm<int> Inner) : Operator<Mutation, int>
    {
        private static Parameter<HoldingAConcreteAlgorithm, GeneticAlgorithm<int>> InnerParameter { get; } =
            new(nameof(Inner), static holder => holder.Inner, static (holder, value) => holder with { Inner = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), InnerParameter];

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
            throw new NotSupportedException();
    }

    private sealed record Building(Func<IOperator<Mutation, int>> Build) : Operator<Mutation, int>
    {
        public static Parameter<Building, Func<IOperator<Mutation, int>>> BuildParameter { get; } =
            new(nameof(Build), static building => building.Build, static (building, value) => building with { Build = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), BuildParameter];

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) =>
            throw new NotSupportedException();
    }

    private sealed record MutationAndCrossoverWithChild(IOperator<Mutation, int> Child) : IOperator<Mutation, int>, IOperator<Crossover, int>
    {
        private static Parameter<MutationAndCrossoverWithChild, IOperator<Mutation, int>> ChildSlot { get; } =
            new(nameof(Child), static both => both.Child, static (both, value) => both with { Child = value });

        public IReadOnlyList<IParameter> GetParameters() => [ChildSlot];

        public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
            where TSearchState : class, ISearchState =>
            throw new NotSupportedException();
    }

    private sealed record MutationAndCrossover : IOperator<Mutation, int>, IOperator<Crossover, int>
    {
        public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
            where TSearchState : class, ISearchState =>
            throw new NotSupportedException();
    }
}
