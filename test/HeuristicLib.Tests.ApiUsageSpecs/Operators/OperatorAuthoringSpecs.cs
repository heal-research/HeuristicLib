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
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
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
    public void StatelessRefiner_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new HalveRefiner();
        var instance = ResolveRefiner(refiner);

        var refined = instance.Refine(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(12),
            problem.SearchSpace,
            problem);

        refined.ShouldBe([RealVector.Repeat(0.5, 3)]);
    }

    [Fact]
    public void SingleCandidateRefiner_CanRefineOneCandidateDirectly()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        SingleCandidateRefiner<RealVector, RealVectorSearchSpace, TestFunctionProblem> refiner = new HalveRefiner();

        var refined = refiner.RefineCandidate(
            RealVector.Repeat(1.0, 3),
            RandomNumberGenerator.Create(12),
            problem.SearchSpace,
            problem);

        refined.ShouldBe(RealVector.Repeat(0.5, 3));
    }

    [Fact]
    public void StatefulRefiner_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new CountingStatefulRefiner();
        var firstInstance = ResolveRefiner(refiner);
        var secondInstance = ResolveRefiner(refiner);
        var candidate = RealVector.Repeat(0.0, 3);

        var first = firstInstance.Refine([candidate], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Refine([candidate], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Refine([candidate], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitRefiner_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new ApplyTwiceRefiner(new HalveRefiner());
        var instance = ResolveRefiner(refiner);

        var refined = instance.Refine(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(34),
            problem.SearchSpace,
            problem);

        refined.ShouldBe([RealVector.Repeat(0.25, 3)]);
    }

    [Fact]
    public void TopologyRefiners_AcceptProblemSpecificChildren()
    {
        // Refinement is composed through operator topologies rather than through repeated lifecycle placement, so an
        // ordered pipeline such as repair, simplification and constant optimization is an ordinary configuration.
        var problem = CreateRastriginProblem(dimension: 3);
        var pipeline = PipelineRefiner.Create<RealVector, RealVectorSearchSpace, TestFunctionProblem>(
            new HalveRefiner(),
            NoChangeRefiner<RealVector>.Instance);

        var refined = ResolveRefiner(pipeline).Refine(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(34),
            problem.SearchSpace,
            problem);

        refined.ShouldBe([RealVector.Repeat(0.5, 3)]);
    }

    [Fact]
    public void IteratedRefiner_RepeatsOneRefinerInsteadOfPlacingItTwice()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new HalveRefiner().AsIterated(3);

        var refined = ResolveRefiner(refiner).Refine(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(34),
            problem.SearchSpace,
            problem);

        refined.ShouldBe([RealVector.Repeat(0.125, 3)]);
    }

    [Fact]
    public void RefinerCompositionFactories_InferRoleTypes()
    {
        IRefiner<RealVector, RealVectorSearchSpace, TestFunctionProblem> childRefiner = new HalveRefiner();
        IRefinerObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem> observer =
            new ActionRefinerObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem>((_, _, _, _) => { });

        var observable = ObservableRefiner.Create(childRefiner, observer);
        var callbackObservable = ObservableRefiner.Create(childRefiner, _ => { });
        var counting = CountingRefiner.Create(childRefiner, new ObservationCounter(), OperatorCountMetric.Calls);
        var iterated = IteratedRefiner.Create(childRefiner, 2);

        observable.ChildRefiner.ShouldBeSameAs(childRefiner);
        observable.Observers.ShouldBe([observer]);
        callbackObservable.Observers.Count.ShouldBe(1);
        counting.ChildRefiner.ShouldBeSameAs(childRefiner);
        iterated.ChildRefiner.ShouldBeSameAs(childRefiner);
    }

    /// <summary>
    /// Refinement evaluation is Baldwinian: the refined candidate is measured but discarded, so the caller keeps the
    /// candidate it supplied. Configuring the same refiner as an algorithm's refiner makes it Lamarckian instead.
    /// </summary>
    [Fact]
    public void RefinementEvaluator_MeasuresARefinedCandidateWithoutReplacingIt()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var candidate = RealVector.Repeat(1.0, 3);
        var evaluator = new ProblemEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>()
            .WithRefinement(new HalveRefiner());

        var objectiveVectors = new ExecutionInstanceRegistry().Resolve(evaluator).Evaluate(
            [candidate],
            RandomNumberGenerator.Create(7),
            problem.SearchSpace,
            problem);

        objectiveVectors[0].ShouldBe(problem.Evaluate(RealVector.Repeat(0.5, 3), RandomNumberGenerator.Create(7)));
        candidate.ShouldBe(RealVector.Repeat(1.0, 3));
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

        var candidate = RealVector.Repeat(2.0, 3);
        var evaluatedCandidates = instance.Evaluate([candidate], RandomNumberGenerator.Create(5), problem.SearchSpace, problem);

        evaluatedCandidates.ShouldBe([new ObjectiveVector(2.0)]);
    }

    [Fact]
    public void SingleCandidateEvaluator_CanEvaluateOneCandidateDirectly()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        SingleCandidateEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> evaluator = new ConcurrentFirstValueEvaluator();

        var objective = evaluator.EvaluateCandidate(
            RealVector.Repeat(2.0, 3),
            RandomNumberGenerator.Create(5),
            problem.SearchSpace,
            problem);

        objective.ShouldBe(new ObjectiveVector(2.0));
    }

    [Fact]
    public void SingleCandidateEvaluator_UsesGeneralExecutionConcurrency()
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

        var candidate = RealVector.Repeat(4.0, 3);
        var evaluatedCandidates = instance.Evaluate([candidate], RandomNumberGenerator.Create(6), problem.SearchSpace, problem);

        evaluatedCandidates.ShouldBe([new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void EvaluatorTopologies_ExposeAndResolveConfiguredChildren()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new FirstValueEvaluator();
        var wrapping = new ForwardingWrappingEvaluator(child);
        var multi = new FirstMultiEvaluator([child, new FirstValueEvaluator()]);

        var wrapped = wrapping.CreateExecutionInstance(new ExecutionInstanceRegistry())
            .Evaluate([RealVector.Repeat(3.0, 3)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var first = multi.CreateExecutionInstance(new ExecutionInstanceRegistry())
            .Evaluate([RealVector.Repeat(4.0, 3)], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        wrapping.ChildEvaluator.ShouldBeSameAs(child);
        multi.ChildEvaluators[0].ShouldBeSameAs(child);
        wrapped.ShouldBe([new ObjectiveVector(3.0)]);
        first.ShouldBe([new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void EvaluatorCompositionFactories_InferRoleTypes()
    {
        IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> child = new FirstValueEvaluator();
        IEvaluatorObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem> observer =
            new ActionEvaluatorObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem>((_, _, _, _) => { });

        var observable = ObservableEvaluator.Create(child, observer);
        var callbackObservable = ObservableEvaluator.Create(child, (_, _) => { });
        var counting = CountingEvaluator.Create(child, new ObservationCounter(), OperatorCountMetric.Calls);
        var duration = DurationMeasuringEvaluator.Create(child, new ObservationDuration());
        var repeating = child.AsRepeated(3, ObjectiveVectorAggregation.Median);
        var caching = child.WithCache(new FirstCoordinateCacheKeySelector());

        observable.ChildEvaluator.ShouldBeSameAs(child);
        observable.Observers.ShouldBe([observer]);
        callbackObservable.Observers.Count.ShouldBe(1);
        counting.ChildEvaluator.ShouldBeSameAs(child);
        duration.ChildEvaluator.ShouldBeSameAs(child);
        repeating.ChildEvaluator.ShouldBeSameAs(child);
        repeating.Aggregator.ShouldBe(ObjectiveVectorAggregation.Median);
        caching.ChildEvaluator.ShouldBeSameAs(child);
        caching.KeySelector.ShouldBe(new FirstCoordinateCacheKeySelector());
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
    public void ReplacerTopologies_ExposeAndResolveConfiguredChildren()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new OffspringReplacer();
        var wrapping = new ForwardingWrappingReplacer(child);
        var multi = new FirstMultiReplacer([child, new OffspringReplacer()]);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var wrapped = wrapping.CreateExecutionInstance(new ExecutionInstanceRegistry())
            .Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var first = multi.CreateExecutionInstance(new ExecutionInstanceRegistry())
            .Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        wrapping.ChildReplacer.ShouldBeSameAs(child);
        multi.ChildReplacers[0].ShouldBeSameAs(child);
        wrapped.ShouldBe(offspring);
        first.ShouldBe(offspring);
    }

    [Fact]
    public void ReplacerCompositionFactories_InferRoleTypes()
    {
        IReplacer<RealVector, RealVectorSearchSpace, TestFunctionProblem> child = new OffspringReplacer();
        IReplacerObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem> observer =
            new ActionReplacerObserver<RealVector, RealVectorSearchSpace, TestFunctionProblem>((_, _, _, _, _, _) => { });

        var observable = ObservableReplacer.Create(child, observer);
        var callbackObservable = ObservableReplacer.Create(child, _ => { });
        var counting = CountingReplacer.Create(child, new ObservationCounter(), OperatorCountMetric.Calls);
        var duration = DurationMeasuringReplacer.Create(child, new ObservationDuration());

        observable.ChildReplacer.ShouldBeSameAs(child);
        observable.Observers.ShouldBe([observer]);
        callbackObservable.Observers.Count.ShouldBe(1);
        counting.ChildReplacer.ShouldBeSameAs(child);
        duration.ChildReplacer.ShouldBeSameAs(child);
    }

    [Fact]
    public void StatelessInterceptor_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(new IncrementingInterceptor());

        var transformed = instance.Transform(new CounterSearchState(1), previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void StatefulInterceptor_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var interceptor = new CountingStatefulInterceptor();
        var firstInstance = new ExecutionInstanceRegistry().Resolve(interceptor);
        var secondInstance = new ExecutionInstanceRegistry().Resolve(interceptor);

        var first = firstInstance.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.Value.ShouldBe(1);
        second.Value.ShouldBe(2);
        independent.Value.ShouldBe(1);
    }

    [Fact]
    public void ExplicitInterceptor_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = new ExecutionInstanceRegistry().Resolve(new ForwardingInterceptor(new IncrementingInterceptor()));

        var transformed = instance.Transform(new CounterSearchState(1), previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void InterceptorTopologyBases_ExposeMatchingConfigurationAndInstanceShapes()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new IncrementingInterceptor();
        var wrapping = new ForwardingWrappingInterceptor(child);
        var multi = new FirstMultiInterceptor([child]);

        wrapping.ChildInterceptor.ShouldBeSameAs(child);
        multi.ChildInterceptors.ShouldBe([child]);
        wrapping.CreateExecutionInstance(new ExecutionInstanceRegistry()).Transform(new CounterSearchState(1), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(2);
        multi.CreateExecutionInstance(new ExecutionInstanceRegistry()).Transform(new CounterSearchState(1), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(2);
    }

    [Fact]
    public void Interceptor_ReducedArities_KeepOnlyConsumedContextOnAuthoringSurface()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> searchSpaceOnly = new SearchSpaceInterceptor();
        IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> candidateOnly = new CandidateInterceptor();
        IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> statefulSearchSpaceOnly = new StatefulSearchSpaceInterceptor();
        IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> statefulCandidateOnly = new StatefulCandidateInterceptor();

        new ExecutionInstanceRegistry().Resolve(searchSpaceOnly).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(1);
        new ExecutionInstanceRegistry().Resolve(candidateOnly).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(1);
        new ExecutionInstanceRegistry().Resolve(statefulSearchSpaceOnly).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(1);
        new ExecutionInstanceRegistry().Resolve(statefulCandidateOnly).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(1);
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
    public void TerminatorTopologyBases_ExposeMatchingConfigurationAndInstanceShapes()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new ValueTerminator(2);
        var wrapping = new ForwardingWrappingTerminator(child);
        var multi = new FirstMultiTerminator([child]);

        wrapping.ChildTerminator.ShouldBeSameAs(child);
        multi.ChildTerminators.ShouldBe([child]);
        wrapping.CreateExecutionInstance(new ExecutionInstanceRegistry()).IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
        multi.CreateExecutionInstance(new ExecutionInstanceRegistry()).IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void Terminator_ReducedArities_IncludeStateAgnosticForm()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> searchSpaceOnly = new SearchSpaceTerminator();
        ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> stateOnly = new StateTerminator();
        ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> stateAgnostic = new StateAgnosticTerminator();
        ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> statefulSearchSpaceOnly = new StatefulSearchSpaceTerminator();
        ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> statefulStateOnly = new StatefulStateTerminator();
        ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> statefulStateAgnostic = new StatefulStateAgnosticTerminator();

        new ExecutionInstanceRegistry().Resolve(searchSpaceOnly).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        new ExecutionInstanceRegistry().Resolve(stateOnly).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        new ExecutionInstanceRegistry().Resolve(stateAgnostic).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        new ExecutionInstanceRegistry().Resolve(statefulSearchSpaceOnly).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        new ExecutionInstanceRegistry().Resolve(statefulStateOnly).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        new ExecutionInstanceRegistry().Resolve(statefulStateAgnostic).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
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

    private static IRefinerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> ResolveRefiner(IRefiner<RealVector, RealVectorSearchSpace, TestFunctionProblem> refiner)
    {
        return new ExecutionInstanceRegistry().Resolve(refiner);
    }

    private sealed record PrefixingWrappingCreator(ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Child)
      : WrappingCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>(Child)
    {
        protected override WrappingCreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childCreator) =>
            new Instance(childCreator);

        private sealed class Instance(ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childCreator)
            : WrappingCreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childCreator)
        {
            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var candidates = ChildCreator.Create(count, random, searchSpace, problem).ToArray();
                candidates[0] = RealVector.Repeat(0.0, problem.TestFunction.Dimension);
                return candidates;
            }
        }
    }

    private sealed record ConstantOriginCreator
      : SingleCandidateCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector CreateCandidate(
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

    private sealed record ConcurrentFirstValueEvaluator : SingleCandidateEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override ObjectiveVector EvaluateCandidate(RealVector candidate, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            new(candidate[0]);
    }

    private sealed record FirstCoordinateCacheKeySelector : ICacheKeySelector<RealVector, double>
    {
        public double SelectKey(RealVector candidate) => candidate[0];
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
            return candidates.Select(candidate => new ObjectiveVector(state.Calls)).ToArray();
        }
    }

    private sealed record ForwardingEvaluator(IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
        : Evaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override EvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(instanceRegistry.Resolve(Inner));

        private sealed class Instance(IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : EvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Evaluate(candidates, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingEvaluator
        : WrappingEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public ForwardingWrappingEvaluator(IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> childEvaluator)
            : base(childEvaluator)
        {
        }

        protected override WrappingEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childEvaluator) =>
            new Instance(childEvaluator);

        private sealed class Instance(IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childEvaluator)
            : WrappingEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childEvaluator)
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiEvaluator
        : MultiEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public FirstMultiEvaluator(IReadOnlyList<IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childEvaluators)
            : base(childEvaluators)
        {
        }

        protected override MultiEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ImmutableArray<IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childEvaluators) =>
            new Instance(childEvaluators);

        private sealed class Instance(ImmutableArray<IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childEvaluators)
            : MultiEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childEvaluators)
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildEvaluators[0].Evaluate(candidates, random, searchSpace, problem);
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

        protected override WrappingSelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ISelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childSelector) =>
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
        public override ReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(instanceRegistry.Resolve(Inner));

        private sealed class Instance(IReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : ReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingReplacer
        : WrappingReplacer<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public ForwardingWrappingReplacer(IReplacer<RealVector, RealVectorSearchSpace, TestFunctionProblem> childReplacer)
            : base(childReplacer)
        {
        }

        protected override WrappingReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(IReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childReplacer) =>
            new Instance(childReplacer);

        private sealed class Instance(IReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childReplacer)
            : WrappingReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childReplacer)
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiReplacer
        : MultiReplacer<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public FirstMultiReplacer(IReadOnlyList<IReplacer<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childReplacers)
            : base(childReplacers)
        {
        }

        protected override MultiReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ImmutableArray<IReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childReplacers) =>
            new Instance(childReplacers);

        private sealed class Instance(ImmutableArray<IReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> childReplacers)
            : MultiReplacerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childReplacers)
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildReplacers[0].Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record CounterSearchState(int Value) : SearchState;

    private sealed record IncrementingInterceptor : StatelessInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record SearchSpaceInterceptor : StatelessInterceptor<RealVector, RealVectorSearchSpace, CounterSearchState>
    {
        public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record CandidateInterceptor : StatelessInterceptor<RealVector, CounterSearchState>
    {
        public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record StatefulSearchSpaceInterceptor : StatefulInterceptor<RealVector, RealVectorSearchSpace, CounterSearchState, StatefulSearchSpaceInterceptor.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record StatefulCandidateInterceptor : StatefulInterceptor<RealVector, CounterSearchState, StatefulCandidateInterceptor.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, ExecutionState state, IRandomNumberGenerator random) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record CountingStatefulInterceptor : StatefulInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState, CountingStatefulInterceptor.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return currentState with { Value = currentState.Value + state.Calls };
        }
    }

    private sealed record ForwardingInterceptor(IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> Inner)
        : Interceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override InterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(instanceRegistry.Resolve(Inner));

        private sealed class Instance(IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> inner)
            : InterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Transform(currentState, previousState, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingInterceptor
        : WrappingInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public ForwardingWrappingInterceptor(IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> childInterceptor)
            : base(childInterceptor)
        {
        }

        protected override WrappingInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutionInstance(IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> childInterceptor) =>
            new Instance(childInterceptor);

        private sealed class Instance(IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> childInterceptor)
            : WrappingInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(childInterceptor)
        {
            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiInterceptor
        : MultiInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public FirstMultiInterceptor(IReadOnlyList<IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>> childInterceptors)
            : base(childInterceptors)
        {
        }

        protected override MultiInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutionInstance(ImmutableArray<IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>> childInterceptors) =>
            new Instance(childInterceptors);

        private sealed class Instance(ImmutableArray<IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>> childInterceptors)
            : MultiInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(childInterceptors)
        {
            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildInterceptors[0].Transform(currentState, previousState, random, searchSpace, problem);
        }
    }

    private sealed record ValueTerminator(int MaximumValue) : StatelessTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) => state.Value >= MaximumValue;
    }

    private sealed record SearchSpaceTerminator : StatelessTerminator<RealVector, RealVectorSearchSpace, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state, RealVectorSearchSpace searchSpace) => false;
    }

    private sealed record StateTerminator : StatelessTerminator<RealVector, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state) => false;
    }

    private sealed record StateAgnosticTerminator : StatelessTerminator<RealVector>
    {
        public override bool IsTerminalState() => false;
    }

    private sealed record StatefulSearchSpaceTerminator : StatefulTerminator<RealVector, RealVectorSearchSpace, CounterSearchState, StatefulSearchSpaceTerminator.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override bool IsTerminalState(CounterSearchState searchState, ExecutionState state, RealVectorSearchSpace searchSpace) => false;
    }

    private sealed record StatefulStateTerminator : StatefulTerminator<RealVector, CounterSearchState, StatefulStateTerminator.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override bool IsTerminalState(CounterSearchState searchState, ExecutionState state) => false;
    }

    private sealed record StatefulStateAgnosticTerminator : StatefulTerminator<RealVector, StatefulStateAgnosticTerminator.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override bool IsTerminalState(ExecutionState state) => false;
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
        public override TerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(instanceRegistry.Resolve(Inner));

        private sealed class Instance(ITerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> inner)
            : TerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public override bool IsTerminalState(CounterSearchState state, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) => inner.IsTerminalState(state, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingTerminator
        : WrappingTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public ForwardingWrappingTerminator(ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> childTerminator)
            : base(childTerminator)
        {
        }

        protected override WrappingTerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutionInstance(ITerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> childTerminator) =>
            new Instance(childTerminator);

        private sealed class Instance(ITerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> childTerminator)
            : WrappingTerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(childTerminator)
        {
            public override bool IsTerminalState(CounterSearchState state, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildTerminator.IsTerminalState(state, searchSpace, problem);
        }
    }

    private sealed record FirstMultiTerminator
        : MultiTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public FirstMultiTerminator(IReadOnlyList<ITerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>> childTerminators)
            : base(childTerminators)
        {
        }

        protected override MultiTerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutionInstance(ImmutableArray<ITerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>> childTerminators) =>
            new Instance(childTerminators);

        private sealed class Instance(ImmutableArray<ITerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>> childTerminators)
            : MultiTerminatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(childTerminators)
        {
            public override bool IsTerminalState(CounterSearchState state, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                ChildTerminators[0].IsTerminalState(state, searchSpace, problem);
        }
    }

    private sealed record ForwardingCrossover(ICrossover<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
        : Crossover<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override CrossoverInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(instanceRegistry.Resolve(Inner));

        private sealed class Instance(ICrossoverInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : CrossoverInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Cross(parents, random, searchSpace, problem);
        }
    }

    private sealed record ConstantOneCreator
      : SingleCandidateCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector CreateCandidate(
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

        protected override WrappingMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childMutator) =>
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

    private sealed record HalveRefiner
      : SingleCandidateRefiner<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector RefineCandidate(
          RealVector candidate,
          IRandomNumberGenerator random,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return new RealVector(candidate.Select(x => x * 0.5));
        }
    }

    private sealed record CountingStatefulRefiner : StatefulRefiner<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingStatefulRefiner.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return candidates.Select(candidate => new RealVector(candidate.Select(value => value + state.Calls))).ToArray();
        }
    }

    private sealed record ApplyTwiceRefiner
        : WrappingRefiner<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public ApplyTwiceRefiner(IRefiner<RealVector, RealVectorSearchSpace, TestFunctionProblem> childRefiner)
            : base(childRefiner)
        {
        }

        protected override WrappingRefinerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(IRefinerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childRefiner) =>
            new Instance(childRefiner);

        private sealed class Instance(IRefinerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> childRefiner)
            : WrappingRefinerInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(childRefiner)
        {
            public override IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var first = ChildRefiner.Refine(candidates, random, searchSpace, problem);
                return ChildRefiner.Refine(first, random, searchSpace, problem);
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
