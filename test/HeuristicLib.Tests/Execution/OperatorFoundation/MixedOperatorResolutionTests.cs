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
    public void NominalResolutionAdaptsToTheDeclaredPreparedOwnerInEitherOrder(bool legacyFirst)
    {
        var source = new DualMutation();
        var counter = new CallCounter();
        var log = new AdviceLog();
        var root = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new CountCalls(counter, log)));
        var typed = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        IMutator<RealVector> legacy = source;
        IOperator<RealVector, Mutation> current = source;
        var first = legacyFirst ? typed.Resolve(legacy) : typed.Resolve(current);
        var second = legacyFirst ? typed.Resolve(current) : typed.Resolve(legacy);
        Invoke(first).ShouldBe(1);
        Invoke(second).ShouldBe(2);
        var child = root.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var operation = child.Resolve(legacy, out ILiveValue? control);
        control.ShouldNotBeNull().Value.ShouldBe(2);
        control.Value = 12;
        Invoke(operation).ShouldBe(13);
        source.Preparations.ShouldBe(1);
        source.LegacyPreparations.ShouldBe(0);
        log.Preparations.ShouldBe(1);
        counter.Value.ShouldBe(3);
    }

    [Fact]
    public void MixedChildrenUseTheSameAncestryAndAspectsInBothDirections()
    {
        var source = new DualMutation();
        IOperator<RealVector, Mutation> current = source;
        IMutator<RealVector> legacy = new LegacyForward(current);
        var outer = new CurrentForward(legacy);
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
    public void LowLevelCallbacksCannotCreateAnIndependentOwnerForTheSameConfiguration(bool legacyFirst)
    {
        var source = new DualMutation();
        var root = ResolutionScope.Create();
        var typed = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> LegacyCallback() =>
            root.Resolve<DualMutation, IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>>(
                source, _ => _ => new DualMutation.Executor(), (_, _) => static child => child);
        if (legacyFirst)
        {
            _ = LegacyCallback();
            Assert.Throws<InvalidOperationException>(() => typed.Resolve((IOperator<RealVector, Mutation>)source))
                .Message.ShouldContain("preparation path");
            source.Preparations.ShouldBe(0);
        }
        else
        {
            _ = typed.Resolve((IOperator<RealVector, Mutation>)source);
            Assert.Throws<InvalidOperationException>(() => LegacyCallback()).Message.ShouldContain("preparation path");
            source.Preparations.ShouldBe(1);
        }
    }

    private static double Invoke(IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> operation) =>
        operation.Mutate([new RealVector(0.0)], RandomNumberGenerator.Create(1), Problem.SearchSpace, Problem)[0][0];

    private sealed class DualMutation : IMutator<RealVector>, IOperator<RealVector, Mutation>
    {
        public int Preparations { get; private set; }
        public int LegacyPreparations { get; private set; }

        public OperatorExecutorFactory CreateExecutorFactory()
        {
            Preparations++;
            var executor = new Executor();
            return OperatorExecutorFactory.FromFactory<Executor>(_ => executor);
        }

        public ExecutorFactory<IMutationExecutor<RealVector, TSearchSpace, TProblem>> CreateExecutorFactory<TSearchSpace, TProblem>()
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            LegacyPreparations++;
            throw new InvalidOperationException("The declared prepared owner must be used.");
        }

        internal sealed class Executor : ILiveValue,
            IMutationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>
        {
            public int Value { get; set; }

            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random,
                ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
                [new RealVector((double)++Value)];
        }
    }

    private sealed record LegacyForward(IOperator<RealVector, Mutation> Child) : IMutator<RealVector>
    {
        public ExecutorFactory<IMutationExecutor<RealVector, TSearchSpace, TProblem>> CreateExecutorFactory<TSearchSpace, TProblem>()
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace> =>
            scope => scope.For<RealVector, TSearchSpace, TProblem>().Resolve(Child);
    }

    private sealed record CurrentForward(IMutator<RealVector> Child) : IOperator<RealVector, Mutation>
    {
        public OperatorExecutorFactory CreateExecutorFactory() => new Factory(Child);

        private sealed class Factory(IMutator<RealVector> child) : OperatorExecutorFactory
        {
            public override ExecutorFactory GetExecutorFactory<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
                OperationExecutorAdapter<TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState> adapter)
            {
                if (child is not IMutator<TCandidate> typed || typeof(TOperation) != typeof(Mutation))
                    throw new InvalidOperationException("Wrong operation view.");
                return ExecutorFactory.FromFactory<IMutationExecutor<TCandidate, TSearchSpace, TProblem>>(
                    scope => scope.For<TCandidate, TSearchSpace, TProblem>().Resolve(typed));
            }
        }
    }
}
