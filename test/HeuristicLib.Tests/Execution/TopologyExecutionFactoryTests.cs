using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public sealed class TopologyExecutionFactoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConcreteComposite_RebindsItsChildWithoutResettingState(bool chooseOne)
    {
        var preparations = 0;
        var reports = new List<double>();
        var leaf = new CountingLeaf(() => preparations++);
        IMutator<RealVector> source = chooseOne ? new ChooseOneMutator<RealVector>([leaf]) : new PipelineMutator<RealVector>([leaf]);
        var parent = ResolutionScope.Create();
        var outer = Resolve(parent, source);
        Next(outer).ShouldBe(1);

        var child = parent.CreateChildScope(builder => builder.Wrap<IMutator<RealVector>>(leaf, original => new ObservingWrapper(original, reports.Add)));
        var inner = Resolve(child, source);
        inner.ShouldNotBeSameAs(outer);
        Next(inner).ShouldBe(2);
        Next(outer).ShouldBe(3);
        Next(inner).ShouldBe(4);

        reports.ShouldBe([2, 4]);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void ConcreteComposite_ValidatesDuringPreparationBeforeResolvingChildren()
    {
        var childPreparations = 0;
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = new ChooseOneMutator<RealVector>([leaf]) { Weights = [1, 1] };

        Should.Throw<InvalidOperationException>(() => source.CreateExecutionFactory<BoundedRealVectorSearchSpace, TestFunctionProblem>());

        childPreparations.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Preparation_PrecedesChildResolutionAndRunsOncePerFactory(bool multi)
    {
        var events = new List<string>();
        var leaf = new CountingLeaf(() => events.Add("prepare child"));
        var source = CreateComposite(multi, leaf, () => events.Add("prepare composite"), () => events.Add("bind composite"));

        var factory = source.CreateExecutionFactory<BoundedRealVectorSearchSpace, TestFunctionProblem>();
        events.ShouldBe(["prepare composite"]);

        var scope = ResolutionScope.Create();
        var first = factory(scope);
        Next(first).ShouldBe(101);
        var second = factory(scope.CreateChildScope());
        Next(second).ShouldBe(202);
        first.ShouldNotBeSameAs(second);
        events.ShouldBe(["prepare composite", "prepare child", "bind composite", "bind composite"]);
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

        var child = parent.CreateChildScope(builder => builder.Wrap<IMutator<RealVector>>(leaf, original => new ObservingWrapper(original, reports.Add)));
        var inner = Resolve(child, source);
        inner.ShouldNotBeSameAs(outer);
        Next(inner).ShouldBe(202);
        reports.ShouldBe([2]);

        Next(outer).ShouldBe(303);
        reports.ShouldBe([2]);
        Next(inner).ShouldBe(404);
        reports.ShouldBe([2, 4]);
        preparations.ShouldBe(1);
        childPreparations.ShouldBe(1);
        bindings.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReusedComposite_KeepsItsPinnedChildDespiteAnEarlierChildLocalSelection(bool multi)
    {
        var childPreparations = 0;
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = CreateComposite(multi, leaf, static () => { }, static () => { });
        var parent = ResolutionScope.Create();
        var reports = new List<double>();
        var child = parent.CreateChildScope(builder => builder.Wrap<IMutator<RealVector>>(leaf, original => new ObservingWrapper(original, reports.Add)));
        var localLeaf = Resolve(child, leaf);
        Next(localLeaf).ShouldBe(1);

        var outer = Resolve(parent, source);
        Next(outer).ShouldBe(101);
        var inner = Resolve(child, source);
        Next(inner).ShouldBe(202);
        Next(outer).ShouldBe(303);
        Next(localLeaf).ShouldBe(2);
        reports.ShouldBe([1, 2, 2]);
        childPreparations.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndependentRoots_PrepareIndependentCompositeAndChildState(bool multi)
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
    public void PreparationFailure_IsStickyAndDoesNotResolveChildren(bool multi)
    {
        var preparations = 0;
        var childPreparations = 0;
        var failure = new InvalidOperationException("preparation failed");
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = CreateComposite(multi, leaf, () => { preparations++; throw failure; }, static () => { });
        var parent = ResolutionScope.Create();

        Should.Throw<InvalidOperationException>(() => Resolve(parent, source)).ShouldBeSameAs(failure);
        Should.Throw<InvalidOperationException>(() => Resolve(parent.CreateChildScope(), source)).ShouldBeSameAs(failure);
        preparations.ShouldBe(1);
        childPreparations.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BindingFailure_IsLocalAndAnotherContextReusesThePreparation(bool multi)
    {
        var preparations = 0;
        var childPreparations = 0;
        var bindings = 0;
        var failure = new InvalidOperationException("binding failed");
        var leaf = new CountingLeaf(() => childPreparations++);
        var source = CreateComposite(multi, leaf, () => preparations++, () => { if (++bindings == 1) throw failure; });
        var parent = ResolutionScope.Create();

        Should.Throw<InvalidOperationException>(() => Resolve(parent, source)).ShouldBeSameAs(failure);
        Should.Throw<InvalidOperationException>(() => Resolve(parent, source)).ShouldBeSameAs(failure);
        var child = parent.CreateChildScope(builder => builder.Wrap<IMutator<RealVector>>(leaf, original => new ObservingWrapper(original, static _ => { })));
        Next(Resolve(child, source)).ShouldBe(101);
        preparations.ShouldBe(1);
        childPreparations.ShouldBe(1);
        bindings.ShouldBe(2);
    }

    [Fact]
    public void MultiFactory_PreservesChildOrder()
    {
        var source = new CountingMulti([new AddOneMutator(), new DoubleMutator()], static () => { }, static () => { });

        Next(Resolve(ResolutionScope.Create(), source), input: 3).ShouldBe(108);
    }

    [Fact]
    public void MultiFactory_PreservesRepeatedChildReferencesAndSharesTheirState()
    {
        var preparations = 0;
        var leaf = new CountingLeaf(() => preparations++);
        var source = new CountingMulti([leaf, leaf], static () => { }, static () => { });
        var execution = Resolve(ResolutionScope.Create(), source);

        Next(execution).ShouldBe(102);
        Next(execution).ShouldBe(204);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void MultiFactory_PassesAnEmptyChildCollectionToItsConstructor()
    {
        var source = new CountingMulti([], static () => { }, static () => { });
        var execution = Resolve(ResolutionScope.Create(), source);

        Next(execution, input: 3).ShouldBe(103);
        Next(execution, input: 3).ShouldBe(203);
    }

    private static IMutator<RealVector> CreateComposite(bool multi, IMutator<RealVector> child, Action prepare, Action bind) =>
        multi ? new CountingMulti([child], prepare, bind) : new CountingWrapper(child, prepare, bind);

    private static IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Resolve(ResolutionScope scope, IMutator<RealVector> source) =>
        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source);

    private static double Next(IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> execution, double input = 0)
    {
        var problem = new TestFunctionProblem(new RastriginFunction(1));
        return execution.Mutate([RealVector.Repeat(input, 1)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem)[0][0];
    }

    private sealed class CounterState
    {
        public int Calls;
    }

    private sealed record CountingLeaf(Action Prepare) : StatefulMutator<RealVector, CounterState>
    {
        protected override CounterState CreateInitialState()
        {
            Prepare();
            return new CounterState();
        }

        protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, CounterState state, IRandomNumberGenerator random) =>
            [RealVector.Repeat(++state.Calls, 1)];
    }

    private sealed record CountingWrapper : WrappingMutator<RealVector>
    {
        private readonly Action prepare;
        private readonly Action bind;

        public CountingWrapper(IMutator<RealVector> childMutator, Action prepare, Action bind) : base(childMutator)
        {
            this.prepare = prepare;
            this.bind = bind;
        }

        protected override WrapperExecutionFactory<IMutatorExecution<RealVector, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
        {
            prepare();
            var state = new CounterState();
            return child =>
            {
                bind();
                return new Execution<TRunSearchSpace, TRunProblem>(child, state);
            };
        }

        private sealed class Execution<TSearchSpace, TProblem>(IMutatorExecution<RealVector, TSearchSpace, TProblem> child, CounterState state)
            : WrappingMutatorExecution<RealVector, TSearchSpace, TProblem>(child)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = ChildMutator.Mutate(parents, random, searchSpace, problem);
                return [RealVector.Repeat(result[0][0] + 100 * ++state.Calls, 1)];
            }
        }
    }

    private sealed record CountingMulti : MultiMutator<RealVector>
    {
        private readonly Action prepare;
        private readonly Action bind;

        public CountingMulti(IReadOnlyList<IMutator<RealVector>> childMutators, Action prepare, Action bind) : base(childMutators)
        {
            this.prepare = prepare;
            this.bind = bind;
        }

        protected override CompositeExecutionFactory<IMutatorExecution<RealVector, TRunSearchSpace, TRunProblem>> CreateCompositeFactory<TRunSearchSpace, TRunProblem>()
        {
            prepare();
            var state = new CounterState();
            return children =>
            {
                bind();
                return new Execution<TRunSearchSpace, TRunProblem>(children, state);
            };
        }

        private sealed class Execution<TSearchSpace, TProblem>(ImmutableArray<IMutatorExecution<RealVector, TSearchSpace, TProblem>> children, CounterState state)
            : MultiMutatorExecution<RealVector, TSearchSpace, TProblem>(children)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = parents;
                foreach (var child in ChildMutators)
                    result = child.Mutate(result, random, searchSpace, problem);
                return [RealVector.Repeat(result[0][0] + 100 * ++state.Calls, 1)];
            }
        }
    }

    private sealed record ObservingWrapper : WrappingMutator<RealVector>
    {
        private readonly Action<double> report;

        public ObservingWrapper(IMutator<RealVector> childMutator, Action<double> report) : base(childMutator)
        {
            this.report = report;
        }

        protected override WrapperExecutionFactory<IMutatorExecution<RealVector, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
            child => new Execution<TRunSearchSpace, TRunProblem>(child, report);

        private sealed class Execution<TSearchSpace, TProblem>(IMutatorExecution<RealVector, TSearchSpace, TProblem> child, Action<double> report)
            : WrappingMutatorExecution<RealVector, TSearchSpace, TProblem>(child)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = ChildMutator.Mutate(parents, random, searchSpace, problem);
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
