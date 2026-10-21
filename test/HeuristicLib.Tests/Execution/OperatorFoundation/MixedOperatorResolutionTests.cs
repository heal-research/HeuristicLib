using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation.Fixtures;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

public sealed class MixedOperatorResolutionTests
{
    private static readonly TestFunctionProblem Problem = new(new SphereFunction(1));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypedAndUntypedResolutionShareThePreparedOwnerInEitherOrder(bool untypedFirst)
    {
        var source = new DualCrossover();
        var counter = new CallCounter();
        var log = new AdviceLog();
        var root = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CountCalls(counter, log)));
        var typed = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        IOperator<Crossover, RealVector> forwarded = source;
        IOperator<Crossover, RealVector> current = source;
        var first = untypedFirst ? root.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(forwarded) : typed.Resolve(current);
        var second = untypedFirst ? typed.Resolve(current) : root.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(forwarded);
        Invoke(first).ShouldBe(1);
        Invoke(second).ShouldBe(2);
        var child = root.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var operation = child.Resolve(current, out ILiveValue? control);
        control.ShouldNotBeNull().Value.ShouldBe(2);
        control.Value = 12;
        Invoke(operation).ShouldBe(13);
        source.Preparations.ShouldBe(1);
        log.Wraps.ShouldBe(3);
        counter.Value.ShouldBe(3);
    }

    [Fact]
    public void MixedChildrenUseTheSameAncestryAndAspectsInBothDirections()
    {
        var source = new DualCrossover();
        IOperator<Crossover, RealVector> current = source;
        IOperator<Crossover, RealVector> forwarded = new ForwardedCrossover(current);
        var outer = new CurrentForward(forwarded);
        var counter = new CallCounter();
        var root = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CountCalls(counter, new AdviceLog())));
        var parent = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var parentNode = parent.Resolve(outer);
        var descendantNode = root.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(outer);
        Invoke(parentNode).ShouldBe(1);
        Invoke(descendantNode).ShouldBe(2);
        source.Preparations.ShouldBe(1);
        counter.Value.ShouldBe(2);

        var independent = ResolutionScope.Create();
        var child = independent.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(outer);
        var ancestor = independent.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(outer);
        Invoke(child).ShouldBe(1);
        Invoke(ancestor).ShouldBe(1);
        source.Preparations.ShouldBe(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LowLevelCallbacksCannotCreateAnIndependentOwnerForTheSameConfiguration(bool untypedFirst)
    {
        var source = new DualCrossover();
        var root = ResolutionScope.Create();
        var typed = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        ICrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> LowLevelCallback() =>
            root.Resolve<DualCrossover, ICrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>>(
                source, _ => _ => new DualCrossover.Executor(), (_, _) => static child => child);
        if (untypedFirst)
        {
            _ = LowLevelCallback();
            Assert.Throws<InvalidOperationException>(() => typed.Resolve((IOperator<Crossover, RealVector>)source))
                .Message.ShouldContain("preparation path");
            source.Preparations.ShouldBe(0);
        }
        else
        {
            _ = typed.Resolve((IOperator<Crossover, RealVector>)source);
            Assert.Throws<InvalidOperationException>(() => LowLevelCallback()).Message.ShouldContain("preparation path");
            source.Preparations.ShouldBe(1);
        }
    }

    private static double Invoke(ICrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> operation) =>
        operation.Cross([Parents.From(new RealVector(0.0), new RealVector(0.0))], RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem)[0][0];

    private sealed class DualCrossover : IOperator<Crossover, RealVector>
    {
        public int Preparations { get; private set; }

        public OperatorExecutorFactory CreateExecutorFactory()
        {
            Preparations++;
            var executor = new Executor();
            return OperatorExecutorFactory.FromFactory<Executor>(_ => executor);
        }

        internal sealed class Executor : ILiveValue,
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
        public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.Forward(Child);

    }

    private sealed record CurrentForward(IOperator<Crossover, RealVector> Child) : IOperator<Crossover, RealVector>
    {
        public OperatorExecutorFactory CreateExecutorFactory() => new Factory(Child);

        private sealed class Factory(IOperator<Crossover, RealVector> child) : OperatorExecutorFactory<Crossover, RealVector>
        {
            protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => scope.Resolve(child);
        }
    }
}
