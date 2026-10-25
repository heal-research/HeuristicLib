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
        var parent = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), new ObservingMutatorAdvice(outerReports.Add)));
        var outer = parent.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(source);
        Next(outer).ShouldBe(1);
        Next(outer).ShouldBe(2);
        Next(outer).ShouldBe(3);

        var child = parent.CreateChildScope(Aspect.Create(Pointcut.Reference(source), new ObservingMutatorAdvice(innerReports.Add)));
        var inner = child.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(source);
        Next(inner).ShouldBe(4);
        Next(inner).ShouldBe(5);

        preparations.ShouldBe(1);
        outerReports.ShouldBe([1, 2, 3, 4, 5]);
        innerReports.ShouldBe([4, 5]);
        Next(outer).ShouldBe(6);
        innerReports.ShouldBe([4, 5]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void StatefulLeaf_ChildFirstStateStaysIndependentOfLaterAncestorState(int arity)
    {
        var preparations = 0;
        var source = CreateMutator(arity, () => preparations++);
        var parent = ResolutionScope.Create();
        var child = parent.CreateChildScope();
        var local = child.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source);
        Next(local).ShouldBe(1);
        var ancestor = parent.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source);
        Next(ancestor).ShouldBe(1);

        Next(child.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source)).ShouldBe(2);
        Next(parent.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source)).ShouldBe(2);
        preparations.ShouldBe(2);
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
        var first = root.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source);
        preparations.ShouldBe(1);

        var second = root.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source);
        first.ShouldNotBeSameAs(second);
        Next(first).ShouldBe(1);
        Next(second).ShouldBe(2);
        preparations.ShouldBe(1);

        Next(ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source)).ShouldBe(1);
        preparations.ShouldBe(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void StatefulLeaf_CompatibleProblemContractsShareTheScopedState(int arity)
    {
        var preparations = 0;
        var source = CreateMutator(arity, () => preparations++);
        var scope = ResolutionScope.Create();
        var wide = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(source);
        var narrow = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source);
        var problem = new TestFunctionProblem(new RastriginFunction(1));

        wide.Mutate([RealVector.Repeat(0, 1)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem)[0][0].ShouldBe(1);
        Next(narrow).ShouldBe(2);
        preparations.ShouldBe(1);
    }

    [Fact]
    public void BoundLeaf_IncompatibleProblemFailsBeforeCreatingState()
    {
        var preparations = 0;
        var source = new BoundMutator(() => preparations++);
        var scope = ResolutionScope.Create();

        scope.TryResolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(
            source, out var executor, out var reason).ShouldBeFalse();

        executor.ShouldBeNull();
        reason.ShouldNotBeNull();
        reason.ShouldContain(nameof(TestFunctionProblem));
        preparations.ShouldBe(0);
    }

    [Fact]
    public void StatefulLeaf_StateCreationFaultIsRetriedWithoutPublishingState()
    {
        var preparations = 0;
        var failure = new InvalidOperationException("initialization failed");
        var source = new CandidateMutator(() =>
        {
            preparations++;
            throw failure;
        });
        var parent = ResolutionScope.Create();

        Should.Throw<InvalidOperationException>(() => parent.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source)).ShouldBeSameAs(failure);
        Should.Throw<InvalidOperationException>(() => parent.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(source)).ShouldBeSameAs(failure);
        preparations.ShouldBe(2);
    }

    [Fact]
    public void InputFreeTerminator_CompatibleSearchStatesShareScopedState()
    {
        var preparations = 0;
        var source = new InputFreeTerminator(() => preparations++);
        var scope = ResolutionScope.Create();
        var population = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(source);
        var single = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source);
        var problem = new TestFunctionProblem(new RastriginFunction(1));
        var searchState = SingleSolutionState.From(RealVector.Repeat(0, 1), new ObjectiveVector(0));

        population.IsTerminalState(searchState, RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        single.IsTerminalState(searchState, RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        population.IsTerminalState(searchState, RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();
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
            source, out var executor, out var reason).ShouldBeFalse();

        executor.ShouldBeNull();
        reason.ShouldNotBeNull();
        reason.ShouldContain(nameof(SingleSolutionState<RealVector>));
        preparations.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlgorithmBridge_CreatesStateOncePerOwningStateAndBindsThroughTheRequestedRunTypes(bool bound)
    {
        var preparations = 0;
        var source = CreateAlgorithm(bound, () => preparations++);
        var scope = ResolutionScope.Create();

        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source).ShouldNotBeNull();
        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source).ShouldNotBeNull();
        preparations.ShouldBe(1);

        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source).ShouldNotBeNull();
        preparations.ShouldBe(2);
    }

    [Fact]
    public void BoundAlgorithm_IncompatibleProblemFailsBeforeCreatingState()
    {
        var preparations = 0;
        var source = new BoundAlgorithm(() => preparations++);

        ResolutionScope.Create().TryResolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>, SingleSolutionState<RealVector>>(
            source, out var executor, out var reason).ShouldBeFalse();

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
            source, out var executor, out var reason).ShouldBeFalse();

        executor.ShouldBeNull();
        reason.ShouldNotBeNull();
        reason.ShouldContain($"{nameof(HillClimber<>.Creator)}: {nameof(RandomBoolVectorCreator)} was not written for an execution over {nameof(FixedCardinalityBoolVectorSearchSpace)}");
        reason.ShouldContain($"{nameof(HillClimber<>.Mutator)}: {nameof(BitFlipMutator)} was not written for an execution over {nameof(FixedCardinalityBoolVectorSearchSpace)}");
        reason.Split(Environment.NewLine).Count(line => line.StartsWith("  - ", StringComparison.Ordinal)).ShouldBe(2);
    }

    [Fact]
    public void AlgorithmAdvice_ForAnotherSearchStateOfTheSelectedAlgorithm_IsSkipped()
    {
        var source = new TwoStateAlgorithm();
        var advice = new CountingSingleSolutionAdvice();
        var scope = ResolutionScope.Create(Aspect.Create(Pointcut.Reference(source), advice));

        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(source).ShouldNotBeNull();
        advice.Wraps.ShouldBe(0);
        scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(source).ShouldNotBeNull();
        advice.Wraps.ShouldBe(1);
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

    private static double Next(IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> executor)
    {
        var problem = new TestFunctionProblem(new RastriginFunction(1));
        return executor.Mutate([RealVector.Repeat(0, 1)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem)[0][0];
    }

    private sealed class CounterState
    {
        public int Calls;
    }

    private sealed record BoundMutator(Action Prepare)
        : StatefulMutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterState>
    {
        protected override CounterState CreateScopedState()
        {
            Prepare();
            return new CounterState();
        }

        protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, CounterState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            [RealVector.Repeat(++state.Calls, 1)];
    }

    private sealed record SearchSpaceMutator(Action Prepare)
        : StatefulMutator<RealVector, BoundedRealVectorSearchSpace, CounterState>
    {
        protected override CounterState CreateScopedState()
        {
            Prepare();
            return new CounterState();
        }

        protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, CounterState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
            [RealVector.Repeat(++state.Calls, 1)];
    }

    private sealed record CandidateMutator(Action Prepare) : StatefulMutator<RealVector, CounterState>
    {
        protected override CounterState CreateScopedState()
        {
            Prepare();
            return new CounterState();
        }

        protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, CounterState state, IRandomNumberGenerator random) =>
            [RealVector.Repeat(++state.Calls, 1)];
    }

    private sealed class ObservingMutatorAdvice(Action<double> report) : MutatorAdvice<RealVector>
    {
        protected override IMutationExecutor<RealVector, TRunSearchSpace, TRunProblem> Wrap<TRunSearchSpace, TRunProblem>(
            IMutationExecutor<RealVector, TRunSearchSpace, TRunProblem> executor, IOperator<Mutation, RealVector> mutator) =>
            new Executor<TRunSearchSpace, TRunProblem>(executor, report);

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

    private sealed record InputFreeTerminator(Action Prepare) : StatefulTerminator<RealVector, CounterState>
    {
        protected override CounterState CreateScopedState()
        {
            Prepare();
            return new CounterState();
        }

        protected override bool IsTerminalState(CounterState state) => ++state.Calls >= 3;
    }

    private sealed record AgnosticAlgorithm(Action Prepare) : Algorithm<AgnosticAlgorithm, RealVector, SingleSolutionState<RealVector>>
    {
        protected override IAlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
        {
            scope.GetScopedState(this, () =>
            {
                Prepare();
                return new CounterState();
            });
            return new ProbeAlgorithmExecutor<TSearchSpace, TProblem, SingleSolutionState<RealVector>>();
        }
    }

    private sealed record BoundAlgorithm(Action Prepare)
        : Algorithm<BoundAlgorithm, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        protected override IAlgorithmExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> CreateExecutor(
            ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> scope)
        {
            scope.GetScopedState(this, () =>
            {
                Prepare();
                return new CounterState();
            });
            return new ProbeAlgorithmExecutor<BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>();
        }
    }

    private sealed record TwoStateAlgorithm
        : Algorithm<TwoStateAlgorithm, RealVector, SingleSolutionState<RealVector>>, IAlgorithm<RealVector, PopulationState<RealVector>>
    {
        // Resolution checks the fit before it builds, so a configuration producing two states declares both.
        public override bool Fits(ExecutionSignature execution) =>
            base.Fits(execution) || execution.SearchState == typeof(PopulationState<RealVector>);

        protected override IAlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope) =>
            new ProbeAlgorithmExecutor<TSearchSpace, TProblem, SingleSolutionState<RealVector>>();

        IAlgorithmExecutor<RealVector, TSearchSpace, TProblem, PopulationState<RealVector>> IAlgorithm<RealVector, PopulationState<RealVector>>.CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, PopulationState<RealVector>> scope) =>
            new ProbeAlgorithmExecutor<TSearchSpace, TProblem, PopulationState<RealVector>>();
    }

    private sealed class CountingSingleSolutionAdvice : AlgorithmAdvice<RealVector, SingleSolutionState<RealVector>>
    {
        public int Wraps { get; private set; }

        public override IAlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> Wrap<TSearchSpace, TProblem>(
            IAlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> executor, IAlgorithm<RealVector, SingleSolutionState<RealVector>> source)
        {
            Wraps++;
            return executor;
        }
    }

    private sealed class ProbeAlgorithmExecutor<TSearchSpace, TProblem, TSearchState> : AlgorithmExecutor<RealVector, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            if (initialState is not null)
                yield return initialState;
        }
    }
}
