using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators;

public class RoutedOperatorTests
{
    [Fact]
    public void ChoosePerItemMutation_SnapshotsMutatorsAndWeights()
    {
        var first = new AddOffsetMutator(1);
        var second = new AddOffsetMutator(2);
        var mutators = new List<IOperator<Mutation, int>> { first, second };
        var weightValues = new[] { 1.0, 2.0 };
        var configuration = new ChoosePerItem<Mutation, int>(mutators) { Weights = weightValues.ToValueArray() };

        mutators.Clear();
        weightValues[0] = 100.0;

        configuration.ChildOperators.ShouldBe([first, second]);
        configuration.Weights.ShouldBe([1.0, 2.0]);
    }

    [Fact]
    public void ChoosePerItemMutation_PreservesStructuralConfigurationEquality()
    {
        var first = new AddOffsetMutator(1);
        var second = new AddOffsetMutator(2);

        var left = ChoosePerItem.Create(
            [first, second],
            [1.0, 2.0]);
        var right = ChoosePerItem.Create(
            [first, second],
            [1.0, 2.0]);

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void ChoosePerItemCreation_ShouldPreserveAssignmentOrder()
    {
        var creator = ChoosePerItem.Create(
            [new ConstantCreator(100), new ConstantCreator(200)],
            [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var executor = ResolutionScope.Create().ResolveCreator(creator);

        var result = executor.Create(3, new SequenceRandomNumberGenerator(0.2, 0.8, 0.3), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([100, 200, 100]);
    }

    [Fact]
    public void TransformedCreator_ShouldApplyMutatorToCreatedCandidates()
    {
        var creator = new ConstantCreator(5).TransformWith(new AddOffsetMutator(10));
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var executor = ResolutionScope.Create().ResolveCreator(creator);

        var result = executor.Create(3, RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([15, 15, 15]);
    }

    [Fact]
    public void TransformedCrossover_ShouldAlwaysApplyMutatorToCrossedCandidates()
    {
        var crossover = new FirstParentCrossover(100).TransformWith(new AddOffsetMutator(10));
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var executor = ResolutionScope.Create().ResolveCrossover(crossover);

        var result = executor.Cross([Parents.From(1, 10), Parents.From(2, 20)], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([111, 112]);
    }

    [Fact]
    public void ChoosePerItemMutation_ShouldPreserveOriginalOrder()
    {
        var mutator = ChoosePerItem.Create(
            [new AddOffsetMutator(100), new AddOffsetMutator(200)],
            [1.0, 1.0]);

        var problem = FuncProblem.Create(
            evaluateFunc: (int x) => x,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
        var rng = new SequenceRandomNumberGenerator(0.2, 0.8, 0.3);
        var executor = ResolutionScope.Create().ResolveMutator(mutator);

        var result = executor.Mutate([1, 2, 3], rng, DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([101, 202, 103]);
    }

    [Fact]
    public void ChoosePerItemEvaluation_ShouldPreserveOriginalOrder()
    {
        var evaluator = ChoosePerItem.Create(
            [new OffsetEvaluator(100), new OffsetEvaluator(200)],
            [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var executor = ResolutionScope.Create().ResolveEvaluator(evaluator);

        var objectives = executor.Evaluate([1, 2, 3], new SequenceRandomNumberGenerator(0.2, 0.8, 0.3), problem.SearchSpace, problem);

        objectives.Select(vector => vector[0]).ShouldBe([101.0, 202.0, 103.0]);
    }

    [Fact]
    public void ChoosePerCallSelector_ShouldChooseOneSelectorForCompleteCall()
    {
        var selector = ChoosePerCall.Create(
            [new FirstCandidatesSelector(), new LastCandidatesSelector()],
            [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var population = CreateEvaluatedCandidates(1, 2, 3);
        var executor = ResolutionScope.Create().ResolveSelector(selector);

        var selected = executor.Select(population, problem.Objective, 2, new SequenceRandomNumberGenerator(0.8), problem.SearchSpace, problem);

        selected.Select(candidate => candidate.Candidate).ShouldBe([2, 3]);
    }

    [Fact]
    public void ChoosePerCallReplacer_ShouldChooseOneReplacerForCompleteCall()
    {
        var replacer = ChoosePerCall.Create(
            [new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer()],
            [1.0, 1.0]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var executor = ResolutionScope.Create().ResolveReplacer(replacer);

        var replaced = executor.Replace(CreateEvaluatedCandidates(1, 2), CreateEvaluatedCandidates(3, 4), problem.Objective, 2, new SequenceRandomNumberGenerator(0.8), problem.SearchSpace, problem);

        replaced.Select(candidate => candidate.Candidate).ShouldBe([3, 4]);
    }

    [Fact]
    public void RoutedOperators_ImplicitWeights_UseUniformSelection()
    {
        ImmutableArray<IOperator<Creation, int>> creators = [new ConstantCreator(100), new ConstantCreator(200)];
        ImmutableArray<IOperator<Crossover, int>> crossovers = [new FirstParentCrossover(100), new SecondParentCrossover(200)];
        ImmutableArray<IOperator<Mutation, int>> mutators = [new AddOffsetMutator(100), new AddOffsetMutator(200)];
        ImmutableArray<IOperator<Selection, int>> selectors = [new FirstCandidatesSelector(), new LastCandidatesSelector()];
        ImmutableArray<IOperator<Replacement, int>> replacers = [new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer()];
        var creatorFromFactory = ChoosePerItem.Create(new ConstantCreator(100), new ConstantCreator(200));
        var crossoverFromFactory = ChoosePerItem.Create(new FirstParentCrossover(100), new SecondParentCrossover(200));
        var mutatorFromFactory = ChoosePerItem.Create(new AddOffsetMutator(100), new AddOffsetMutator(200));
        var selectorFromFactory = ChoosePerCall.Create(new FirstCandidatesSelector(), new LastCandidatesSelector());
        var replacerFromFactory = ChoosePerCall.Create(new PreviousCandidatesReplacer(), new OffspringCandidatesReplacer());

        new ChoosePerItem<Creation, int>(creators).Weights.ShouldBeEmpty();
        creatorFromFactory.Weights.ShouldBeEmpty();
        new ChoosePerItem<Crossover, int>(crossovers).Weights.ShouldBeEmpty();
        crossoverFromFactory.Weights.ShouldBeEmpty();
        new ChoosePerItem<Mutation, int>(mutators).Weights.ShouldBeEmpty();
        mutatorFromFactory.Weights.ShouldBeEmpty();
        new ChoosePerCall<Selection, int>(selectors).Weights.ShouldBeEmpty();
        selectorFromFactory.Weights.ShouldBeEmpty();
        new ChoosePerCall<Replacement, int>(replacers).Weights.ShouldBeEmpty();
        replacerFromFactory.Weights.ShouldBeEmpty();
    }

    [Fact]
    public void ChoosePerItemMutation_EmptyWeights_AreRetainedAsUniformSelection()
    {
        var mutators = new IOperator<Mutation, int>[]
        {
            new AddOffsetMutator(1),
            new AddOffsetMutator(2),
        };

        ImmutableArray<double> defaultWeights = default;
        var emptyWeightsMutator = new ChoosePerItem<Mutation, int>(mutators) { Weights = [] };
        var defaultWeightsMutator = new ChoosePerItem<Mutation, int>(mutators) { Weights = defaultWeights.ToValueArray() };

        emptyWeightsMutator.Weights.ShouldBeEmpty();
        defaultWeightsMutator.Weights.ShouldBeEmpty();
    }

    [Fact]
    public void WeightedRouter_RoutesEachInputAndRestoresInputOrder()
    {
        var router = WeightedRouter.Create(100, 200);
        var random = new SequenceRandomNumberGenerator(0.1, 0.6, 0.9);

        var result = router.Route([1, 2, 3], random, static (value, batch) => Enumerable.Repeat(value, batch.Count).ToArray());

        result.ShouldBe([100, 200, 200]);
    }

    [Fact]
    public void WeightedRouter_RoutesARequestedCountWithoutPlaceholderInputs()
    {
        var router = WeightedRouter.Create(100, 200);
        var random = new SequenceRandomNumberGenerator(0.1, 0.6, 0.9);

        var result = router.Route(3, random, static (value, items) => Enumerable.Repeat(value, items.Count).ToArray());

        result.ShouldBe([100, 200, 200]);
    }

    [Fact]
    public void WeightedRouter_HandsTheOriginalInputsToASingleCertainChoice()
    {
        var router = WeightedRouter.Create([100, 200], [0.0, 1.0]);
        IReadOnlyList<int> inputs = [1, 2, 3];
        IReadOnlyList<int>? received = null;

        // The empty sequence throws on any draw, so this also shows that a certain choice draws nothing.
        var result = router.Route(inputs, new SequenceRandomNumberGenerator(), (offset, batch) =>
        {
            received = batch;
            return batch.Select(input => input + offset).ToArray();
        });

        received.ShouldBeSameAs(inputs);
        result.ShouldBe([201, 202, 203]);
    }

    [Fact]
    public void WeightedRouter_RejectsABatchResultOfTheWrongSize()
    {
        var router = WeightedRouter.Create(100, 200);

        Should.Throw<InvalidOperationException>(() =>
            router.Route([1, 2, 3], new SequenceRandomNumberGenerator(0.1, 0.6, 0.9), static (_, batch) => batch.Skip(1).ToArray()));
    }

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(-0.1, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(double.NaN, false)]
    [InlineData(1.0, true)]
    [InlineData(1.1, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void WeightedRouter_AtRate_RoutesExtremeRatesWithoutDrawing(double rate, bool expected)
    {
        var result = WeightedRouter.AtRate(rate).Route([1, 2], new SequenceRandomNumberGenerator(), static (routed, batch) => batch.Select(_ => routed).ToArray());

        result.ShouldBe([expected, expected]);
    }

    [Fact]
    public void WeightedRouter_AtRate_RoutesAsAThreshold()
    {
        var result = WeightedRouter.AtRate(0.3).Route([1, 2, 3], new SequenceRandomNumberGenerator(0.1, 0.5, 0.29), static (routed, batch) => batch.Select(_ => routed).ToArray());

        result.ShouldBe([true, false, true]);
    }

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(-0.1, false)]
    [InlineData(double.NegativeInfinity, false)]
    [InlineData(double.NaN, false)]
    [InlineData(1.0, true)]
    [InlineData(1.1, true)]
    [InlineData(double.PositiveInfinity, true)]
    public void WeightedRouter_AtRate_RoutesTheSuppliedReferenceOrNullWithoutDrawing(double rate, bool applied)
    {
        var choice = new object();
        var result = WeightedRouter.AtRate(choice, rate).Route([1, 2], new SequenceRandomNumberGenerator(),
            static (selected, batch) => Enumerable.Repeat(selected, batch.Count).ToArray());

        result.ShouldAllBe(selected => ReferenceEquals(selected, applied ? choice : null));
    }

    [Fact]
    public void WeightedRouter_AtRate_RoutesTheSuppliedReferenceOrNullAsAThreshold()
    {
        var result = WeightedRouter.AtRate("mutate", 0.3).Route([1, 2, 3], new SequenceRandomNumberGenerator(0.1, 0.5, 0.29),
            static (selected, batch) => Enumerable.Repeat(selected, batch.Count).ToArray());

        result.ShouldBe(["mutate", null, "mutate"]);
    }

    [Fact]
    public void RoutedOperators_ShouldRejectInvalidWeights()
    {
        // Weights are an optional setting rather than a constructor argument, so a count that disagrees with the
        // children is detected when the executor is built rather than when the configuration is created.
        var tooManyWeights = ChoosePerItem.Create([new AddOffsetMutator(100)], [1.0, 1.0]);
        var tooFewWeights = ChoosePerItem.Create([new ConstantCreator(100), new ConstantCreator(200)], [1.0]);

        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().ResolveMutator(tooManyWeights));
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().ResolveCreator(tooFewWeights));

        var emptyCreator = ChoosePerItem.Create<Creation, int>();
        var emptyCrossover = ChoosePerItem.Create<Crossover, int>();
        var emptyMutator = ChoosePerItem.Create<Mutation, int>();
        var emptySelector = ChoosePerCall.Create<Selection, int>();
        var emptyReplacer = ChoosePerCall.Create<Replacement, int>();
        var scope = ResolutionScope.Create();

        emptyCreator.ChildOperators.ShouldBeEmpty();
        emptyCrossover.ChildOperators.ShouldBeEmpty();
        emptyMutator.ChildOperators.ShouldBeEmpty();
        emptySelector.ChildOperators.ShouldBeEmpty();
        emptyReplacer.ChildOperators.ShouldBeEmpty();
        Should.Throw<InvalidOperationException>(() => scope.ResolveCreator(emptyCreator));
        Should.Throw<InvalidOperationException>(() => scope.ResolveCrossover(emptyCrossover));
        Should.Throw<InvalidOperationException>(() => scope.ResolveMutator(emptyMutator));
        Should.Throw<InvalidOperationException>(() => scope.ResolveSelector(emptySelector));
        Should.Throw<InvalidOperationException>(() => scope.ResolveReplacer(emptyReplacer));
    }

    [Fact]
    public void RoutedOperators_ShouldRejectChildrenRemovedByReconfiguration()
    {
        var creator = ChoosePerItem.Create(new ConstantCreator(1)) with { ChildOperators = [] };
        var crossover = ChoosePerItem.Create(new FirstParentCrossover(1)) with { ChildOperators = [] };
        var mutator = ChoosePerItem.Create(new AddOffsetMutator(1)) with { ChildOperators = [] };
        var selector = ChoosePerCall.Create(new FirstCandidatesSelector()) with { ChildOperators = [] };
        var replacer = ChoosePerCall.Create(new PreviousCandidatesReplacer()) with { ChildOperators = [] };
        var scope = ResolutionScope.Create();

        Should.Throw<InvalidOperationException>(() => scope.ResolveCreator(creator));
        Should.Throw<InvalidOperationException>(() => scope.ResolveCrossover(crossover));
        Should.Throw<InvalidOperationException>(() => scope.ResolveMutator(mutator));
        Should.Throw<InvalidOperationException>(() => scope.ResolveSelector(selector));
        Should.Throw<InvalidOperationException>(() => scope.ResolveReplacer(replacer));
    }

    [Theory]
    [InlineData(-0.1, 1)]
    [InlineData(double.NegativeInfinity, 1)]
    [InlineData(double.NaN, 1)]
    [InlineData(1.1, 101)]
    [InlineData(double.PositiveInfinity, 101)]
    public void MutatorWithRate_UsesThresholdSemantics(double mutationRate, int expected)
    {
        var mutator = new AddOffsetMutator(100).AppliedAtRate(mutationRate);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var executor = ResolutionScope.Create().ResolveMutator(mutator);

        // A low draw selects the first entry under uniform fallback, so a rate that fails to make the
        // mutator unselectable is detected instead of being masked by an accidental fallback draw.
        var result = executor.Mutate([1], new SequenceRandomNumberGenerator(0.1), problem.SearchSpace, problem);

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
        var crossover = new FirstParentCrossover(100).AppliedAtRate(crossoverRate);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var executor = ResolutionScope.Create().ResolveCrossover(crossover);

        var result = executor.Cross([Parents.From(1, 10)], new SequenceRandomNumberGenerator(0.1), problem.SearchSpace, problem);

        result.ShouldBe([expected]);
    }

    [Fact]
    public void ChoosePerItemCrossover_ShouldPreserveOriginalOrder()
    {
        var crossover = ChoosePerItem.Create(
            [new FirstParentCrossover(100), new SecondParentCrossover(200)],
            [1.0, 1.0]);

        var problem = FuncProblem.Create(
            evaluateFunc: (int x) => x,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
        var rng = new SequenceRandomNumberGenerator(0.2, 0.8, 0.3);
        var executor = ResolutionScope.Create().ResolveCrossover(crossover);

        var result = executor.Cross(
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
        var executor = ResolutionScope.Create().ResolveMutator(mutator);

        var result = executor.Mutate([1, 2, 3], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

        result.ShouldBe([111, 112, 113]);
    }

    [Fact]
    public void PipelineMutator_ShouldForwardSameInvocationDataToContractOnlyStages()
    {
        IReadOnlyList<int> parents = [1, 2, 3];
        var random = RandomNumberGenerator.Create(0);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var stageOrder = new List<int>();
        var first = new CallbackExecutionMutator((actualParents, actualRandom, actualSearchSpace, actualProblem) =>
        {
            actualParents.ShouldBeSameAs(parents);
            actualRandom.ShouldBeSameAs(random);
            actualSearchSpace.ShouldBeSameAs(problem.SearchSpace);
            actualProblem.ShouldBeSameAs(problem);
            stageOrder.Add(1);
            return actualParents;
        });
        var second = new CallbackExecutionMutator((actualParents, actualRandom, actualSearchSpace, actualProblem) =>
        {
            actualParents.ShouldBeSameAs(parents);
            actualRandom.ShouldBeSameAs(random);
            actualSearchSpace.ShouldBeSameAs(problem.SearchSpace);
            actualProblem.ShouldBeSameAs(problem);
            stageOrder.Add(2);
            return actualParents;
        });
        var executor = PipelineMutator.Create(first, second).CreateMutatorExecution();

        var result = executor.Mutate(parents, random, problem.SearchSpace, problem);

        result.ShouldBeSameAs(parents);
        stageOrder.ShouldBe([1, 2]);
    }

    [Fact]
    public void PipelineMutator_ShouldStopAfterAStageThrows()
    {
        var finalStageCalled = false;
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var first = new CallbackExecutionMutator((parents, _, _, _) => parents);
        var throwing = new CallbackExecutionMutator((_, _, _, _) => throw new InvalidOperationException());
        var final = new CallbackExecutionMutator((parents, _, _, _) =>
        {
            finalStageCalled = true;
            return parents;
        });
        var executor = PipelineMutator.Create(first, throwing, final).CreateMutatorExecution();

        Should.Throw<InvalidOperationException>(() =>
            executor.Mutate([1], RandomNumberGenerator.Create(0), problem.SearchSpace, problem));

        finalStageCalled.ShouldBeFalse();
    }

    [Fact]
    public void PipelineMutator_ShouldResolveChildMutatorsOncePerExecution()
    {
        var countingMutator = new CountingExecutionMutator();
        var mutator = PipelineMutator.Create(countingMutator);

        var problem = FuncProblem.Create(
            evaluateFunc: (int x) => x,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
        var executor = ResolutionScope.Create().ResolveMutator(mutator);

        var first = executor.Mutate([1], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);
        var second = executor.Mutate([1], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, problem);

        countingMutator.ExecutionsCreated.ShouldBe(1);
        first.ShouldBe([2]);
        second.ShouldBe([3]);
    }

    [Fact]
    public void DurationWrappers_SharingAChildMutator_ReuseItsExecution()
    {
        var innerOperator = new CountingExecutionMutator();
        var firstWrapper = Aspect.MeasureDuration(innerOperator, new DurationAccumulator());
        var secondWrapper = Aspect.MeasureDuration(innerOperator, new DurationAccumulator());
        var scope = ResolutionScope.Create();
        var firstExecutor = scope.With(firstWrapper).ResolveMutator(innerOperator);
        var secondExecutor = scope.With(secondWrapper).ResolveMutator(innerOperator);
        var problem = FuncProblem.Create(
            evaluateFunc: (int x) => x,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);

        var first = firstExecutor.Mutate([1], RandomNumberGenerator.Create(0), problem.SearchSpace, problem);
        var second = secondExecutor.Mutate([1], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        innerOperator.ExecutionsCreated.ShouldBe(1);
        first.ShouldBe([2]);
        second.ShouldBe([3]);
    }

    [Fact]
    public void ChoosePerItemMutation_RepeatedChildMutator_ReusesItsExecution()
    {
        var innerOperator = new CountingExecutionMutator();
        var chooseOne = ChoosePerItem.Create(innerOperator, innerOperator);

        _ = ResolutionScope.Create().ResolveMutator(chooseOne);

        innerOperator.ExecutionsCreated.ShouldBe(1);
    }

    [Fact]
    public void PipelineMutator_EmptyPipeline_IsIdentity()
    {
        var pipeline = new PipelineMutator<int>([]);
        var executor = ResolutionScope.Create().ResolveMutator(pipeline);
        var parents = new[] { 1, 2, 3 };
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

        var result = executor.Mutate(parents, RandomNumberGenerator.Create(0), problem.SearchSpace, problem);

        result.ShouldBeSameAs(parents);
    }

    [Fact]
    public void PipelineMutator_DefaultImmutableArray_IsNormalizedToEmpty()
    {
        ImmutableArray<IOperator<Mutation, int>> childMutators = default;

        var pipeline = new PipelineMutator<int>(childMutators);

        pipeline.ChildOperators.ShouldBeEmpty();
    }

    [Fact]
    public void PipelineInterceptor_ShouldApplyInterceptorsInSequence()
    {
        var interceptor = new AddToStateInterceptor(10).Then(new AddToStateInterceptor(100));

        var problem = FuncProblem.Create(
            evaluateFunc: (int x) => x,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TestAlgorithmState>(interceptor);

        var result = execution.Transform(new TestAlgorithmState { Value = 1 }, previousState: null, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, problem);

        result.Value.ShouldBe(111);
    }

    [Fact]
    public void PipelineInterceptor_EmptyPipeline_ShouldBeIdentity()
    {
        var interceptor = new PipelineInterceptor<int, TestAlgorithmState>([]);
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var execution = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TestAlgorithmState>(interceptor);
        var state = new TestAlgorithmState { Value = 1 };

        execution.Transform(state, previousState: null, RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, problem).ShouldBeSameAs(state);
    }

    [Fact]
    public void PipelineInterceptor_PassesSameRandomAndOriginalPreviousStateToEveryStage()
    {
        var observations = new List<(TestAlgorithmState? PreviousState, IRandomNumberGenerator Random)>();
        var interceptor = new RecordingStateInterceptor(observations).Then(new RecordingStateInterceptor(observations));
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var previousState = new TestAlgorithmState { Value = 0 };
        var random = RandomNumberGenerator.Create(1);

        ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TestAlgorithmState>(interceptor)
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
        var scope = ResolutionScope.Create();
        var any = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TestAlgorithmState>(new ConstantTerminator(true).Or(new ThrowingTerminator()));
        var all = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TestAlgorithmState>(new ConstantTerminator(false).And(new ThrowingTerminator()));

        any.IsTerminalState(state, RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem).ShouldBeTrue();
        all.IsTerminalState(state, RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem).ShouldBeFalse();
    }

    [Fact]
    public void EmptyLogicalTerminators_ShouldUseBooleanIdentity()
    {
        var problem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var state = new TestAlgorithmState { Value = 1 };
        var scope = ResolutionScope.Create();
        var any = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TestAlgorithmState>(new AnyTerminator<int>());
        var all = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TestAlgorithmState>(new AllTerminator<int>());

        any.IsTerminalState(state, RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem).ShouldBeFalse();
        all.IsTerminalState(state, RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem).ShouldBeTrue();
    }

    [Theory]
    [InlineData("creation")]
    [InlineData("crossover")]
    [InlineData("mutation")]
    [InlineData("refinement")]
    public void PerItemRouting_SkipsEmptyBatchesAndRejectsWrongCardinality(string role)
    {
        var source = new SingleResultOperator();
        Routed(role, source, 0).ShouldBeEmpty();
        source.Calls.ShouldBe(0);
        Should.Throw<InvalidOperationException>(() => Routed(role, source, 2));
        source.Calls.ShouldBe(1);
    }

    [Theory]
    [InlineData("creation")]
    [InlineData("crossover")]
    [InlineData("mutation")]
    [InlineData("refinement")]
    public void PerItemRouting_ConcurrentChildrenPreserveSeededResultsAndDraws(string role)
    {
        var sequential = DrawingRouting(role, ExecutionConcurrency.Sequential());
        var concurrent = DrawingRouting(role, ExecutionConcurrency.Concurrent(4));
        concurrent.Results.ShouldBe(sequential.Results);
        concurrent.NextDraw.ShouldBe(sequential.NextDraw);
    }

    [Theory]
    [InlineData("creation")]
    [InlineData("crossover")]
    [InlineData("mutation")]
    [InlineData("refinement")]
    public void PerItemRouting_PreservesDegenerateWeightSemanticsAndDraws(string role)
    {
        var uniform = DrawingRouting(role, ExecutionConcurrency.Sequential(), [1, 1]);
        foreach (double[] weights in new double[][] { [double.NaN, double.NegativeInfinity], [double.PositiveInfinity, double.PositiveInfinity] })
        {
            var actual = DrawingRouting(role, ExecutionConcurrency.Sequential(), weights);
            actual.Results.ShouldBe(uniform.Results);
            actual.NextDraw.ShouldBe(uniform.NextDraw);
        }

        var certain = DrawingRouting(role, ExecutionConcurrency.Sequential(), [0, 1]);
        foreach (double[] weights in new double[][] { [-1, double.PositiveInfinity], [double.NaN, 2], [double.NegativeInfinity, 1] })
        {
            var actual = DrawingRouting(role, ExecutionConcurrency.Sequential(), weights);
            actual.Results.ShouldBe(certain.Results);
            actual.NextDraw.ShouldBe(certain.NextDraw);
        }
    }

    private static readonly IProblem<int, DummySearchSpace<int>> RoutingProblem = FuncProblem.Create((int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private static IReadOnlyList<int> Routed(string role, SingleResultOperator source, int count)
    {
        var scope = ResolutionScope.Create().For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();
        var space = DummySearchSpace<int>.Instance;
        var random = new SequenceRandomNumberGenerator();
        var inputs = Enumerable.Range(0, count).ToArray();
        return role switch
        {
            "creation" => scope.Resolve(new ChoosePerItem<Creation, int>([(IOperator<Creation, int>)source])).Create(count, random, space, RoutingProblem),
            "crossover" => scope.Resolve(new ChoosePerItem<Crossover, int>([(IOperator<Crossover, int>)source])).Cross(inputs.Select(x => Parents.From(x, x)).ToArray(), random, space, RoutingProblem),
            "mutation" => scope.Resolve(new ChoosePerItem<Mutation, int>([(IOperator<Mutation, int>)source])).Mutate(inputs, random, space, RoutingProblem),
            "refinement" => scope.Resolve(new ChoosePerItem<Refinement, int>([(IOperator<Refinement, int>)source])).Refine(inputs, random, space, RoutingProblem),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }

    private static (IReadOnlyList<int> Results, int NextDraw) DrawingRouting(string role, ExecutionConcurrency concurrency, IReadOnlyList<double>? suppliedWeights = null)
    {
        var scope = ResolutionScope.Create().For<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>();
        var space = DummySearchSpace<int>.Instance;
        var random = RandomNumberGenerator.Create(42);
        var inputs = Enumerable.Range(0, 32).ToArray();
        IReadOnlyList<double> weights = suppliedWeights ?? [1, 3];
        IReadOnlyList<int> result = role switch
        {
            "creation" => scope.Resolve(ChoosePerItem.Create([new DrawingCreator(100) { Concurrency = concurrency }, new DrawingCreator(200) { Concurrency = concurrency }], weights)).Create(32, random, space, RoutingProblem),
            "crossover" => scope.Resolve(ChoosePerItem.Create([new DrawingCrossover(100) { Concurrency = concurrency }, new DrawingCrossover(200) { Concurrency = concurrency }], weights)).Cross(inputs.Select(x => Parents.From(x, x)).ToArray(), random, space, RoutingProblem),
            "mutation" => scope.Resolve(ChoosePerItem.Create([new DrawingMutator(100) { Concurrency = concurrency }, new DrawingMutator(200) { Concurrency = concurrency }], weights)).Mutate(inputs, random, space, RoutingProblem),
            "refinement" => scope.Resolve(ChoosePerItem.Create([new DrawingRefiner(100) { Concurrency = concurrency }, new DrawingRefiner(200) { Concurrency = concurrency }], weights)).Refine(inputs, random, space, RoutingProblem),
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        return (result, random.NextInt());
    }

    private sealed record DrawingCreator(int Offset) : SingleCandidateCreator<int>
    {
        public override int CreateCandidate(IRandomNumberGenerator random) => Offset + random.NextInt(1000);
    }

    private sealed record DrawingCrossover(int Offset) : SingleCandidateCrossover<int>
    {
        public override int CrossParents(Parents<int> parents, IRandomNumberGenerator random) => parents.Parent1 + Offset + random.NextInt(1000);
    }

    private sealed record DrawingMutator(int Offset) : SingleCandidateMutator<int>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random) => parent + Offset + random.NextInt(1000);
    }

    private sealed record DrawingRefiner(int Offset) : SingleCandidateRefiner<int>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random) => candidate + Offset + random.NextInt(1000);
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
        public override TestAlgorithmState Transform(TestAlgorithmState currentState, TestAlgorithmState? previousState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            currentState with { Value = currentState.Value + Offset };
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

    private sealed record OffsetEvaluator(int Offset) : SingleCandidateEvaluator<int>
    {
        public override ObjectiveVector EvaluateCandidate(int candidate, IRandomNumberGenerator random) => new(candidate + Offset);
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
        public override bool IsTerminalState(TestAlgorithmState state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) => Result;
    }

    private sealed record ThrowingTerminator
        : StatelessTerminator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
    {
        public override bool IsTerminalState(TestAlgorithmState state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem) =>
            throw new InvalidOperationException("This terminator should not be reached.");
    }

    private static IReadOnlyList<EvaluatedCandidate<int>> CreateEvaluatedCandidates(params int[] candidates) =>
        candidates.Select(candidate => EvaluatedCandidate.From(candidate, new ObjectiveVector(candidate))).ToArray();

    private sealed record TestAlgorithmState : SearchState
    {
        public required int Value { get; init; }
    }

    private sealed class CountingExecutionMutator : IOperator<Mutation, int>
    {
        public int ExecutionsCreated { get; private set; }

        // Every build in one owning state shares the call count.
        IExecutor IOperator<Mutation, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => new Executor(scope.GetExecutionState(this, CreateExecutionState));

        private ExecutionState CreateExecutionState()
        {
            ExecutionsCreated++;
            return new ExecutionState();
        }

        private sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        private sealed class Executor(ExecutionState state) : IMutationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
        {
            public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
            {
                var calls = ++state.Calls;
                return parents.Select(parent => parent + calls).ToArray();
            }
        }
    }

    private delegate IReadOnlyList<int> MutationCallback(
        IReadOnlyList<int> parents,
        IRandomNumberGenerator random,
        DummySearchSpace<int> searchSpace,
        IProblem<int, DummySearchSpace<int>> problem);

    private sealed record CallbackExecutionMutator(MutationCallback Callback)
        : IOperator<Mutation, int>
    {
        IExecutor IOperator<Mutation, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => CreateBoundExecution();

        private IMutationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateBoundExecution() =>
            new Executor(Callback);

        private sealed class Executor(MutationCallback callback)
            : IMutationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
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

/// <summary>
/// Names the run's triple once for this file. Every mutator exercised here is authored over
/// <see cref="DummySearchSpace{T}"/>, so the triple is the same at every call site and repeating it per resolution
/// would only obscure what each test is actually asserting.
/// </summary>

/// <summary>
/// Names the run's triple once for this file. Every operator exercised here is authored over
/// <see cref="DummySearchSpace{T}"/>, so the triple is the same at every call site and repeating it per resolution
/// would only obscure what each test is actually asserting.
/// </summary>
file static class OperatorResolution
{
    extension(ResolutionScope scope)
    {
        public ICreationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCreator(IOperator<Creation, int> creator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(creator);

        public ICrossoverExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveCrossover(IOperator<Crossover, int> crossover) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(crossover);

        public IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveMutator(IOperator<Mutation, int> mutator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator);

        public IEvaluationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveEvaluator(IOperator<Evaluation, int> evaluator) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(evaluator);

        public ISelectionExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveSelector(IOperator<Selection, int> selector) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(selector);

        public IReplacementExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> ResolveReplacer(IOperator<Replacement, int> replacer) =>
            scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(replacer);
    }

    extension(IOperator<Creation, int> creator)
    {
        public ICreationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCreatorExecution() =>
            ResolutionScope.Create().ResolveCreator(creator);
    }

    extension(IOperator<Crossover, int> crossover)
    {
        public ICrossoverExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateCrossoverExecution() =>
            ResolutionScope.Create().ResolveCrossover(crossover);
    }

    extension(IOperator<Mutation, int> mutator)
    {
        public IMutationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> CreateMutatorExecution() =>
            ResolutionScope.Create().ResolveMutator(mutator);
    }
}
