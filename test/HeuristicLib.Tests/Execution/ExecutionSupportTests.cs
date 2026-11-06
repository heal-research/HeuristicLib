using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Execution;

// Which configurations are asked whether they support a run, and about which run.
// See plans/core-simplification-plan.md, D47 and D48.
public class ExecutionSupportTests
{
    private static readonly ExecutionSignature Run = For<RunSpace>();
    private static readonly ExecutionSignature Inner = For<InnerSpace>();

    private static readonly IOperator<Creation, int> ForRun = new CreatorFor<RunSpace>();
    private static readonly IOperator<Creation, int> ForInner = new CreatorFor<InnerSpace>();

    private static ExecutionSignature For<TSearchSpace>()
        where TSearchSpace : class, ISearchSpace<int> =>
        ExecutionSignature.For<TSearchSpace, IProblem<int, TSearchSpace>, ISearchState>();

    [Fact]
    public void AnUnsetOptionalSlot_IsSupported()
    {
        Run.Supports(null).ShouldBeTrue();
    }

    [Fact]
    public void ADeclaredChild_IsAskedAboutTheExecutionOfItsOwner()
    {
        Run.Supports(new Passing(ForRun)).ShouldBeTrue();
        Run.Supports(new Passing(ForInner)).ShouldBeFalse();
        Run.Supports(new Passing(new Passing(ForInner))).ShouldBeFalse();
    }

    // These answered yes for any child before, because each left out the hand-written forward.
    [Fact]
    public void ACompositionWritesNoCodeToAnswerForItsChildren()
    {
        Run.Supports(new EliteSelector<int>(new SelectorFor<RunSpace>())).ShouldBeTrue();
        Run.Supports(new EliteSelector<int>(new SelectorFor<InnerSpace>())).ShouldBeFalse();
        Run.Supports(new PredefinedCandidatesCreator<int>([1], ForRun)).ShouldBeTrue();
        Run.Supports(new PredefinedCandidatesCreator<int>([1], ForInner)).ShouldBeFalse();
    }

    [Fact]
    public void ChildExecution_IsTheOwnersUnlessTheParameterStatesAnother()
    {
        ((IParameter)Passing.ChildParameter).ChildExecution(Run).ShouldBe(Run);
        ((IParameter)Adapting.ChildParameter).ChildExecution(Run).ShouldBe(Inner);
    }

    [Fact]
    public void AForwardedChild_IsAskedAboutTheExecutionItsParameterStates()
    {
        Run.Supports(new Adapting(ForInner)).ShouldBeTrue();
        Run.Supports(new Adapting(ForRun)).ShouldBeFalse();
    }

    // Everything below a forward belongs to the inner execution.
    [Fact]
    public void AForwardReachesEverythingBelowIt()
    {
        Run.Supports(new Adapting(new Passing(ForInner))).ShouldBeTrue();
        Run.Supports(new Adapting(new Passing(ForRun))).ShouldBeFalse();
    }

    [Fact]
    public void Validation_ReportsAForwardedChildAgainstTheExecutionItsParameterStates()
    {
        var report = SearchConfigurationValidation.Validate(new Adapting(ForRun), new InnerSpace(), Run);

        var diagnostic = report.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Path.ShouldEndWith($"{nameof(Adapting)}.{nameof(Adapting.Child)}");
        diagnostic.Message.ShouldContain(nameof(InnerSpace));
        diagnostic.Message.ShouldNotContain(nameof(RunSpace));
    }

