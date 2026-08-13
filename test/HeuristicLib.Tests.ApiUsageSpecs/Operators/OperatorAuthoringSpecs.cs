using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Operators;

public class OperatorAuthoringSpecs
{
    [Fact]
    public void StatelessMutator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new PullTowardZeroMutator();
        var instance = ResolveMutator(mutator);

        var offspring = instance.Mutate(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(12),
            problem.SearchSpace,
            problem);

        offspring.ShouldBe([RealVector.Repeat(0.5, 3)]);
    }

    [Fact]
    public void SingleCandidateMutator_CanMutateOneCandidateDirectly()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        SingleCandidateMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem> mutator = new PullTowardZeroMutator();

        var offspring = mutator.MutateCandidate(
            RealVector.Repeat(1.0, 3),
            RandomNumberGenerator.Create(12),
            problem.SearchSpace,
            problem);

        offspring.ShouldBe(RealVector.Repeat(0.5, 3));
    }

    [Fact]
    public void StatefulMutator_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new CountingStatefulMutator();
        var firstInstance = ResolveMutator(mutator);
        var secondInstance = ResolveMutator(mutator);
        var parent = RealVector.Repeat(0.0, 3);

        var first = firstInstance.Mutate([parent], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Mutate([parent], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Mutate([parent], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitMutator_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new ApplyTwiceMutator(new PullTowardZeroMutator());
        var instance = ResolveMutator(mutator);

        var offspring = instance.Mutate(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(34),
            problem.SearchSpace,
            problem);

        offspring.ShouldBe([RealVector.Repeat(0.25, 3)]);
    }

    [Fact]
    public void TopologyMutators_AcceptProblemSpecificChildren()
    {
        // Wrapping and multi bases stay at the full arity so their child slot can hold a problem-specific mutator.
        // A reduced-arity topology base would fix the child to the widest role contract and reject this.
        var problem = CreateRastriginProblem(dimension: 3);
        var pipeline = PipelineMutator.Create<RealVector, RealVectorSearchSpace, TestFunctionProblem>(
            new PullTowardZeroMutator(),
            NoChangeMutator<RealVector>.Instance);

        var offspring = ResolveMutator(pipeline).Mutate(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(34),
            problem.SearchSpace,
            problem);

        offspring.ShouldBe([RealVector.Repeat(0.5, 3)]);
    }

    [Fact]
    public void MutatorCompositionFactories_InferRoleTypes()
    {
        IMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem> childMutator = new PullTowardZeroMutator();
        IMutatorObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem> observer =
            new ActionMutatorObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem>((_, _, _, _) => { });

        var observable = ObservableMutator.Create(childMutator, observer);
        var callbackObservable = ObservableMutator.Create(childMutator, _ => { });
        var counting = CountingMutator.Create(childMutator, new ObservationCounter(), OperatorCountMetric.Calls);

        observable.ChildMutator.ShouldBeSameAs(childMutator);
        observable.Observers.ShouldBe([observer]);
        callbackObservable.Observers.Count.ShouldBe(1);
        counting.ChildMutator.ShouldBeSameAs(childMutator);
    }

    [Fact]
    public async Task WrappingCreator_AuthoringExample_RunsInsideHillClimber()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new PrefixingWrappingCreator(new ConstantOriginCreator()),
            Mutator = new NoChangeMutator(),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 3,
            MaxNeighbors = 3
        };

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(123),
          ct: TestContext.Current.CancellationToken);

        finalState.EvaluatedCandidate.Candidate.ShouldBe(RealVector.Repeat(0.0, problem.TestFunction.Dimension));
    }

    [Fact]
    public void StatefulCreator_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var creator = new CountingStatefulCreator();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(creator);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(creator);

        var first = firstInstance.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Create(1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void StatefulCrossover_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var crossover = new CountingStatefulCrossover();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(crossover);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(crossover);
        var parents = Parents.From(RealVector.Repeat(0.0, 3), RealVector.Repeat(10.0, 3));

        var first = firstInstance.Cross([parents], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Cross([parents], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Cross([parents], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitCrossover_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var crossover = new ForwardingCrossover(SelectFirstParentCrossover.For(problem));
        var instance = new ExecutionInstanceRegistry().Resolve(crossover);
        var parents = Parents.From(RealVector.Repeat(1.0, 3), RealVector.Repeat(2.0, 3));

        var offspring = instance.Cross([parents], RandomNumberGenerator.Create(4), problem.SearchSpace, problem);

        offspring.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void StatelessEvaluator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new FirstValueEvaluator();
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);

        var objectives = instance.Evaluate([RealVector.Repeat(2.0, 3)], RandomNumberGenerator.Create(5), problem.SearchSpace, problem);

        objectives.ShouldBe([new ObjectiveVector(2.0)]);
    }

    [Fact]
    public void SingleSolutionEvaluator_UsesGeneralExecutionConcurrency()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new ConcurrentFirstValueEvaluator { Concurrency = ExecutionConcurrency.Concurrent(2) };
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);

        var objectives = instance.Evaluate(
            [RealVector.Repeat(2.0, 3), RealVector.Repeat(4.0, 3)],
            RandomNumberGenerator.Create(5),
            problem.SearchSpace,
            problem);

        objectives.ShouldBe([new ObjectiveVector(2.0), new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void StatefulEvaluator_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new CountingStatefulEvaluator();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(evaluator);
        var candidates = new[] { RealVector.Repeat(0.0, 3) };

        var first = firstInstance.Evaluate(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Evaluate(candidates, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Evaluate(candidates, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([new ObjectiveVector(1.0)]);
        second.ShouldBe([new ObjectiveVector(2.0)]);
        independent.ShouldBe([new ObjectiveVector(1.0)]);
    }

    [Fact]
    public void ExplicitEvaluator_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new ForwardingEvaluator(new FirstValueEvaluator());
        var instance = new ExecutionInstanceRegistry().Resolve(evaluator);

        var objectives = instance.Evaluate([RealVector.Repeat(4.0, 3)], RandomNumberGenerator.Create(6), problem.SearchSpace, problem);

        objectives.ShouldBe([new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void StatelessSelector_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new FirstSelector();
        var instance = new ExecutionInstanceRegistry().Resolve(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = instance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(7), problem.SearchSpace, problem);

        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void StatefulSelector_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new RotatingStatefulSelector();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(selector);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(selector);
        var population = CreatePopulation(1.0, 2.0);

        var first = firstInstance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([population[0]]);
        second.ShouldBe([population[1]]);
        independent.ShouldBe([population[0]]);
    }

    [Fact]
    public void ExplicitSelector_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new ForwardingSelector(new FirstSelector());
        var instance = new ExecutionInstanceRegistry().Resolve(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = instance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(8), problem.SearchSpace, problem);

        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void WrappingSelector_AuthoringExample_ResolvesItsChildOnce()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var childSelector = new FirstSelector();
        var selector = new DoublingWrappingSelector(childSelector);
        var instance = new ExecutionInstanceRegistry().Resolve(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = instance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(11), problem.SearchSpace, problem);

        selector.ChildSelector.ShouldBeSameAs(childSelector);
        selected.ShouldBe([population[0], population[0]]);
    }

    [Fact]
    public void MultiSelector_AuthoringExample_ResolvesEveryChildOnce()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var first = new FirstSelector();
        var last = new LastSelector();
        var selector = new PreferFirstMultiSelector([first, last]);
        var instance = new ExecutionInstanceRegistry().Resolve(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = instance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(12), problem.SearchSpace, problem);

        selector.ChildSelectors.ShouldBe([first, last]);
        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void SelectorCompositionFactories_InferRoleTypes()
    {
        ISelector<RealVector, RealVectorSearchSpace, TestFunctionProblem> childSelector = new FirstSelector();
        ISelectorObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem> observer =
            new ActionSelectorObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem>((_, _, _, _, _, _) => { });

        var observable = childSelector.ObserveWith(observer);
        var staticObservable = ObservableSelector.Create(childSelector, observer);
        var callbackObservable = ObservableSelector.Create(childSelector, _ => { });
        var counting = childSelector.CountSelectorCalls(new ObservationCounter());
        var chooseOne = ChooseOneSelector.Create(childSelector, new LastSelector());

        observable.ChildSelector.ShouldBeSameAs(childSelector);
        observable.Observers.ShouldBe([observer]);
        staticObservable.ChildSelector.ShouldBeSameAs(childSelector);
        staticObservable.Observers.ShouldBe([observer]);
        callbackObservable.Observers.Count.ShouldBe(1);
        counting.ChildSelector.ShouldBeSameAs(childSelector);
        chooseOne.ChildSelectors[0].ShouldBeSameAs(childSelector);
    }

    [Fact]
    public void StatelessReplacer_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new OffspringReplacer();
        var instance = new ExecutionInstanceRegistry().Resolve(replacer);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var replaced = instance.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(9), problem.SearchSpace, problem);

        replaced.ShouldBe(offspring);
    }

    [Fact]
    public void StatefulReplacer_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new AlternatingStatefulReplacer();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(replacer);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(replacer);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var first = firstInstance.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe(previous);
        second.ShouldBe(offspring);
        independent.ShouldBe(previous);
    }

    [Fact]
    public void ExplicitReplacer_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new ForwardingReplacer(new OffspringReplacer());
        var instance = new ExecutionInstanceRegistry().Resolve(replacer);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var replaced = instance.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(10), problem.SearchSpace, problem);

        replaced.ShouldBe(offspring);
    }

    [Fact]
    public void StatelessInterceptor_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(new IncrementingInterceptor());

        var transformed = instance.Transform(new CounterSearchState(1), previousState: null, problem.SearchSpace, problem);

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void StatefulInterceptor_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var interceptor = new CountingStatefulInterceptor();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(interceptor);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(interceptor);

        var first = firstInstance.Transform(new CounterSearchState(0), previousState: null, problem.SearchSpace, problem);
        var second = firstInstance.Transform(new CounterSearchState(0), previousState: null, problem.SearchSpace, problem);
        var independent = secondInstance.Transform(new CounterSearchState(0), previousState: null, problem.SearchSpace, problem);

        first.Value.ShouldBe(1);
        second.Value.ShouldBe(2);
        independent.Value.ShouldBe(1);
    }

    [Fact]
    public void ExplicitInterceptor_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(new ForwardingInterceptor(new IncrementingInterceptor()));

        var transformed = instance.Transform(new CounterSearchState(1), previousState: null, problem.SearchSpace, problem);

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void StatelessTerminator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(new ValueTerminator(2));

        instance.IsTerminalState(new CounterSearchState(1), problem.SearchSpace, problem).ShouldBeFalse();
        instance.IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void StatefulTerminator_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var terminator = new CountingStatefulTerminator();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(terminator);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(terminator);
        var state = new CounterSearchState(0);

        firstInstance.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeFalse();
        firstInstance.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeTrue();
        secondInstance.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeFalse();
    }

    [Fact]
    public void ExplicitTerminator_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(new ForwardingTerminator(new ValueTerminator(2)));

        instance.IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public async Task MultiMutator_AuthoringExample_CanBeUsedInsideHillClimber()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new ConstantOneCreator(),
            Mutator = new PreferFirstMultiMutator(
                [
                    new PullTowardZeroMutator(),
                    new PushAwayFromZeroMutator()
                ]),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 2,
            MaxNeighbors = 2
        }.WithMaxIterations(1);

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(456),
          ct: TestContext.Current.CancellationToken);

        problem.SearchSpace.Contains(finalState.EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    private static TestFunctionProblem CreateRastriginProblem(int dimension)
    {
        return new TestFunctionProblem(new RastriginFunction(dimension));
    }

    private static IReadOnlyList<EvaluatedCandidate<RealVector>> CreatePopulation(params double[] values) =>
        values.Select(value => EvaluatedCandidate.From(RealVector.Repeat(value, 3), new ObjectiveVector(value))).ToArray();

    private static IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> ResolveMutator(IMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem> mutator)
    {
        return new ExecutionInstanceRegistry().Resolve(mutator);
    }

    private sealed record PrefixingWrappingCreator(ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> InnerCreator)
      : WrappingCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>(InnerCreator)
    {
        protected override WrappingCreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateCreatorInstance(ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> innerCreator) =>
            new Instance(innerCreator);

        private sealed class Instance(ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> innerCreator)
            : WrappingCreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(innerCreator)
        {
            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var offspring = InnerCreator.Create(count, random, searchSpace, problem).ToArray();
                offspring[0] = RealVector.Repeat(0.0, problem.TestFunction.Dimension);
                return offspring;
            }
        }
    }

    private sealed record ConstantOriginCreator
      : SingleSolutionCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector Create(
          IRandomNumberGenerator random,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return RealVector.Repeat(1.0, problem.TestFunction.Dimension);
        }
    }

    private sealed record CountingStatefulCreator : StatefulCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingStatefulCreator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<RealVector> Create(int count, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return Enumerable.Repeat(RealVector.Repeat(state.Calls, problem.TestFunction.Dimension), count).ToArray();
        }
    }

    private sealed record CountingStatefulCrossover : StatefulCrossover<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingStatefulCrossover.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return parents.Select(parent => new RealVector(parent.Parent1.Select(value => value + state.Calls))).ToArray();
        }
    }

    private sealed record FirstValueEvaluator : StatelessEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            candidates.Select(candidate => new ObjectiveVector(candidate[0])).ToArray();
    }

    private sealed record ConcurrentFirstValueEvaluator : SingleSolutionEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override ObjectiveVector Evaluate(RealVector candidate, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            new(candidate[0]);
    }

    private sealed record CountingStatefulEvaluator : StatefulEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingStatefulEvaluator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return candidates.Select(_ => new ObjectiveVector(state.Calls)).ToArray();
        }
    }

    private sealed record ForwardingEvaluator(IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
        : Evaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        protected override EvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateEvaluatorInstance(ExecutionInstanceRegistry registry) =>
            new Instance(registry.Resolve(Inner));

        private sealed class Instance(IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : EvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Evaluate(candidates, random, searchSpace, problem);
        }
    }

    private sealed record FirstSelector : StatelessSelector<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            population.Take(count).ToArray();
    }

    private sealed record RotatingStatefulSelector : StatefulSelector<RealVector, RealVectorSearchSpace, TestFunctionProblem, RotatingStatefulSelector.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            var start = state.Calls++ % population.Count;
            return Enumerable.Range(0, count).Select(index => population[(start + index) % population.Count]).ToArray();
        }
    }

    private sealed record ForwardingSelector(ISelector<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
        : Selector<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override SelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(instanceRegistry.Resolve(Inner));

        private sealed class Instance(ISelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : SelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Select(population, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record LastSelector : StatelessSelector<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            population.TakeLast(count).ToArray();
    }

    private sealed record DoublingWrappingSelector
        : WrappingSelector<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public DoublingWrappingSelector(ISelector<RealVector, RealVectorSearchSpace, TestFunctionProblem> childSelector)
            : base(childSelector)
        {
        }

        protected override WrappingSelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(
            ISelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childSelector) =>
            new Instance(childSelector);

        private sealed class Instance(ISelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childSelector)
            : WrappingSelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childSelector)
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var selected = ChildSelector.Select(population, objective, count, random, searchSpace, problem);
                return [.. selected, .. selected];
            }
        }
    }

    private sealed record PreferFirstMultiSelector
        : MultiSelector<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public PreferFirstMultiSelector(ImmutableArray<ISelector<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childSelectors)
            : base(childSelectors)
        {
        }

        protected override MultiSelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ImmutableArray<ISelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childSelectors) =>
            new Instance(childSelectors);

        private sealed class Instance(ImmutableArray<ISelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childSelectors)
            : MultiSelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childSelectors)
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildSelectors[0].Select(population, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record OffspringReplacer : StatelessReplacer<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            offspringPopulation.Take(count).ToArray();
    }

    private sealed record AlternatingStatefulReplacer : StatefulReplacer<RealVector, RealVectorSearchSpace, TestFunctionProblem, AlternatingStatefulReplacer.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            state.Calls++ % 2 == 0 ? previousPopulation.Take(count).ToArray() : offspringPopulation.Take(count).ToArray();
    }

    private sealed record ForwardingReplacer(IReplacer<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
        : Replacer<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateReplacerInstance(ExecutionInstanceRegistry registry) =>
            new Instance(registry.Resolve(Inner));

        private sealed class Instance(IReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : ReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record CounterSearchState(int Value) : SearchState;

    private sealed record IncrementingInterceptor : StatelessInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record CountingStatefulInterceptor : StatefulInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState, CountingStatefulInterceptor.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, ExecutionState state, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return currentState with { Value = currentState.Value + state.Calls };
        }
    }

    private sealed record ForwardingInterceptor(IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> Inner)
        : Interceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        protected override InterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateInterceptorInstance(ExecutionInstanceRegistry registry) =>
            new Instance(registry.Resolve(Inner));

        private sealed class Instance(IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> inner)
            : InterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Transform(currentState, previousState, searchSpace, problem);
        }
    }

    private sealed record ValueTerminator(int MaximumValue) : StatelessTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) => state.Value >= MaximumValue;
    }

    private sealed record CountingStatefulTerminator : StatefulTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState, CountingStatefulTerminator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override bool IsTerminalState(CounterSearchState searchState, ExecutionState state, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) => ++state.Calls >= 2;
    }

    private sealed record ForwardingTerminator(ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> Inner)
        : Terminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        protected override TerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateTerminatorInstance(ExecutionInstanceRegistry registry) =>
            new Instance(registry.Resolve(Inner));

        private sealed class Instance(ITerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> inner)
            : TerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public override bool IsTerminalState(CounterSearchState state, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) => inner.IsTerminalState(state, searchSpace, problem);
        }
    }

    private sealed record ForwardingCrossover(ICrossover<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
        : Crossover<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        protected override CrossoverInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateCrossoverInstance(ExecutionInstanceRegistry registry) =>
            new Instance(registry.Resolve(Inner));

        private sealed class Instance(ICrossoverInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : CrossoverInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Cross(parents, random, searchSpace, problem);
        }
    }

    private sealed record ConstantOneCreator
      : SingleSolutionCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector Create(
          IRandomNumberGenerator random,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return RealVector.Repeat(1.0, problem.TestFunction.Dimension);
        }
    }

    private sealed record NoChangeMutator
      : SingleCandidateMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector MutateCandidate(
          RealVector parent,
          IRandomNumberGenerator random,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return parent;
        }
    }

    private sealed record PullTowardZeroMutator
      : SingleCandidateMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector MutateCandidate(
          RealVector parent,
          IRandomNumberGenerator random,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return new RealVector(parent.Select(x => x * 0.5));
        }
    }

    private sealed record CountingStatefulMutator : StatefulMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingStatefulMutator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return parents.Select(parent => new RealVector(parent.Select(value => value + state.Calls))).ToArray();
        }
    }

    private sealed record ApplyTwiceMutator
        : WrappingMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public ApplyTwiceMutator(IMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem> childMutator)
            : base(childMutator)
        {
        }

        protected override WrappingMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(
            IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childMutator) =>
            new Instance(childMutator);

        private sealed class Instance(IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childMutator)
            : WrappingMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childMutator)
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var first = ChildMutator.Mutate(parents, random, searchSpace, problem);
                return ChildMutator.Mutate(first, random, searchSpace, problem);
            }
        }
    }

    private sealed record PushAwayFromZeroMutator
        : SingleCandidateMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector MutateCandidate(RealVector parent, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            return new RealVector(parent.Select(x => x * 2.0));
        }
    }

    private sealed record PreferFirstMultiMutator
        : MultiMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public PreferFirstMultiMutator(ImmutableArray<IMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childMutators)
            : base(childMutators)
        {
        }

        protected override MultiMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ImmutableArray<IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childMutators) =>
            new Instance(childMutators);

        private sealed class Instance(ImmutableArray<IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childMutators)
            : MultiMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childMutators)
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildMutators[0].Mutate(parents, random, searchSpace, problem);
        }
    }
}
