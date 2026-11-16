using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Execution;

public sealed class ProblemBindingTests
{
    [Fact]
    public void ViewsOfOneScope_ReuseOnlyTheSameConfigurationAndProblemReferences()
    {
        var scope = ResolutionScope.Create();
        var configuration = new CountingMutator();
        var problem = new BoundProblem(new Space(), 10);
        var view = scope.For<int, Space, BoundProblem>(problem);
        var executor = view.Resolve(configuration);

        scope.For<int, Space, BoundProblem>(problem).Resolve(configuration).ShouldBeSameAs(executor);
        scope.For<int, Space, IProblem<int, Space>>(problem).Resolve(configuration).ShouldBeSameAs(executor);
        view.Resolve(configuration with { }).ShouldNotBeSameAs(executor);
        scope.For<int, Space, BoundProblem>(problem with { }).Resolve(configuration).ShouldNotBeSameAs(executor);
        view.Problem.ShouldBeSameAs(problem);
        view.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
        executor.Mutate([0], RandomNumberGenerator.Create(1)).ShouldBe([11]);
        view.Resolve(configuration).Mutate([0], RandomNumberGenerator.Create(1)).ShouldBe([12]);
    }

    [Fact]
    public void SharedConfigurations_RetainOneExecutorPerProblemAcrossChildActivations()
    {
        var root = ResolutionScope.Create();
        var configuration = new CountingMutator();
        var firstProblem = new BoundProblem(new Space(), 10);
        var secondProblem = new BoundProblem(new Space(), 20);
        root.Share(configuration);
        var first = root.CreateChildScope().For<int, Space, BoundProblem>(firstProblem).Resolve(configuration);
        var second = root.CreateChildScope().For<int, Space, BoundProblem>(secondProblem).Resolve(configuration);

        root.CreateChildScope().For<int, Space, BoundProblem>(firstProblem).Resolve(configuration).ShouldBeSameAs(first);
        root.For<int, Space, BoundProblem>(secondProblem).Resolve(configuration).ShouldBeSameAs(second);
        second.ShouldNotBeSameAs(first);
        first.Mutate([0], RandomNumberGenerator.Create(1)).ShouldBe([11]);
        second.Mutate([0], RandomNumberGenerator.Create(1)).ShouldBe([21]);
    }

    [Fact]
    public void FreshActivations_KeepBindingsPrivateWhileProblemFreeConfigurationsSpanProblemViews()
    {
        var root = ResolutionScope.Create();
        var configuration = new CountingMutator();
        var plain = new PlainConfiguration();
        var problem = new BoundProblem(new Space(), 0);
        root.Share(plain);
        var firstView = root.For<int, Space, BoundProblem>(problem);
        var secondView = root.For<int, Space, BoundProblem>(problem with { });

        secondView.Resolve(plain).ShouldBeSameAs(firstView.Resolve(plain));
        firstView.CreateChildScope().Resolve(configuration).ShouldNotBeSameAs(firstView.Resolve(configuration));
        firstView.CreateChildScope().Resolve(plain).ShouldBeSameAs(firstView.Resolve(plain));
    }

    [Fact]
    public void ShareGuard_IsConfigurationWideAcrossProblemViewsAndChildScopes()
    {
        var root = ResolutionScope.Create();
        var configuration = new CountingMutator();
        var unrelated = new CountingMutator();
        var problem = new BoundProblem(new Space(), 0);
        root.CreateChildScope().For<int, Space, BoundProblem>(problem).Resolve(configuration);

        Should.Throw<InvalidOperationException>(() => root.For<int, Space, BoundProblem>(problem with { }).Share(configuration));
        root.Share(unrelated);
        root.CreateChildScope().For<int, Space, BoundProblem>(problem).Resolve(unrelated).ShouldNotBeNull();
    }

    [Fact]
    public void EveryResolve_PreflightsItsDeclaredSignatureEvenWhenThePairAlreadyExists()
    {
        var scope = ResolutionScope.Create();
        var configuration = new ProblemSpecificMutator();
        var problem = new BoundProblem(new Space(), 0);
        var executor = scope.For<int, Space, BoundProblem>(problem).Resolve(configuration);

        scope.For<int, Space, IProblem<int, Space>>(problem).TryResolve(configuration, out var rejected, out var reason).ShouldBeFalse();
        rejected.ShouldBeNull();
        reason.ShouldContain(nameof(BoundProblem));
        scope.For<int, Space, BoundProblem>(problem).Resolve(configuration).ShouldBeSameAs(executor);
    }

    [Fact]
    public void EveryResolve_PreflightsDeclaredChildrenBeforeAnythingIsBuilt()
    {
        var probe = new ConstructionProbe();
        var configuration = new ProbedWrapper(new ProblemSpecificMutator(), probe);
        var problem = new BoundProblem(new Space(), 0);

        ResolutionScope.Create().For<int, Space, IProblem<int, Space>>(problem)
            .TryResolve(configuration, out var rejected, out var reason).ShouldBeFalse();

        rejected.ShouldBeNull();
        reason.ShouldContain(nameof(ProblemSpecificMutator));
        probe.Constructions.ShouldBe(0);
    }

    [Fact]
    public void ResultsBelongToTheRootAcrossProblemViewsAndRemainReadableAfterFailure()
    {
        var root = ResolutionScope.Create();
        var problem = new BoundProblem(new Space(), 0);
        var first = root.For<int, Space, BoundProblem>(problem);
        var second = root.CreateChildScope().For<int, Space, BoundProblem>(problem with { });
        var counter = new Counter();
        var result = first.Results.Result(counter);
        result.IncrementBy(3);
        second.Share(counter); // Reading the result did not resolve the configuration.

        second.Results.Result(counter).ShouldBeSameAs(result);
        ResolutionScope.Create().Results.Result(counter).ShouldNotBeSameAs(result);
        Should.Throw<InvalidOperationException>(() => second.Resolve(new FailingMutator()));
        first.Results.Result(counter).CurrentCount.ShouldBe(3);
        Should.Throw<InvalidOperationException>(() => first.Resolve(new CountingMutator()));
    }

