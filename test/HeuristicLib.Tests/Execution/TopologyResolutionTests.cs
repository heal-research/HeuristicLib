using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public sealed class TopologyResolutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConcreteComposite_RebindsItsChildWithoutResettingState(bool chooseOne)
    {
        var preparations = 0;
        var reports = new List<double>();
        var leaf = new CountingLeaf(() => preparations++);
        IOperator<Mutation, RealVector> source = chooseOne ? new ChoosePerItem<Mutation, RealVector>([leaf]) : new PipelineMutator<RealVector>([leaf]);
        var parent = ResolutionScope.Create();
        var outer = Resolve(parent, source);
        Next(outer).ShouldBe(1);

        var child = parent.CreateChildScope();
        var inner = Resolve(child, source.Substitute(leaf, mutator => new ObservingMutator(mutator, reports.Add)));
        inner.ShouldNotBeSameAs(outer);
        Next(inner).ShouldBe(1);
        Next(outer).ShouldBe(2);
        Next(inner).ShouldBe(2);

        reports.ShouldBe([1, 2]);
        preparations.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompositeTargetWrapperWrapsTheOneExecutorOfItsScope(bool chooseOne)
    {
        var preparations = 0;
        var leaf = new CountingLeaf(() => preparations++);
        IOperator<Mutation, RealVector> source = chooseOne ? new ChoosePerItem<Mutation, RealVector>([leaf]) : new PipelineMutator<RealVector>([leaf]);
        var calls = new Counter();
        var counted = source.Counted(calls);
        var root = ResolutionScope.Create();
        var first = Resolve(root, counted);
        var second = Resolve(root, counted);
        first.ShouldBeSameAs(second);
        Next(first).ShouldBe(1);
        Next(second).ShouldBe(2);
        var reports = new List<double>();
        var child = root.CreateChildScope();
        Next(Resolve(child, new ObservingMutator(counted, reports.Add))).ShouldBe(1);
        root.Results.Result(calls).CurrentCount.ShouldBe(3);
        reports.ShouldBe([1]);
        preparations.ShouldBe(2);
    }

    [Fact]
    public void ConcreteComposite_ValidatesBeforeResolvingChildren()
    {
        var childPreparations = 0;
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = new ChoosePerItem<Mutation, RealVector>([leaf]) { Weights = [1, 1] };

        Should.Throw<InvalidOperationException>(() => Resolve(ResolutionScope.Create(), source));

        childPreparations.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrivateData_PrecedesChildResolutionOnEachConstruction(bool multi)
    {
        var events = new List<string>();
        var leaf = new CountingLeaf(() => events.Add("prepare child"));
        var source = CreateComposite(multi, leaf, () => events.Add("prepare composite"), () => events.Add("bind composite"));

        var scope = ResolutionScope.Create();
        var first = Resolve(scope, source);
        events.ShouldBe(["prepare composite", "prepare child", "bind composite"]);
        Next(first).ShouldBe(101);
        var second = Resolve(scope.CreateChildScope(), source);
        Next(second).ShouldBe(101);
        first.ShouldNotBeSameAs(second);
        events.ShouldBe(["prepare composite", "prepare child", "bind composite", "prepare composite", "prepare child", "bind composite"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DescendantObservation_RebindsChildrenAndPreservesBothStates(bool multi)
    {
        var preparations = 0;
        var childPreparations = 0;
        var bindings = 0;
        var reports = new List<double>();
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = CreateComposite(multi, leaf, () => preparations++, () => bindings++);
        var parent = ResolutionScope.Create();
        var outer = Resolve(parent, source);
        Next(outer).ShouldBe(101);

        var child = parent.CreateChildScope();
        var inner = Resolve(child, source.Substitute(leaf, mutator => new ObservingMutator(mutator, reports.Add)));
        inner.ShouldNotBeSameAs(outer);
        Next(inner).ShouldBe(101);
        reports.ShouldBe([1]);

        Next(outer).ShouldBe(202);
        reports.ShouldBe([1]);
        Next(inner).ShouldBe(202);
        reports.ShouldBe([1, 2]);
        preparations.ShouldBe(2);
        childPreparations.ShouldBe(2);
        bindings.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompositeInAChildScope_UsesTheLeafThatScopeAlreadyBuilt(bool multi)
    {
        var childPreparations = 0;
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = CreateComposite(multi, leaf, static () => { }, static () => { });
        var parent = ResolutionScope.Create();
        var reports = new List<double>();
        var child = parent.CreateChildScope();
        var observedLeaf = new ObservingMutator(leaf, reports.Add);
        var localLeaf = Resolve(child, observedLeaf);
        Next(localLeaf).ShouldBe(1);

        var outer = Resolve(parent, source);
        Next(outer).ShouldBe(101);
        var inner = Resolve(child, source.Substitute(leaf, _ => observedLeaf));
        Next(inner).ShouldBe(102);
        Next(outer).ShouldBe(202);
        Next(localLeaf).ShouldBe(3);
        reports.ShouldBe([1, 2, 3]);
        childPreparations.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndependentRoots_CreateIndependentCompositeAndChildState(bool multi)
    {
        var preparations = 0;
        var childPreparations = 0;
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = CreateComposite(multi, leaf, () => preparations++, static () => { });
        var first = Resolve(ResolutionScope.Create(), source);
        var second = Resolve(ResolutionScope.Create(), source);

        Next(first).ShouldBe(101);
        Next(first).ShouldBe(202);
        Next(second).ShouldBe(101);
        preparations.ShouldBe(2);
        childPreparations.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StateCreationFailure_EndsTheScopeTreeAndDoesNotResolveChildren(bool multi)
    {
        var preparations = 0;
        var childPreparations = 0;
        var failure = new InvalidOperationException("preparation failed");
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = CreateComposite(multi, leaf, () => { preparations++; throw failure; }, static () => { });
        var parent = ResolutionScope.Create();

        Should.Throw<InvalidOperationException>(() => Resolve(parent, source)).ShouldBeSameAs(failure);
        Should.Throw<InvalidOperationException>(() => Resolve(parent.CreateChildScope(), source)).Message.ShouldContain("new scope");
        Should.Throw<InvalidOperationException>(() => Resolve(ResolutionScope.Create(), source)).ShouldBeSameAs(failure);
        preparations.ShouldBe(2);
        childPreparations.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstructionFailureIsRetriedInANewScopeWithFreshPrivateData(bool multi)
    {
        var preparations = 0;
        var childPreparations = 0;
        var bindings = 0;
        var failure = new InvalidOperationException("binding failed");
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = CreateComposite(multi, leaf, () => preparations++, () => { if (++bindings == 1) throw failure; });
        var parent = ResolutionScope.Create();

        Should.Throw<InvalidOperationException>(() => Resolve(parent, source)).ShouldBeSameAs(failure);
        Should.Throw<InvalidOperationException>(() => Resolve(parent, source)).Message.ShouldContain("new scope");
        var retry = ResolutionScope.Create();
        Next(Resolve(retry, source)).ShouldBe(101);
        var child = retry.CreateChildScope();
        Next(Resolve(child, source.Substitute(leaf, mutator => new ObservingMutator(mutator, static _ => { })))).ShouldBe(101);
        preparations.ShouldBe(3);
        childPreparations.ShouldBe(3);
        bindings.ShouldBe(3);
    }

    [Fact]
    public void MultiFactory_PreservesChildOrder()
    {
        var source = new CountingMulti([new AddOneMutator(), new DoubleMutator()], static () => { }, static () => { });

        Next(Resolve(ResolutionScope.Create(), source), input: 3).ShouldBe(108);
    }

    [Fact]
    public void MultiFactory_RepeatedReferencesShareOneChildExecutor()
    {
        var preparations = 0;
        var leaf = new CountingLeaf(() => preparations++);
        var source = new CountingMulti([leaf, leaf], static () => { }, static () => { });
        var executor = Resolve(ResolutionScope.Create(), source);

        Next(executor).ShouldBe(102);
        Next(executor).ShouldBe(204);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void MultiFactory_PassesAnEmptyChildCollectionToItsConstructor()
    {
        var source = new CountingMulti([], static () => { }, static () => { });
        var executor = Resolve(ResolutionScope.Create(), source);

        Next(executor, input: 3).ShouldBe(103);
        Next(executor, input: 3).ShouldBe(203);
    }

    private static IOperator<Mutation, RealVector> CreateComposite(bool multi, IOperator<Mutation, RealVector> child, Action prepare, Action bind) =>
        multi ? new CountingMulti([child], prepare, bind) : new CountingWrapper(child, prepare, bind);

    private static IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Resolve(ResolutionScope scope, IOperator<Mutation, RealVector> source) =>
        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source);

    private static double Next(IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> executor, double input = 0)
    {
        var problem = new TestFunctionProblem(new RastriginFunction(1));
        return executor.Mutate([RealVector.Repeat(input, 1)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem)[0][0];
    }

    private sealed record CountingLeaf(Action Prepare) : Mutator<RealVector>
    {

        protected override IMutationExecutor<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>> CreateExecutor(ResolutionScope scope)
        {
            Prepare();
            return new Executor();
        }

        private sealed class Executor
            : MutationExecutor<RealVector>
        {
            private int invocations;

            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            [RealVector.Repeat(++invocations, 1)];
        }
    }

    private sealed record CountingWrapper : WrappingOperator<Mutation, RealVector>
    {
        private readonly Action prepare;
        private readonly Action bind;

        public CountingWrapper(IOperator<Mutation, RealVector> childMutator, Action prepare, Action bind) : base(childMutator)
        {
            this.prepare = prepare;
            this.bind = bind;
        }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            prepare();
            var child = scope.Resolve(ChildOperator);
            bind();
            return new Executor<TSearchSpace, TProblem>(child);
        }

        private sealed class Executor<TSearchSpace, TProblem>(IMutationExecutor<RealVector, TSearchSpace, TProblem> child)
            : IMutationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            private int invocations;

            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Mutate(parents, random, searchSpace, problem);
                return [RealVector.Repeat(result[0][0] + 100 * ++invocations, 1)];
            }
        }

    }

    private sealed record CountingMulti : MultiOperator<Mutation, RealVector>
    {
        private readonly Action prepare;
        private readonly Action bind;

        public CountingMulti(IReadOnlyList<IOperator<Mutation, RealVector>> childMutators, Action prepare, Action bind) : base(childMutators)
        {
            this.prepare = prepare;
            this.bind = bind;
        }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            prepare();
            ImmutableArray<IMutationExecutor<RealVector, TSearchSpace, TProblem>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            bind();
            return new Executor<TSearchSpace, TProblem>(children);
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IMutationExecutor<RealVector, TSearchSpace, TProblem>> children)
            : IMutationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            private int invocations;

            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = parents;
                foreach (var child in children)
                    result = child.Mutate(result, random, searchSpace, problem);
                return [RealVector.Repeat(result[0][0] + 100 * ++invocations, 1)];
            }
        }

    }

    // Reports the first coordinate of each successful call's first offspring.
    private sealed record ObservingMutator : WrappingOperator<Mutation, RealVector>
    {
        public ObservingMutator(IOperator<Mutation, RealVector> childMutator, Action<double> report)
            : base(childMutator)
        {
            Report = report;
        }

        public Action<double> Report { get; init; }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
            new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(ChildOperator), Report);

        private sealed class Executor<TSearchSpace, TProblem>(IMutationExecutor<RealVector, TSearchSpace, TProblem> child, Action<double> report)
            : IMutationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Mutate(parents, random, searchSpace, problem);
                report(result[0][0]);
                return result;
            }
        }
    }

    private sealed record AddOneMutator : StatelessMutator<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            [RealVector.Repeat(parents[0][0] + 1, 1)];
    }

    private sealed record DoubleMutator : StatelessMutator<RealVector>
    {
        public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            [RealVector.Repeat(parents[0][0] * 2, 1)];
    }
}