    [Fact]
    public void Validation_DoesNotReportAForwardedChildThatSupportsItsExecution()
    {
        SearchConfigurationValidation.Validate(new Adapting(ForInner), new RunSpace(), Run).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AReferenceListEntry_IsNotAsked()
    {
        Run.Supports(new Naming([ForInner])).ShouldBeTrue();
    }

    // A meta-optimization candidate can be a configuration, and it is still data to the creator holding it.
    [Fact]
    public void APredefinedCandidateThatIsAConfiguration_IsNotAChild()
    {
        var candidate = new CreatorFor<InnerSpace>();
        var creator = new CandidateCreator();
        var source = new PredefinedCandidatesCreator<CreatorFor<InnerSpace>>([candidate], creator);
        var execution = ExecutionSignature.For<CandidateSpace, IProblem<CreatorFor<InnerSpace>, CandidateSpace>, ISearchState>();

        source.GetParameters().SelectMany(parameter => parameter.GetChildren(source)).ShouldBe([creator]);
        // The candidate is written for another search space, so asking it would answer no.
        execution.Supports(candidate).ShouldBeFalse();
        execution.Supports(source).ShouldBeTrue();
        var parameter = PredefinedCandidatesCreator<CreatorFor<InnerSpace>>.PredefinedCandidatesParameter;
        parameter.Get(source).ShouldBe(source.PredefinedCandidates);
        parameter.With(source, []).PredefinedCandidates.ShouldBeEmpty();
    }

    // A single value typed by a type parameter has no list to tell it apart from a child.
    [Fact]
    public void ASettingThatIsAConfiguration_IsNotAsked()
    {
        Run.Supports(new StoppingAt<IConfigurationNode>(ForInner)).ShouldBeTrue();
    }

    [Fact]
    public void AResolveFailingOnAChild_NamesTheChild()
    {
        var scope = ResolutionScope.Create().For<int, RunSpace, IProblem<int, RunSpace>>();

        var message = Should.Throw<InvalidOperationException>(() => scope.Resolve(new EliteSelector<int>(new SelectorFor<InnerSpace>()))).Message;

        message.ShouldContain("SelectorFor");
        message.ShouldContain(nameof(EliteSelector<>.SelectorForRemaining));
    }

    [Fact]
    public void AResolveFailingOnTheOperatorItself_NamesWhatItIsWrittenFor()
    {
        var scope = ResolutionScope.Create().For<int, RunSpace, IProblem<int, RunSpace>>();

        var message = Should.Throw<InvalidOperationException>(() => scope.Resolve(ForInner)).Message;

        message.ShouldContain($"is written for {nameof(InnerSpace)}");
        message.ShouldContain(nameof(RunSpace));
    }

    [Fact]
    public void AnUndeclaredChild_IsNotAsked()
    {
        Run.Supports(new Undeclared(ForInner)).ShouldBeTrue();
    }

    private sealed class RunSpace : ISearchSpace<int>
    {
        public bool Contains(int candidate) => true;
    }

    private sealed class InnerSpace : ISearchSpace<int>
    {
        public bool Contains(int candidate) => true;
    }

    private sealed class CandidateSpace : ISearchSpace<CreatorFor<InnerSpace>>
    {
        public bool Contains(CreatorFor<InnerSpace> candidate) => true;
    }

    private sealed record CreatorFor<TSearchSpace> : StatelessCreator<int, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<int>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace) => new int[count];
    }

    private sealed record CandidateCreator : StatelessCreator<CreatorFor<InnerSpace>>
    {
        public override IReadOnlyList<CreatorFor<InnerSpace>> Create(int count, IRandomNumberGenerator random) => new CreatorFor<InnerSpace>[count];
    }

    private sealed record SelectorFor<TSearchSpace> : StatelessSelector<int, TSearchSpace>
        where TSearchSpace : class, ISearchSpace<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            population;
    }

    private sealed record Passing(IConfigurationNode Child) : IConfigurationNode
    {
        public static Parameter<Passing, IConfigurationNode> ChildParameter { get; } =
            new(nameof(Child), static passing => passing.Child, static (passing, value) => passing with { Child = value });

        public IReadOnlyList<IParameter> GetParameters() => [ChildParameter];
    }

    private sealed record Adapting(IConfigurationNode Child) : IConfigurationNode
    {
        public static Parameter<Adapting, IConfigurationNode> ChildParameter { get; } =
            new(nameof(Child), static adapting => adapting.Child, static (adapting, value) => adapting with { Child = value })
            {
                ChildExecution = static _ => Inner
            };

        public IReadOnlyList<IParameter> GetParameters() => [ChildParameter];
    }

    private sealed record Naming(IReadOnlyList<IConfigurationNode> Targets) : IConfigurationNode
    {
        public static ReferenceListParameter<Naming> TargetsParameter { get; } =
            new(nameof(Targets), static naming => naming.Targets, static (naming, value) => naming with { Targets = value });

        public IReadOnlyList<IParameter> GetParameters() => [TargetsParameter];
    }

    private sealed record Undeclared(IConfigurationNode Child) : IConfigurationNode;

    private sealed record StoppingAt<TCandidate>(TCandidate Target) : IConfigurationNode
    {
        public static SettingParameter<StoppingAt<TCandidate>, TCandidate> TargetParameter { get; } =
            new(nameof(Target), static stopping => stopping.Target, static (stopping, value) => stopping with { Target = value });

        public IReadOnlyList<IParameter> GetParameters() => [TargetParameter];
    }
}
