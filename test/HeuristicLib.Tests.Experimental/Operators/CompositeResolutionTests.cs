using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using CompositeCandidate = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<string, string>;
using CompositeSpace = HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<string, HEAL.HeuristicLib.Tests.TestSupport.Mocks.UnrestrictedSearchSpace<string>, string, HEAL.HeuristicLib.Tests.TestSupport.Mocks.UnrestrictedSearchSpace<string>>;

namespace HEAL.HeuristicLib.Tests.Experimental.Operators;

public class CompositeResolutionTests
{
    [Fact]
    public void CompositeCreator_ConstructsIndependentAdaptedChildCursors()
    {
        var searchSpace = new CompositeSpace(UnrestrictedSearchSpace<string>.Instance, UnrestrictedSearchSpace<string>.Instance);
        var problem = new NoProblem<CompositeCandidate, CompositeSpace>(searchSpace);
        var fallback = new FallbackCreator();
        var left = new PredefinedCandidatesCreator<string>(["a", "b", "c"], fallback);
        var right = new PredefinedCandidatesCreator<string>(["x", "y", "z"], fallback);
        var source = searchSpace.CombineCreators(left, right);
        var root = ResolutionScope.Create();
        var outer = root.Resolve<CompositeCandidate, CompositeSpace, IProblem<CompositeCandidate, CompositeSpace>>(source);
        var calls = new CountAccumulator();
        var observed = source.AnalyzedBy(left.Invocations.Observing(_ => calls.IncrementBy(1)));
        var child = root.CreateChildScope();
        var inner = child.Resolve<CompositeCandidate, CompositeSpace, IProblem<CompositeCandidate, CompositeSpace>>(observed);

        outer.Create(1, RandomNumberGenerator.Create(1), searchSpace, problem).Single().ShouldBe(new CompositeCandidate("a", "x"));
        inner.Create(1, RandomNumberGenerator.Create(1), searchSpace, problem).Single().ShouldBe(new CompositeCandidate("a", "x"));
        outer.Create(1, RandomNumberGenerator.Create(1), searchSpace, problem).Single().ShouldBe(new CompositeCandidate("b", "y"));
        calls.CurrentCount.ShouldBe(1);
        var independent = ResolutionScope.Create().Resolve<CompositeCandidate, CompositeSpace, IProblem<CompositeCandidate, CompositeSpace>>(source);
        independent.Create(1, RandomNumberGenerator.Create(1), searchSpace, problem).Single().ShouldBe(new CompositeCandidate("a", "x"));
    }

    [Fact]
    public void CompositeCreator_RejectsAnIncompatibleOuterSearchSpace()
    {
        var searchSpace = new CompositeSpace(UnrestrictedSearchSpace<string>.Instance, UnrestrictedSearchSpace<string>.Instance);
        var source = searchSpace.CombineCreators(new FallbackCreator(), new FallbackCreator());
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve<CompositeCandidate, UnrestrictedSearchSpace<CompositeCandidate>, IProblem<CompositeCandidate, UnrestrictedSearchSpace<CompositeCandidate>>>(source));
    }

    // A part is written for the search space of its side, which is not the composite's, so the outer execution would reject it.
    [Fact]
    public void CompositeCreator_AsksEachPartAboutTheSearchSpaceOfItsSide()
    {
        var searchSpace = new CompositeSpace(UnrestrictedSearchSpace<string>.Instance, UnrestrictedSearchSpace<string>.Instance);
        var execution = ExecutionSignature.For<CompositeSpace, IProblem<CompositeCandidate, CompositeSpace>, ISearchState>();
        var valid = searchSpace.CombineCreators(new FallbackCreator(), new FallbackCreator());
        var invalid = searchSpace.CombineCreators(new FallbackCreator(), new CreatorForAnotherSpace());

        execution.Supports(valid).ShouldBeTrue();
        SearchConfigurationValidation.Validate(valid, searchSpace, execution).IsValid.ShouldBeTrue();
        execution.Supports(invalid).ShouldBeFalse();
        var diagnostic = SearchConfigurationValidation.Validate(invalid, searchSpace, execution).Diagnostics.ShouldHaveSingleItem();
        diagnostic.Path.ShouldEndWith(".Operator2");
        diagnostic.Message.ShouldContain(nameof(CreatorForAnotherSpace));
        diagnostic.Message.ShouldContain(nameof(UnrestrictedSearchSpace<>));
    }

    private sealed class AnotherSpace : ISearchSpace<string>
    {
        public bool Contains(string candidate) => true;
    }

    private sealed record CreatorForAnotherSpace : StatelessCreator<string, AnotherSpace>
    {
        public override IReadOnlyList<string> Create(int count, IRandomNumberGenerator random, AnotherSpace searchSpace) => new string[count];
    }

    private sealed record FallbackCreator : StatelessCreator<string, UnrestrictedSearchSpace<string>, IProblem<string, UnrestrictedSearchSpace<string>>>
    {
        public override IReadOnlyList<string> Create(int count, IRandomNumberGenerator random, UnrestrictedSearchSpace<string> searchSpace, IProblem<string, UnrestrictedSearchSpace<string>> problem) => Enumerable.Repeat("fallback", count).ToArray();
    }
}
