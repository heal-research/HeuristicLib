using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class MixedOperatorResolutionTests
{
    private static readonly TestFunctionProblem Problem = new(new SphereFunction(1));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypedAndUntypedResolutionShareTheExecutorOfTheirScopeInEitherOrder(bool untypedFirst)
    {
        var source = new DualCrossover();
        var counter = new Counter();
        var root = ResolutionScope.Create();
        var typed = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        IOperator<Crossover, RealVector> counted = source.Counted(counter);
        IOperator<Crossover, RealVector> forwarded = counted;
        IOperator<Crossover, RealVector> current = counted;
        var first = untypedFirst ? root.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(forwarded) : typed.Resolve(current);
        var second = untypedFirst ? typed.Resolve(current) : root.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(forwarded);
        second.ShouldBeSameAs(first);
        Invoke(first).ShouldBe(1);
        Invoke(second).ShouldBe(2);
        var child = root.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var operation = child.Resolve(current, out ILiveValue? control);
        control.ShouldNotBeNull().Value.ShouldBe(0);
        control.Value = 12;
        Invoke(operation).ShouldBe(13);
        source.Preparations.ShouldBe(2);
        root.Results.Result(counter).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void MixedChildrenUseTheSameAncestryAndWrappersInBothDirections()
    {
        var source = new DualCrossover();
        var counter = new Counter();
        IOperator<Crossover, RealVector> current = source.Counted(counter);
        IOperator<Crossover, RealVector> forwarded = new ForwardedCrossover(current);
        var outer = new CurrentForward(forwarded);
        var root = ResolutionScope.Create();
        var parent = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var parentNode = parent.Resolve(outer);
        var descendantNode = root.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(outer);
        Invoke(parentNode).ShouldBe(1);
        Invoke(descendantNode).ShouldBe(1);
        source.Preparations.ShouldBe(2);
        root.Results.Result(counter).CurrentCount.ShouldBe(2);

        var independent = ResolutionScope.Create();
        var child = independent.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(outer);
        var ancestor = independent.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(outer);
        Invoke(child).ShouldBe(1);
        Invoke(ancestor).ShouldBe(1);
        source.Preparations.ShouldBe(4);
    }

    private static double Invoke(ICrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> operation) =>
        operation.Cross([Parents.From(new RealVector(0.0), new RealVector(0.0))], RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem)[0][0];

    private sealed class DualCrossover : IOperator<Crossover, RealVector>
    {
        public int Preparations { get; private set; }

        // Each executor owns its live value.
        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            Preparations++;
            return new Executor();
        }

        private sealed class Executor : ILiveValue,
            ICrossoverExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>
        {
            public int Value { get; set; }

            public IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random,
                ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
                [new RealVector((double)++Value)];
        }

    }

    private sealed record ForwardedCrossover(IOperator<Crossover, RealVector> Child) : IOperator<Crossover, RealVector>
    {
        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => scope.ResolveOperator(Child);
    }

    private sealed record CurrentForward(IOperator<Crossover, RealVector> Child) : IOperator<Crossover, RealVector>
    {
        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => scope.Resolve(Child);
    }
}
