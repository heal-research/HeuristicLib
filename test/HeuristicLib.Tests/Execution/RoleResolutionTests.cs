using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public sealed class RoleResolutionTests
{
    private static readonly TestFunctionProblem Problem = new(new RastriginFunction(1));
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void StatefulLeaf_ChildObservationKeepsStateAndCreatesItOnce(int arity)
    {
        var preparations = 0;
        var source = CreateMutator(arity, () => preparations++);
        var outerReports = new List<double>();
        var innerReports = new List<double>();
        var observed = new ReportingMutator(source, outerReports.Add);
        var parent = ResolutionScope.Create();
        var outer = parent.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Problem).Resolve(observed);
        Next(outer).ShouldBe(1);
        Next(outer).ShouldBe(2);
        Next(outer).ShouldBe(3);

        var child = parent.CreateChildScope();
        var inner = child.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Problem)
            .Resolve(observed.Substitute(source, mutator => new ReportingMutator(mutator, innerReports.Add)));
        Next(inner).ShouldBe(1);
        Next(inner).ShouldBe(2);

        preparations.ShouldBe(2);
        outerReports.ShouldBe([1, 2, 3, 1, 2]);
        innerReports.ShouldBe([1, 2]);
        Next(outer).ShouldBe(4);
        innerReports.ShouldBe([1, 2]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void StatefulLeaf_EachScopeBuildsItsOwnPrivateFields(int arity)
    {
        var preparations = 0;
        var source = CreateMutator(arity, () => preparations++);
        var parent = ResolutionScope.Create();
        var child = parent.CreateChildScope();
        var local = child.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem);
        Next(local).ShouldBe(1);
        var ancestor = parent.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem);
        Next(ancestor).ShouldBe(1);

        Next(child.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem)).ShouldBe(2);
        Next(parent.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem)).ShouldBe(1);
        preparations.ShouldBe(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void StatefulLeaf_CreatesStateOnFirstBuildAndEachOwningStateKeepsItsOwn(int arity)
    {
        var preparations = 0;
        var source = CreateMutator(arity, () => preparations++);
        var root = ResolutionScope.Create();
        var first = root.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem);
        preparations.ShouldBe(1);

        var second = root.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem);
        first.ShouldNotBeSameAs(second);
        Next(first).ShouldBe(1);
        Next(second).ShouldBe(1);
        preparations.ShouldBe(2);

        Next(ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem)).ShouldBe(1);
        preparations.ShouldBe(3);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void StatefulLeaf_CompatibleProblemContractsShareTheExecutorOfTheirScope(int arity)
    {
        var preparations = 0;
        var source = CreateMutator(arity, () => preparations++);
        var scope = ResolutionScope.Create();
        var wide = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(source, Problem);
        var narrow = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem);

        wide.Mutate([RealVector.Repeat(0, 1)], RandomNumberGenerator.Create(1))[0][0].ShouldBe(1);
        Next(narrow).ShouldBe(2); // the executor built for the wider problem contract serves the narrower one too
        preparations.ShouldBe(1);
    }

    [Fact]
    public void BoundLeaf_IncompatibleProblemFailsBeforeCreatingState()
    {
        var preparations = 0;
        var source = new BoundMutator(() => preparations++);
        var scope = ResolutionScope.Create();

        scope.TryResolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(
            source, Problem, out var executor, out var reason).ShouldBeFalse();

        executor.ShouldBeNull();
        reason.ShouldNotBeNull();
        reason.ShouldContain(nameof(TestFunctionProblem));
        preparations.ShouldBe(0);
    }

    [Fact]
    public void StatefulLeaf_StateCreationFaultEndsTheScopeTreeAndIsRetriedInANewScope()
    {
        var preparations = 0;
        var failure = new InvalidOperationException("initialization failed");
        var source = new CandidateMutator(() =>
        {
            preparations++;
            throw failure;
        });
        var parent = ResolutionScope.Create();

        Should.Throw<InvalidOperationException>(() => parent.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem)).ShouldBeSameAs(failure);
        Should.Throw<InvalidOperationException>(() => parent.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem))
            .Message.ShouldContain("new scope");
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source, Problem)).ShouldBeSameAs(failure);
        preparations.ShouldBe(2);
    }

    [Fact]
    public void InputFreeTerminator_CompatibleSearchStatesShareTheCounterOfTheirScope()
    {
        var preparations = 0;
        var source = new InputFreeTerminator(() => preparations++);
        var scope = ResolutionScope.Create();
        var population = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(source, Problem);
        var single = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source, Problem);
        var searchState = SingleSolutionState.From(RealVector.Repeat(0, 1), new ObjectiveVector(0));

        population.IsTerminalState(searchState, RandomNumberGenerator.Create(0)).ShouldBeFalse();
        single.IsTerminalState(searchState, RandomNumberGenerator.Create(0)).ShouldBeFalse();
        population.IsTerminalState(searchState, RandomNumberGenerator.Create(0)).ShouldBeTrue();
        preparations.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlgorithmBridge_IncompatibleSearchStateFailsBeforeCreatingState(bool bound)
    {
        var preparations = 0;
        var source = CreateAlgorithm(bound, () => preparations++);

        ResolutionScope.Create().TryResolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(
            source, Problem, out var executor, out var reason).ShouldBeFalse();

        executor.ShouldBeNull();
        reason.ShouldNotBeNull();
        reason.ShouldContain(nameof(SingleSolutionState<RealVector>));
        preparations.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlgorithmBridge_BuildsOncePerScopeThroughTheRequestedRunTypes(bool bound)
    {
        var preparations = 0;
        var source = CreateAlgorithm(bound, () => preparations++);
        var scope = ResolutionScope.Create();

        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source, Problem).ShouldNotBeNull();
        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source, Problem).ShouldNotBeNull();
        preparations.ShouldBe(1);

        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source, Problem).ShouldNotBeNull();
        preparations.ShouldBe(2);
    }

    [Fact]
    public void BoundAlgorithm_IncompatibleProblemFailsBeforeCreatingState()
    {
        var preparations = 0;
        var source = new BoundAlgorithm(() => preparations++);

        ResolutionScope.Create().TryResolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>, SingleSolutionState<RealVector>>(
            source, Problem, out var executor, out var reason).ShouldBeFalse();

        executor.ShouldBeNull();
        reason.ShouldNotBeNull();
        reason.ShouldContain($"{nameof(BoundAlgorithm)} was not written for an execution over");
        preparations.ShouldBe(0);
    }

    /// <summary>
    /// The algorithm fits only if its operators do, so the failure names each operator that does not, rather than the
    /// algorithm that only holds them.
    /// </summary>
    [Fact]
    public void AlgorithmMismatch_NamesEveryOperatorThatDoesNotFit()
    {
        var source = new HillClimber<BoolVector>
        {
            Creator = new RandomBoolVectorCreator(),
            Mutator = new BitFlipMutator()
        };

        ResolutionScope.Create().TryResolve<BoolVector, FixedCardinalityBoolVectorSearchSpace, IProblem<BoolVector, FixedCardinalityBoolVectorSearchSpace>, SingleSolutionState<BoolVector>>(
            source, FuncProblem.Create(static (BoolVector _) => 0.0, new FixedCardinalityBoolVectorSearchSpace(3, 1), SingleObjective.Minimize), out var executor, out var reason).ShouldBeFalse();

        executor.ShouldBeNull();
        reason.ShouldNotBeNull();
        reason.ShouldContain($"{nameof(HillClimber<>.Creator)}: {nameof(RandomBoolVectorCreator)} was not written for an execution over {nameof(FixedCardinalityBoolVectorSearchSpace)}");
        reason.ShouldContain($"{nameof(HillClimber<>.Mutator)}: {nameof(BitFlipMutator)} was not written for an execution over {nameof(FixedCardinalityBoolVectorSearchSpace)}");
        reason.Split(Environment.NewLine).Count(line => line.StartsWith("  - ", StringComparison.Ordinal)).ShouldBe(2);
    }

    [Fact]
    public void ATwoStateAlgorithm_IsBuiltForOneOfItsStatesInAScope()
    {
        var source = new TwoStateAlgorithm();
        var scope = ResolutionScope.Create();

        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(source, Problem).ShouldNotBeNull();
        scope.TryResolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source, Problem, out _, out var reason).ShouldBeFalse();
        reason.ShouldContain("already built in this resolution scope");
        scope.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source, Problem).ShouldNotBeNull();
    }

    private static IOperator<Mutation, RealVector> CreateMutator(int arity, Action prepare) => arity switch
    {
        0 => new BoundMutator(prepare),
        1 => new SearchSpaceMutator(prepare),
        2 => new CandidateMutator(prepare),
        _ => throw new ArgumentOutOfRangeException(nameof(arity)),
    };

    private static IAlgorithm<RealVector> CreateAlgorithm(bool bound, Action prepare) =>
        bound ? new BoundAlgorithm(prepare) : new AgnosticAlgorithm(prepare);

    private static double Next(IMutationExecutor<RealVector> executor)
    {
        return executor.Mutate([RealVector.Repeat(0, 1)], RandomNumberGenerator.Create(1))[0][0];
    }

    private sealed record BoundMutator(Action Prepare)
        : Mutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {

        protected override IMutationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope)
        {
            Prepare();
            return new Executor(scope.Problem);
        }

        private sealed class Executor
            : MutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            [RealVector.Repeat(++invocations, 1)];
        }
    }

    private sealed record SearchSpaceMutator(Action Prepare)
        : Mutator<RealVector, BoundedRealVectorSearchSpace>
    {

        protected override IMutationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>, TRunSearchState> scope)
        {
            Prepare();
            return new Executor(searchSpace: scope.SearchSpace);
        }

        private sealed class Executor
            : MutationExecutor<RealVector, BoundedRealVectorSearchSpace>
        {
            public Executor(BoundedRealVectorSearchSpace searchSpace) : base(searchSpace: searchSpace) { }

            private int invocations;

            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
            [RealVector.Repeat(++invocations, 1)];
        }
    }

    private sealed record CandidateMutator(Action Prepare) : Mutator<RealVector>
    {

        protected override IMutationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, TRunSearchState> scope)
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

    // Reports the first coordinate of each successful call's first offspring.
    private sealed record ReportingMutator : WrappingOperator<Mutation, RealVector>
    {
        public ReportingMutator(IOperator<Mutation, RealVector> childMutator, Action<double> report)
            : base(childMutator)
        {
            Report = report;
        }

        public Action<double> Report { get; init; }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
            new Executor(scope.Resolve(ChildOperator), Report);

        private sealed class Executor(IMutationExecutor<RealVector> child, Action<double> report)
            : IMutationExecutor<RealVector>
        {
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random)
            {
                var result = child.Mutate(parents, random);
                report(result[0][0]);
                return result;
            }
        }
    }

    private sealed record InputFreeTerminator(Action Prepare) : Terminator<RealVector>
    {

        protected override ITerminationExecutor<ISearchState> CreateExecutor(ResolutionScope<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, ISearchState> scope)
        {
            Prepare();
            return new Executor();
        }

        private sealed class Executor
            : TerminationExecutor
        {
            private int invocations;

            public override bool IsTerminalState() => ++invocations >= 3;
        }
    }

    private sealed record AgnosticAlgorithm(Action Prepare) : Algorithm<AgnosticAlgorithm, RealVector, SingleSolutionState<RealVector>>
    {
        protected override IAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
        {
            Prepare();
            return new ProbeAlgorithmExecutor<SingleSolutionState<RealVector>>();
        }

    }

    private sealed record BoundAlgorithm(Action Prepare)
        : Algorithm<BoundAlgorithm, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        protected override IAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor(
            ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> scope)
        {
            Prepare();
            return new ProbeAlgorithmExecutor<SingleSolutionState<RealVector>>();
        }

    }

    private sealed record TwoStateAlgorithm
        : Algorithm<TwoStateAlgorithm, RealVector, SingleSolutionState<RealVector>>, IAlgorithm<RealVector, PopulationState<RealVector>>
    {
        // Resolution checks the fit before it builds, so a configuration producing two states declares both.
        public override bool Supports(ExecutionSignature execution) =>
            base.Supports(execution) || execution.SearchState == typeof(PopulationState<RealVector>);

        protected override IAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope) =>
            new ProbeAlgorithmExecutor<SingleSolutionState<RealVector>>();

        IAlgorithmExecutor<PopulationState<RealVector>> IAlgorithm<RealVector, PopulationState<RealVector>>.CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, PopulationState<RealVector>> scope) =>
            new ProbeAlgorithmExecutor<PopulationState<RealVector>>();
    }

    private sealed class ProbeAlgorithmExecutor<TSearchState> : AlgorithmExecutor<TSearchState>
        where TSearchState : class, ISearchState
    {

        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            if (initialState is not null)
                yield return initialState;
        }
    }
}
