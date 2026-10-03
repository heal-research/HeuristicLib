using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using CompositeCandidate = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<string, string>;
using CompositeSpace = HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<string, HEAL.HeuristicLib.Tests.TestSupport.Mocks.UnrestrictedSearchSpace<string>, string, HEAL.HeuristicLib.Tests.TestSupport.Mocks.UnrestrictedSearchSpace<string>>;

namespace HEAL.HeuristicLib.Tests.Experimental.Operators;

public class CompositeExecutionFactoryTests
{
    [Fact]
    public void CompositeCreator_RebindsAdaptedChildrenWithoutResettingTheirCursors()
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
        var child = root.CreateChildScope(builder => builder.Wrap<ICreator<string>>(left, original => original.CountCalls(calls)));
        var inner = child.Resolve<CompositeCandidate, CompositeSpace, IProblem<CompositeCandidate, CompositeSpace>>(source);

        outer.Create(1, RandomNumberGenerator.Create(1), searchSpace, problem).Single().ShouldBe(new CompositeCandidate("a", "x"));
        inner.Create(1, RandomNumberGenerator.Create(1), searchSpace, problem).Single().ShouldBe(new CompositeCandidate("b", "y"));
        outer.Create(1, RandomNumberGenerator.Create(1), searchSpace, problem).Single().ShouldBe(new CompositeCandidate("c", "z"));
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

    private sealed record FallbackCreator : StatelessCreator<string, UnrestrictedSearchSpace<string>, IProblem<string, UnrestrictedSearchSpace<string>>>
    {
        public override IReadOnlyList<string> Create(int count, IRandomNumberGenerator random, UnrestrictedSearchSpace<string> searchSpace, IProblem<string, UnrestrictedSearchSpace<string>> problem) => Enumerable.Repeat("fallback", count).ToArray();
    }
}
