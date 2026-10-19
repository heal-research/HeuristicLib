using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ExecutionInfrastructure;

public sealed class RoleExecutorFactoryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void StatefulLeaf_ChildObservationPreservesStateAndPreparesOnce(int arity)
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
    public void StatefulLeaf_PreparationCreatesStateBeforeBindingAndEachFactoryOwnsItsState(int arity)
    {
        var preparations = 0;
        var source = CreateMutator(arity, () => preparations++);
        var factory = source.CreateExecutorFactory();
        preparations.ShouldBe(1);

        var first = Construct(factory, ResolutionScope.Create());
        var second = Construct(factory, ResolutionScope.Create());
        Next(first).ShouldBe(1);
        Next(second).ShouldBe(2);
        preparations.ShouldBe(1);

        var independentFactory = source.CreateExecutorFactory();
        Next(Construct(independentFactory, ResolutionScope.Create())).ShouldBe(1);
        preparations.ShouldBe(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void StatefulLeaf_CompatibleProblemContractsShareThePreparedState(int arity)
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
    public void BoundLeaf_IncompatibleProblemFailsBeforeStatePreparation()
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
    public void StatefulLeaf_PreparationFaultIsRetriedWithoutPublishingState()
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
    public void InputFreeTerminator_CompatibleSearchStatesSharePreparedState()
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

    [Fact]
    public void BoundInterceptor_IncompatibleSearchStateFailsBeforePreparation()
    {
        var preparations = 0;
        var source = new BoundInterceptor(() => preparations++);

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
    public void AlgorithmBridge_IncompatibleSearchStateFailsBeforePreparation(bool bound)
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
    public void AlgorithmBridge_PreparesOnceAndBindsThroughTheRequestedRunTypes(bool bound)
    {
        var preparations = 0;
        var source = CreateAlgorithm(bound, () => preparations++);
        var factory = source.CreateExecutorFactory<BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>();
        preparations.ShouldBe(1);

        factory(ResolutionScope.Create()).ShouldNotBeNull();
        factory(ResolutionScope.Create()).ShouldNotBeNull();
        preparations.ShouldBe(1);
    }

    [Fact]
    public void BoundAlgorithm_IncompatibleProblemFailsBeforePreparation()
    {
        var preparations = 0;
        var source = new BoundAlgorithm(() => preparations++);

        Should.Throw<InvalidOperationException>(() => source.CreateExecutorFactory<BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>());

        preparations.ShouldBe(0);
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

    private static IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Construct(
        OperatorExecutorFactory factory, ResolutionScope scope) =>
        (IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>)factory.CreateExecutor<Mutation, RealVector,
            BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>(scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, ISearchState>());

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
        protected override CounterState CreateInitialState()
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
        protected override CounterState CreateInitialState()
        {
            Prepare();
            return new CounterState();
        }

        protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, CounterState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
            [RealVector.Repeat(++state.Calls, 1)];
    }

    private sealed record CandidateMutator(Action Prepare) : StatefulMutator<RealVector, CounterState>
    {
        protected override CounterState CreateInitialState()
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
        protected override CounterState CreateInitialState()
        {
            Prepare();
            return new CounterState();
        }

        protected override bool IsTerminalState(CounterState state) => ++state.Calls >= 3;
    }

    private sealed record BoundInterceptor(Action Prepare)
        : StatefulInterceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>, CounterState>
    {
        protected override CounterState CreateInitialState()
        {
            Prepare();
            return new CounterState();
        }

        protected override SingleSolutionState<RealVector> Transform(SingleSolutionState<RealVector> currentState, SingleSolutionState<RealVector>? previousState, CounterState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            currentState;
    }

    private sealed record AgnosticAlgorithm(Action Prepare) : Algorithm<AgnosticAlgorithm, RealVector, SingleSolutionState<RealVector>>
    {
        public override ExecutorFactory<IAlgorithmExecutor<RealVector, TRunSearchSpace, TRunProblem, SingleSolutionState<RealVector>>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        {
            Prepare();
            return _ => new ProbeAlgorithmExecutor<TRunSearchSpace, TRunProblem>();
        }
    }

    private sealed record BoundAlgorithm(Action Prepare)
        : Algorithm<BoundAlgorithm, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public override ExecutorFactory<AlgorithmExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>> CreateExecutorFactory()
        {
            Prepare();
            return _ => new ProbeAlgorithmExecutor<BoundedRealVectorSearchSpace, TestFunctionProblem>();
        }
    }

    private sealed class ProbeAlgorithmExecutor<TSearchSpace, TProblem> : AlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>
        where TSearchSpace : class, ISearchSpace<RealVector>
        where TProblem : class, IProblem<RealVector, TSearchSpace>
    {
        public override async IAsyncEnumerable<SingleSolutionState<RealVector>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, SingleSolutionState<RealVector>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            if (initialState is not null)
                yield return initialState;
        }
    }
}
