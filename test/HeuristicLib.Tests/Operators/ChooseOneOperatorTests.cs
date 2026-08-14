using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators;

public class ChooseOneOperatorTests
{
    [Fact]
    public void ChooseOneMutator_SnapshotsMutatorsAndWeights()
    {
        var first = new AddOffsetMutator(1);
        var second = new AddOffsetMutator(2);
        var mutators = new List<IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> { first, second };
        var weightValues = new[] { 1.0, 2.0 };
        var configuration = new ChooseOneMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutators) { Weights = weightValues.ToValueArray() };

        mutators.Clear();
        weightValues[0] = 100.0;

        configuration.ChildMutators.ShouldBe([first, second]);
        configuration.Weights.ShouldBe([1.0, 2.0]);
    }

    [Fact]
    public void ChooseOneMutator_PreservesStructuralConfigurationEquality()
    {
        var first = new AddOffsetMutator(1);
        var second = new AddOffsetMutator(2);

        var left = ChooseOneMutator.Create(
            [first, second],
            [1.0, 2.0]);
        var right = ChooseOneMutator.Create(
            [first, second],
            [1.0, 2.0]);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void ChooseOneCreator_ShouldPreserveAssignmentOrder()
    {
        var creator = ChooseOneCreator.Create(
          [new ConstantCreator(100), new ConstantCreator(200)],
          [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(creator);

        var result = instance.Create(3, new SequenceRandomNumberGenerator(0.2, 0.8, 0.3), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([100, 200, 100]);
    }

    [Fact]
    public void TransformedCreator_ShouldApplyMutatorToCreatedCandidates()
    {
        var creator = new ConstantCreator(5).TransformWith(new AddOffsetMutator(10));
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(creator);

        var result = instance.Create(3, RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([15, 15, 15]);
    }

    [Fact]
    public void TransformedCrossover_ShouldAlwaysApplyMutatorToCrossedCandidates()
    {
        var crossover = new FirstParentCrossover(100).TransformWith(new AddOffsetMutator(10));
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(crossover);

        var result = instance.Cross([Parents.From(1, 10), Parents.From(2, 20)], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([111, 112]);
    }

    [Fact]
    public void ChooseOneMutator_ShouldPreserveOriginalOrder()
    {
        var mutator = ChooseOneMutator.Create(
          [new AddOffsetMutator(100), new AddOffsetMutator(200)],
          [1.0, 1.0]);

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var rng = new SequenceRandomNumberGenerator(0.2, 0.8, 0.3);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(mutator);

        var result = instance.Mutate([1, 2, 3], rng, DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([101, 202, 103]);
    }

    [Fact]
    public void ChooseOneSelector_ShouldChooseOneSelectorForCompleteCall()
    {
        var selector = ChooseOneSelector.Create(
            [new FirstCandidatesSelector(), new LastCandidatesSelector()],
            [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var population = CreateEvaluatedCandidates(1, 2, 3);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(selector);

        var selected = instance.Select(population, problem.Objective, 2, new SequenceRandomNumberGenerator(0.8), problem.SearchSpace, problem);

        selected.Select(candidate => candidate.Candidate).ShouldBe([2, 3]);
    }

    [Fact]
    public void ChooseOneReplacer_ShouldChooseOneReplacerForCompleteCall()
    {
        var replacer = ChooseOneReplacer.Create(
            [new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer()],
            [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(replacer);

        var replaced = instance.Replace(CreateEvaluatedCandidates(1, 2), CreateEvaluatedCandidates(3, 4), problem.Objective, 2, new SequenceRandomNumberGenerator(0.8), problem.SearchSpace, problem);

        replaced.Select(candidate => candidate.Candidate).ShouldBe([3, 4]);
    }

    [Fact]
    public void ChooseOneOperators_ImplicitWeights_UseUniformSelection()
    {
        ImmutableArray<ICreator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> creators = [new ConstantCreator(100), new ConstantCreator(200)];
        ImmutableArray<ICrossover<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> crossovers = [new FirstParentCrossover(100), new SecondParentCrossover(200)];
        ImmutableArray<IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> mutators = [new AddOffsetMutator(100), new AddOffsetMutator(200)];
        ImmutableArray<ISelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> selectors = [new FirstCandidatesSelector(), new LastCandidatesSelector()];
        ImmutableArray<IReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> replacers = [new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer()];
        var creatorFromFactory = ChooseOneCreator.Create(new ConstantCreator(100), new ConstantCreator(200));
        var crossoverFromFactory = ChooseOneCrossover.Create(new FirstParentCrossover(100), new SecondParentCrossover(200));
        var mutatorFromFactory = ChooseOneMutator.Create(new AddOffsetMutator(100), new AddOffsetMutator(200));
        var selectorFromFactory = ChooseOneSelector.Create(new FirstCandidatesSelector(), new LastCandidatesSelector());
        var replacerFromFactory = ChooseOneReplacer.Create(new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer());

        new ChooseOneCreator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(creators).Weights.ShouldBeEmpty();
        creatorFromFactory.Weights.ShouldBeEmpty();
        new ChooseOneCrossover<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(crossovers).Weights.ShouldBeEmpty();
        crossoverFromFactory.Weights.ShouldBeEmpty();
        new ChooseOneMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutators).Weights.ShouldBeEmpty();
        mutatorFromFactory.Weights.ShouldBeEmpty();
        new ChooseOneSelector<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(selectors).Weights.ShouldBeEmpty();
        selectorFromFactory.Weights.ShouldBeEmpty();
        new ChooseOneReplacer<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(replacers).Weights.ShouldBeEmpty();
        replacerFromFactory.Weights.ShouldBeEmpty();
    }

    [Fact]
    public void ChooseOneMutator_EmptyWeights_AreRetainedAsUniformSelection()
    {
        var mutators = new IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>[]
        {
            new AddOffsetMutator(1),
            new AddOffsetMutator(2),
        };

        ImmutableArray<double> defaultWeights = default;
        var emptyWeightsMutator = new ChooseOneMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutators) { Weights = [] };
        var defaultWeightsMutator = new ChooseOneMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(mutators) { Weights = defaultWeights.ToValueArray() };

        emptyWeightsMutator.Weights.ShouldBeEmpty();
        defaultWeightsMutator.Weights.ShouldBeEmpty();
    }

    [Fact]
    public void WeightedBatchDispatch_PreservesOriginalWeightsAndPositiveInfinityOverridesFiniteWeights()
    {
        var dispatch = new WeightedBatchDispatch([1.0, 3.3, double.PositiveInfinity, double.PositiveInfinity]);
        var random = new SequenceRandomNumberGenerator(0.1, 0.9);

        dispatch.Weights.ShouldBe([1.0, 3.3, double.PositiveInfinity, double.PositiveInfinity]);
        dispatch.ChooseOperator(random, 4).ShouldBe(2);
        dispatch.ChooseOperator(random, 4).ShouldBe(3);
    }

    [Fact]
    public void WeightedBatchDispatch_IgnoresNeverWeightsAndPreservesPositiveFiniteRatios()
    {
        var dispatch = new WeightedBatchDispatch([double.NaN, double.NegativeInfinity, -2.0, 0.0, 1.0, 3.0]);
        var random = new SequenceRandomNumberGenerator(0.2, 0.8);

        dispatch.ChooseOperator(random, 6).ShouldBe(4);
        dispatch.ChooseOperator(random, 6).ShouldBe(5);
    }

    [Fact]
    public void WeightedBatchDispatch_AllNeverWeightsFallBackToUniformSelection()
    {
        var dispatch = new WeightedBatchDispatch([double.NaN, double.NegativeInfinity, -1.0, 0.0]);
        var random = new SequenceRandomNumberGenerator(0.1, 0.3, 0.6, 0.9);

        dispatch.ChooseOperator(random, 4).ShouldBe(0);
        dispatch.ChooseOperator(random, 4).ShouldBe(1);
        dispatch.ChooseOperator(random, 4).ShouldBe(2);
        dispatch.ChooseOperator(random, 4).ShouldBe(3);
    }

    [Fact]
    public void WeightedBatchDispatch_ScalesLargeFiniteWeightsWithoutOverflow()
    {
        var dispatch = new WeightedBatchDispatch([double.MaxValue, double.MaxValue / 2]);
        var random = new SequenceRandomNumberGenerator(0.2, 0.8);

        dispatch.ChooseOperator(random, 2).ShouldBe(0);
        dispatch.ChooseOperator(random, 2).ShouldBe(1);
    }

    [Fact]
    public void WeightedBatchDispatch_EmptyWeightsUseUniformSelectionAcrossOperators()
    {
        var dispatch = new WeightedBatchDispatch([]);
        var random = new SequenceRandomNumberGenerator(0.1, 0.6, 0.9);

        var result = dispatch.Dispatch(
            [1, 2, 3],
            [100, 200],
            random,
            static (value, batch) => Enumerable.Repeat(value, batch.Count).ToArray());

        dispatch.Weights.ShouldBeEmpty();
        result.ShouldBe([100, 200, 200]);
    }

    [Fact]
    public void ChooseOneOperators_ShouldRejectInvalidWeights()
    {
        // Weights are an optional setting rather than a constructor argument, so a count that disagrees with the
        // children is detected when the execution instance is built rather than when the configuration is created.
        var tooManyWeights = ChooseOneMutator.Create([new AddOffsetMutator(100)], [1.0, 1.0]);
        var tooFewWeights = ChooseOneCreator.Create([new ConstantCreator(100), new ConstantCreator(200)], [1.0]);

        Should.Throw<InvalidOperationException>(() => new Execution.ExecutionInstanceRegistry().Resolve(tooManyWeights));
        Should.Throw<InvalidOperationException>(() => new Execution.ExecutionInstanceRegistry().Resolve(tooFewWeights));

        var emptyCreator = ChooseOneCreator.Create<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();
        var emptyCrossover = ChooseOneCrossover.Create<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();
        var emptyMutator = ChooseOneMutator.Create<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();
        var emptySelector = ChooseOneSelector.Create<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();
        var emptyReplacer = ChooseOneReplacer.Create<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();
        var registry = new Execution.ExecutionInstanceRegistry();

        emptyCreator.ChildCreators.ShouldBeEmpty();
        emptyCrossover.ChildCrossovers.ShouldBeEmpty();
        emptyMutator.ChildMutators.ShouldBeEmpty();
        emptySelector.ChildSelectors.ShouldBeEmpty();
        emptyReplacer.ChildReplacers.ShouldBeEmpty();
        Should.Throw<InvalidOperationException>(() => registry.Resolve(emptyCreator));
        Should.Throw<InvalidOperationException>(() => registry.Resolve(emptyCrossover));
        Should.Throw<InvalidOperationException>(() => registry.Resolve(emptyMutator));
        Should.Throw<InvalidOperationException>(() => registry.Resolve(emptySelector));
        Should.Throw<InvalidOperationException>(() => registry.Resolve(emptyReplacer));
    }

    [Fact]
    public void ChooseOneOperators_ShouldRejectChildrenRemovedByReconfiguration()
    {
        var creator = ChooseOneCreator.Create(new ConstantCreator(1)) with { ChildCreators = [] };
        var crossover = ChooseOneCrossover.Create(new FirstParentCrossover(1)) with { ChildCrossovers = [] };
        var mutator = ChooseOneMutator.Create(new AddOffsetMutator(1)) with { ChildMutators = [] };
        var selector = ChooseOneSelector.Create(new FirstCandidatesSelector()) with { ChildSelectors = [] };
        var replacer = ChooseOneReplacer.Create(new PreviousCandidatesReplacer()) with { ChildReplacers = [] };
        var registry = new Execution.ExecutionInstanceRegistry();

        Should.Throw<InvalidOperationException>(() => registry.Resolve(creator));
        Should.Throw<InvalidOperationException>(() => registry.Resolve(crossover));
        Should.Throw<InvalidOperationException>(() => registry.Resolve(mutator));
        Should.Throw<InvalidOperationException>(() => registry.Resolve(selector));
        Should.Throw<InvalidOperationException>(() => registry.Resolve(replacer));
    }

    [Theory]
    [InlineData(-0.1, 1)]
    [InlineData(double.NegativeInfinity, 1)]
    [InlineData(double.NaN, 1)]
    [InlineData(1.1, 101)]
    [InlineData(double.PositiveInfinity, 101)]
    public void MutatorWithRate_UsesThresholdSemantics(double mutationRate, int expected)
    {
        var mutator = new AddOffsetMutator(100).WithRate(mutationRate);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(mutator);

        // A low draw selects the first entry under uniform fallback, so a rate that fails to make the
        // mutator unselectable is detected instead of being masked by an accidental fallback draw.
        var result = instance.Mutate([1], new SequenceRandomNumberGenerator(0.1), problem.SearchSpace, problem);

        result.ShouldBe([expected]);
    }

    [Theory]
    [InlineData(-0.1, 1)]
    [InlineData(double.NegativeInfinity, 1)]
    [InlineData(double.NaN, 1)]
    [InlineData(1.1, 101)]
    [InlineData(double.PositiveInfinity, 101)]
    public void CrossoverWithRate_UsesThresholdSemantics(double crossoverRate, int expected)
    {
        var crossover = new FirstParentCrossover(100).WithRate(crossoverRate);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(crossover);

        var result = instance.Cross([Parents.From(1, 10)], new SequenceRandomNumberGenerator(0.1), problem.SearchSpace, problem);

        result.ShouldBe([expected]);
    }

    [Fact]
    public void ChooseOneCrossover_ShouldPreserveOriginalOrder()
    {
        var crossover = ChooseOneCrossover.Create(
          [new FirstParentCrossover(100), new SecondParentCrossover(200)],
          [1.0, 1.0]);

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var rng = new SequenceRandomNumberGenerator(0.2, 0.8, 0.3);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(crossover);

        var result = instance.Cross(
          [Parents.From(1, 10), Parents.From(2, 20), Parents.From(3, 30)],
          rng,
          DummySearchSpace<int>.Instance,
          problem);

        result.ShouldBe([101, 220, 103]);
    }

    [Fact]
    public void PipelineMutator_ShouldApplyMutatorsInSequence()
    {
        var mutator = new AddOffsetMutator(10).Then(new AddOffsetMutator(100));

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(mutator);

        var result = instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([111, 112, 113]);
    }

    [Fact]
    public void PipelineMutator_ShouldForwardSameInvocationDataToContractOnlyStages()
    {
        IReadOnlyList<int> parents = [1, 2, 3];
        var random = RandomNumberGenerator.Create(0);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var stageOrder = new List<int>();
        var first = new CallbackInstanceMutator((actualParents, actualRandom, actualSearchSpace, actualProblem) =>
        {
            actualParents.ShouldBeSameAs(parents);
            actualRandom.ShouldBeSameAs(random);
            actualSearchSpace.ShouldBeSameAs(problem.SearchSpace);
            actualProblem.ShouldBeSameAs(problem);
            stageOrder.Add(1);
            return actualParents;
        });
        var second = new CallbackInstanceMutator((actualParents, actualRandom, actualSearchSpace, actualProblem) =>
        {
            actualParents.ShouldBeSameAs(parents);
            actualRandom.ShouldBeSameAs(random);
            actualSearchSpace.ShouldBeSameAs(problem.SearchSpace);
            actualProblem.ShouldBeSameAs(problem);
            stageOrder.Add(2);
            return actualParents;
        });
        var instance = PipelineMutator.Create(first, second).CreateExecutionInstance(new Execution.ExecutionInstanceRegistry());

        var result = instance.Mutate(parents, random, problem.SearchSpace, problem);

        result.ShouldBeSameAs(parents);
        stageOrder.ShouldBe([1, 2]);
    }

    [Fact]
    public void PipelineMutator_ShouldStopAfterAStageThrows()
    {
        var finalStageCalled = false;
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var first = new CallbackInstanceMutator((parents, _, _, _) => parents);
        var throwing = new CallbackInstanceMutator((_, _, _, _) => throw new InvalidOperationException());
        var final = new CallbackInstanceMutator((parents, _, _, _) =>
        {
            finalStageCalled = true;
            return parents;
        });
        var instance = PipelineMutator.Create(first, throwing, final).CreateExecutionInstance(new Execution.ExecutionInstanceRegistry());

        Should.Throw<InvalidOperationException>(() =>
            instance.Mutate([1], RandomNumberGenerator.Create(0), problem.SearchSpace, problem));

        finalStageCalled.ShouldBeFalse();
    }

    [Fact]
    public void PipelineMutator_ShouldResolveChildMutatorsOncePerExecutionInstance()
    {
        var countingMutator = new CountingInstanceMutator();
        var mutator = PipelineMutator.Create(countingMutator);

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(mutator);

        var first = instance.Mutate([1], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);
        var second = instance.Mutate([1], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, problem);

        countingMutator.ExecutionInstancesCreated.ShouldBe(1);
        first.ShouldBe([2]);
        second.ShouldBe([3]);
    }

    [Fact]
    public void DurationWrappers_SharingAChildMutator_ReuseItsExecutionInstance()
    {
        var innerOperator = new CountingInstanceMutator();
        var firstWrapper = innerOperator.MeasureMutatorDuration(new ObservationDuration());
        var secondWrapper = innerOperator.MeasureMutatorDuration(new ObservationDuration());
        var registry = new Execution.ExecutionInstanceRegistry();
        var firstInstance = registry.Resolve(firstWrapper);
        var secondInstance = registry.Resolve(secondWrapper);
        var problem = FuncProblem.Create(
            evaluateFunc: (int x) => x,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);

        var first = firstInstance.Mutate([1], RandomNumberGenerator.Create(0), problem.SearchSpace, problem);
        var second = secondInstance.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        innerOperator.ExecutionInstancesCreated.ShouldBe(1);
        first.ShouldBe([2]);
        second.ShouldBe([3]);
    }

    [Fact]
    public void ChooseOneMutator_RepeatedChildMutator_ReusesItsExecutionInstance()
    {
        var innerOperator = new CountingInstanceMutator();
        var chooseOne = ChooseOneMutator.Create(innerOperator, innerOperator);

        _ = new Execution.ExecutionInstanceRegistry().Resolve(chooseOne);

        innerOperator.ExecutionInstancesCreated.ShouldBe(1);
    }

    [Fact]
    public void PipelineMutator_EmptyPipeline_IsIdentity()
    {
        var pipeline = new PipelineMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>([]);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(pipeline);
        var parents = new[] { 1, 2, 3 };
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

        var result = instance.Mutate(parents, RandomNumberGenerator.Create(0), problem.SearchSpace, problem);

        result.ShouldBeSameAs(parents);
    }

    [Fact]
    public void PipelineMutator_DefaultImmutableArray_IsNormalizedToEmpty()
    {
        ImmutableArray<IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>> childMutators = default;

        var pipeline = new PipelineMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(childMutators);

        pipeline.ChildMutators.ShouldBeEmpty();
    }

    [Fact]
    public void PipelineInterceptor_ShouldApplyInterceptorsInSequence()
    {
        var interceptor = new AddToStateInterceptor(10).Then(new AddToStateInterceptor(100));

        var problem = FuncProblem.Create(
          evaluateFunc: (int x) => x,
          encoding: DummySearchSpace<int>.Instance,
          objective: SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(interceptor);

        var result = instance.Transform(new TestAlgorithmState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, problem);

        result.Value.ShouldBe(111);
    }

    [Fact]
    public void PipelineInterceptor_EmptyPipeline_ShouldBeIdentity()
    {
        var interceptor = new PipelineInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>([]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var instance = new Execution.ExecutionInstanceRegistry().Resolve(interceptor);
        var state = new TestAlgorithmState { Value = 1 };

        instance.Transform(state, previousState: null, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, problem).ShouldBeSameAs(state);
    }

    [Fact]
    public void PipelineInterceptor_PassesSameRandomAndOriginalPreviousStateToEveryStage()
    {
        var observations = new List<(TestAlgorithmState? PreviousState, IRandomNumberGenerator Random)>();
        var interceptor = new RecordingStateInterceptor(observations).Then(new RecordingStateInterceptor(observations));
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var previousState = new TestAlgorithmState { Value = 0 };
        var random = RandomNumberGenerator.Create(1);

        new Execution.ExecutionInstanceRegistry().Resolve(interceptor)
            .Transform(new TestAlgorithmState { Value = 1 }, previousState, random, DummySearchSpace<int>.Instance, problem);

        observations.Count.ShouldBe(2);
        observations.ShouldAllBe(observation => ReferenceEquals(observation.PreviousState, previousState));
        observations.ShouldAllBe(observation => ReferenceEquals(observation.Random, random));
    }

    [Fact]
    public void LogicalTerminators_ShouldShortCircuit()
    {
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var state = new TestAlgorithmState { Value = 1 };
        var registry = new Execution.ExecutionInstanceRegistry();
        var any = registry.Resolve(new ConstantTerminator(true).Or(new ThrowingTerminator()));
        var all = registry.Resolve(new ConstantTerminator(false).And(new ThrowingTerminator()));

        any.IsTerminalState(state, DummySearchSpace<int>.Instance, problem).ShouldBeTrue();
        all.IsTerminalState(state, DummySearchSpace<int>.Instance, problem).ShouldBeFalse();
    }

    [Fact]
    public void EmptyLogicalTerminators_ShouldUseBooleanIdentity()
    {
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var state = new TestAlgorithmState { Value = 1 };
        var registry = new Execution.ExecutionInstanceRegistry();
        var any = registry.Resolve(new AnyTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>());
        var all = registry.Resolve(new AllTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>());

        any.IsTerminalState(state, DummySearchSpace<int>.Instance, problem).ShouldBeFalse();
        all.IsTerminalState(state, DummySearchSpace<int>.Instance, problem).ShouldBeTrue();
    }

    private sealed record AddOffsetMutator(int Offset) : SingleCandidateMutator<int, DummySearchSpace<int>>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parent + Offset;
    }

    private sealed record ConstantCreator(int Value) : SingleCandidateCreator<int, DummySearchSpace<int>>
    {
        public override int CreateCandidate(IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => Value;
    }

    private sealed record FirstParentCrossover(int Offset) : SingleCandidateCrossover<int, DummySearchSpace<int>>
    {
        public override int CrossParents(Parents<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parents.Parent1 + Offset;
    }

    private sealed record SecondParentCrossover(int Offset) : SingleCandidateCrossover<int, DummySearchSpace<int>>
    {
        public override int CrossParents(Parents<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parents.Parent2 + Offset;
    }

    private sealed record AddToStateInterceptor(int Offset)
      : StatelessInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
    {
        public override TestAlgorithmState Transform(TestAlgorithmState currentState, TestAlgorithmState? previousState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
            => currentState with { Value = currentState.Value + Offset };
    }

    private sealed record RecordingStateInterceptor(List<(TestAlgorithmState? PreviousState, IRandomNumberGenerator Random)> Observations)
      : StatelessInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
    {
        public override TestAlgorithmState Transform(TestAlgorithmState currentState, TestAlgorithmState? previousState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
        {
            Observations.Add((previousState, random));
            return currentState;
        }
    }

    private sealed record FirstCandidatesSelector : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            population.Take(count).ToArray();
    }

    private sealed record LastCandidatesSelector : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            population.TakeLast(count).ToArray();
    }

    private sealed record PreviousCandidatesReplacer : StatelessReplacer<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            previousPopulation.Take(count).ToArray();
    }

    private sealed record OffspringCandidatesReplacer : StatelessReplacer<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation, IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            offspringPopulation.Take(count).ToArray();
    }

    private sealed record ConstantTerminator(bool Result)
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
    {
        public override bool IsTerminalState(TestAlgorithmState state, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) => Result;
    }

    private sealed record ThrowingTerminator
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
    {
        public override bool IsTerminalState(TestAlgorithmState state, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException("This terminator should not be reached.");
    }

    private static IReadOnlyList<EvaluatedCandidate<int>> CreateEvaluatedCandidates(params int[] candidates) =>
        candidates.Select(candidate => EvaluatedCandidate.From(candidate, new ObjectiveVector(candidate))).ToArray();

    private sealed record TestAlgorithmState : SearchState
    {
        public required int Value { get; init; }
    }

    private sealed class CountingInstanceMutator : IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public int ExecutionInstancesCreated { get; private set; }

        public IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(Execution.ExecutionInstanceRegistry instanceRegistry)
        {
            ExecutionInstancesCreated++;
            return new Instance();
        }

        private sealed class Instance : IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
        {
            private int calls;

            public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
            {
                calls++;
                return parents.Select(parent => parent + calls).ToArray();
            }
        }
    }

    private delegate IReadOnlyList<int> MutationCallback(
        IReadOnlyList<int> parents,
        IRandomNumberGenerator random,
        DummySearchSpace<int> searchSpace,
        IProblem<int, DummySearchSpace<int>> problem);

    private sealed record CallbackInstanceMutator(MutationCallback Callback)
        : IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        public IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(Execution.ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(Callback);

        private sealed class Instance(MutationCallback callback)
            : IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
        {
            public IReadOnlyList<int> Mutate(
                IReadOnlyList<int> parents,
                IRandomNumberGenerator random,
                DummySearchSpace<int> searchSpace,
                IProblem<int, DummySearchSpace<int>> problem) =>
                callback(parents, random, searchSpace, problem);
        }
    }
}