    [Fact]
    public void ProblemBoundOperatorBridge_ReportsTheCompleteRunSignature()
    {
        IOperator<int> configuration = new ProblemSpecificMutator();
        var problem = new FuncProblem<int, Space>(static candidate => (double)candidate, new Space(), SingleObjective.Minimize);
        var scope = ResolutionScope.Create().For<int, Space, FuncProblem<int, Space>, PopulationState<int>>(problem);

        // Deliberately bypass preflight to exercise the construction bridge's defensive mismatch.
#pragma warning disable HLib0001
        var exception = Should.Throw<InvalidOperationException>(() => configuration.CreateExecutor(scope));
#pragma warning restore HLib0001

        exception.Message.ShouldBe("ProblemSpecificMutator is written for Space with BoundProblem, and cannot run over Space with FuncProblem<Int32, Space> producing PopulationState<Int32>.");
    }

    [Fact]
    public void StateBoundOperatorBridge_ReportsTheCompleteRunSignature()
    {
        IOperator<int> configuration = new StateBoundMutator();
        var problem = new FuncProblem<int, Space>(static candidate => (double)candidate, new Space(), SingleObjective.Minimize);
        var scope = ResolutionScope.Create().For<int, Space, FuncProblem<int, Space>, PopulationState<int>>(problem);

        // Deliberately bypass preflight to exercise the construction bridge's defensive mismatch.
#pragma warning disable HLib0001
        var exception = Should.Throw<InvalidOperationException>(() => configuration.CreateExecutor(scope));
#pragma warning restore HLib0001

        exception.Message.ShouldBe("StateBoundMutator is written for Space with BoundProblem producing ISearchState, and cannot run over Space with FuncProblem<Int32, Space> producing PopulationState<Int32>.");
    }

    [Fact]
    public void ProblemBoundAlgorithmBridge_ReportsTheCompleteRunSignature()
    {
        IAlgorithm<int, PopulationState<int>> configuration = new ProblemSpecificAlgorithm();
        var problem = new FuncProblem<int, Space>(static candidate => (double)candidate, new Space(), SingleObjective.Minimize);
        var scope = ResolutionScope.Create().For<int, Space, FuncProblem<int, Space>, PopulationState<int>>(problem);

        // Deliberately bypass preflight to exercise the construction bridge's defensive mismatch.
#pragma warning disable HLib0001
        var exception = Should.Throw<InvalidOperationException>(() => configuration.CreateExecutor(scope));
#pragma warning restore HLib0001

        exception.Message.ShouldBe("ProblemSpecificAlgorithm is written for Space with BoundProblem producing PopulationState<Int32>, and cannot run over Space with FuncProblem<Int32, Space> producing PopulationState<Int32>.");
    }

    private sealed class Space : ISearchSpace<int>
    {
        public bool Contains(int candidate) => true;
    }

    private sealed record BoundProblem(Space SearchSpace, int Offset) : IProblem<int, Space>
    {
        public ObjectiveDirections Objective => SingleObjective.Minimize;
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            candidates.Select(candidate => new ObjectiveVector(candidate + Offset)).ToArray();
    }

    private sealed record CountingMutator : Mutator<int, Space>
    {
        protected override IMutationExecutor<int> CreateExecutor<TSearchState>(ResolutionScope<int, Space, IProblem<int, Space>, TSearchState> scope) =>
            new Executor(scope.Problem);

        private sealed class Executor(IProblem<int, Space> problem) : MutationExecutor<int>
        {
            private int calls;
            public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) =>
                problem.Evaluate(parents, random).Select(value => (int)value[0] + ++calls).ToArray();
        }
    }

    private sealed record ProblemSpecificMutator : StatelessMutator<int, Space, BoundProblem>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, Space searchSpace, BoundProblem problem) => parents;
    }

    private sealed record StateBoundMutator : Operator<Mutation, int, Space, BoundProblem, ISearchState, IMutationExecutor<int>>
    {
        protected override IMutationExecutor<int> CreateExecutor(ResolutionScope<int, Space, BoundProblem, ISearchState> scope) =>
            throw new NotSupportedException("A mismatched run must not reach the typed hook.");
    }

    private sealed record ProblemSpecificAlgorithm : Algorithm<ProblemSpecificAlgorithm, int, Space, BoundProblem, PopulationState<int>>
    {
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor(ResolutionScope<int, Space, BoundProblem, PopulationState<int>> scope) =>
            throw new NotSupportedException("A mismatched run must not reach the typed hook.");
    }

    private sealed record FailingMutator : Mutator<int, Space>
    {
        protected override IMutationExecutor<int> CreateExecutor<TSearchState>(ResolutionScope<int, Space, IProblem<int, Space>, TSearchState> scope) =>
            throw new InvalidOperationException("Construction failed.");
    }

    private sealed class ConstructionProbe
    {
        public int Constructions { get; set; }
    }

    // Counts its constructions, so a test can tell whether resolution built anything.
    private sealed record ProbedWrapper(IOperator<Mutation, int> Child, ConstructionProbe Probe) : WrappingOperator<Mutation, int>(Child)
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
        {
            Probe.Constructions++;
            return scope.ResolveOperator(ChildOperator);
        }
    }

    private sealed record PlainConfiguration : IConfigurationNode<PlainExecutor>
    {
        public PlainExecutor CreateExecutionNode(ResolutionScope scope) => new();
    }

    private sealed class PlainExecutor : IExecutor;
}
