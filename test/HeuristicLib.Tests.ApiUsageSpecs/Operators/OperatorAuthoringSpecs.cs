using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Instrumentation;
using HEAL.HeuristicLib.Objectives;
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
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Operators;

public class OperatorAuthoringSpecs
{
    [Fact]
    public void StatelessMutator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new PullTowardZeroMutator();
        var execution = ResolveMutator(mutator);

        var offspring = execution.Mutate(
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
        SingleCandidateMutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> mutator = new PullTowardZeroMutator();

        var offspring = mutator.MutateCandidate(
            RealVector.Repeat(1.0, 3),
            RandomNumberGenerator.Create(12),
            problem.SearchSpace,
            problem);

        offspring.ShouldBe(RealVector.Repeat(0.5, 3));
    }

    [Fact]
    public void StatefulMutator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new CountingStatefulMutator();
        var firstExecution = ResolveMutator(mutator);
        var secondExecution = ResolveMutator(mutator);
        var parent = RealVector.Repeat(0.0, 3);

        var first = firstExecution.Mutate([parent], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecution.Mutate([parent], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecution.Mutate([parent], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitMutator_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new ApplyTwiceMutator(new PullTowardZeroMutator());
        var execution = ResolveMutator(mutator);

        var offspring = execution.Mutate(
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
        var pipeline = PipelineMutator.Create(
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
        IMutator<RealVector> childMutator = new PullTowardZeroMutator();
        var counting = CountingMutator.Create(childMutator, new CountAccumulator(), OperatorCountMetric.Calls);
        counting.ChildMutator.ShouldBeSameAs(childMutator);
    }

    [Fact]
    public void StatelessRefiner_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new HalveRefiner();
        var execution = ResolveRefiner(refiner);

        var refined = execution.Refine(
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
        SingleCandidateRefiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> refiner = new HalveRefiner();

        var refined = refiner.RefineCandidate(
            RealVector.Repeat(1.0, 3),
            RandomNumberGenerator.Create(12),
            problem.SearchSpace,
            problem);

        refined.ShouldBe(RealVector.Repeat(0.5, 3));
    }

    [Fact]
    public void StatefulRefiner_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new CountingStatefulRefiner();
        var firstExecution = ResolveRefiner(refiner);
        var secondExecution = ResolveRefiner(refiner);
        var candidate = RealVector.Repeat(0.0, 3);

        var first = firstExecution.Refine([candidate], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecution.Refine([candidate], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecution.Refine([candidate], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitRefiner_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new ApplyTwiceRefiner(new HalveRefiner());
        var execution = ResolveRefiner(refiner);

        var refined = execution.Refine(
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
        var pipeline = PipelineRefiner.Create(
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
        IRefiner<RealVector> childRefiner = new HalveRefiner();
        var counting = CountingRefiner.Create(childRefiner, new CountAccumulator(), OperatorCountMetric.Calls);
        var iterated = IteratedRefiner.Create(childRefiner, 2);
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
        var evaluator = new ProblemEvaluator<RealVector>()
            .AppliedAfterRefinement(new HalveRefiner());

        var objectiveVectors = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator).Evaluate(
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
        var algorithm = new HillClimber<RealVector>
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
    public void StatefulCreator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var creator = new CountingStatefulCreator();
        var firstExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(creator);
        var secondExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(creator);

        var first = firstExecution.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecution.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecution.Create(1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void StatefulCrossover_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var crossover = new CountingStatefulCrossover();
        var firstExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(crossover);
        var secondExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(crossover);
        var parents = Parents.From(RealVector.Repeat(0.0, 3), RealVector.Repeat(10.0, 3));

        var first = firstExecution.Cross([parents], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecution.Cross([parents], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecution.Cross([parents], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitCrossover_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var crossover = new ForwardingCrossover(SelectFirstParentCrossover.For(problem));
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(crossover);
        var parents = Parents.From(RealVector.Repeat(1.0, 3), RealVector.Repeat(2.0, 3));

        var offspring = execution.Cross([parents], RandomNumberGenerator.Create(4), problem.SearchSpace, problem);

        offspring.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void StatelessEvaluator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new FirstValueEvaluator();
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator);

        var candidate = RealVector.Repeat(2.0, 3);
        var evaluatedCandidates = execution.Evaluate([candidate], RandomNumberGenerator.Create(5), problem.SearchSpace, problem);

        evaluatedCandidates.ShouldBe([new ObjectiveVector(2.0)]);
    }

    [Fact]
    public void SingleCandidateEvaluator_CanEvaluateOneCandidateDirectly()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        SingleCandidateEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> evaluator = new ConcurrentFirstValueEvaluator();

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
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator);

        var objectives = execution.Evaluate(
            [RealVector.Repeat(2.0, 3), RealVector.Repeat(4.0, 3)],
            RandomNumberGenerator.Create(5),
            problem.SearchSpace,
            problem);

        objectives.ShouldBe([new ObjectiveVector(2.0), new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void StatefulEvaluator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new CountingStatefulEvaluator();
        var firstExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator);
        var secondExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator);
        var candidates = new[] { RealVector.Repeat(0.0, 3) };

        var first = firstExecution.Evaluate(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecution.Evaluate(candidates, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecution.Evaluate(candidates, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([new ObjectiveVector(1.0)]);
        second.ShouldBe([new ObjectiveVector(2.0)]);
        independent.ShouldBe([new ObjectiveVector(1.0)]);
    }

    [Fact]
    public void ExplicitEvaluator_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new ForwardingEvaluator(new FirstValueEvaluator());
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator);

        var candidate = RealVector.Repeat(4.0, 3);
        var evaluatedCandidates = execution.Evaluate([candidate], RandomNumberGenerator.Create(6), problem.SearchSpace, problem);

        evaluatedCandidates.ShouldBe([new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void EvaluatorTopologies_ExposeAndResolveConfiguredChildren()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new FirstValueEvaluator();
        var wrapping = new ForwardingWrappingEvaluator(child);
        var multi = new FirstMultiEvaluator([child, new FirstValueEvaluator()]);

        var wrapped = wrapping.CreateExecutionInstance<BoundedRealVectorSearchSpace, TestFunctionProblem>(ResolutionScope.Create())
            .Evaluate([RealVector.Repeat(3.0, 3)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var first = multi.CreateExecutionInstance<BoundedRealVectorSearchSpace, TestFunctionProblem>(ResolutionScope.Create())
            .Evaluate([RealVector.Repeat(4.0, 3)], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        wrapping.ChildEvaluator.ShouldBeSameAs(child);
        multi.ChildEvaluators[0].ShouldBeSameAs(child);
        wrapped.ShouldBe([new ObjectiveVector(3.0)]);
        first.ShouldBe([new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void EvaluatorCompositionFactories_InferRoleTypes()
    {
        IEvaluator<RealVector> child = new FirstValueEvaluator();
        var counting = CountingEvaluator.Create(child, new CountAccumulator(), OperatorCountMetric.Calls);
        var duration = DurationMeasuringEvaluator.Create(child, new DurationAccumulator());
        var repeating = child.AsRepeated(3, ObjectiveVectorAggregation.Median);
        var caching = child.Cached(new FirstCoordinateCacheKeySelector());
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
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = execution.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(7), problem.SearchSpace, problem);

        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void StatefulSelector_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new RotatingStatefulSelector();
        var firstExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var secondExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var population = CreatePopulation(1.0, 2.0);

        var first = firstExecution.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecution.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecution.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([population[0]]);
        second.ShouldBe([population[1]]);
        independent.ShouldBe([population[0]]);
    }

    [Fact]
    public void ExplicitSelector_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new ForwardingSelector(new FirstSelector());
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = execution.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(8), problem.SearchSpace, problem);

        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void WrappingSelector_AuthoringExample_ResolvesItsChildOnce()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var childSelector = new FirstSelector();
        var selector = new DoublingWrappingSelector(childSelector);
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = execution.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(11), problem.SearchSpace, problem);

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
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = execution.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(12), problem.SearchSpace, problem);

        selector.ChildSelectors.ShouldBe([first, last]);
        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void SelectorCompositionFactories_InferRoleTypes()
    {
        ISelector<RealVector> childSelector = new FirstSelector();
        var counting = childSelector.CountCalls(new CountAccumulator());
        var chooseOne = ChooseOneSelector.Create(childSelector, new LastSelector());
        counting.ChildSelector.ShouldBeSameAs(childSelector);
        chooseOne.ChildSelectors[0].ShouldBeSameAs(childSelector);
    }

    [Fact]
    public void StatelessReplacer_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new OffspringReplacer();
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var replaced = execution.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(9), problem.SearchSpace, problem);

        replaced.ShouldBe(offspring);
    }

    [Fact]
    public void StatefulReplacer_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new AlternatingStatefulReplacer();
        var firstExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer);
        var secondExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var first = firstExecution.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecution.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecution.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe(previous);
        second.ShouldBe(offspring);
        independent.ShouldBe(previous);
    }

    [Fact]
    public void ExplicitReplacer_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new ForwardingReplacer(new OffspringReplacer());
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var replaced = execution.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(10), problem.SearchSpace, problem);

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

        var wrapped = wrapping.CreateExecutionInstance<BoundedRealVectorSearchSpace, TestFunctionProblem>(ResolutionScope.Create())
            .Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var first = multi.CreateExecutionInstance<BoundedRealVectorSearchSpace, TestFunctionProblem>(ResolutionScope.Create())
            .Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        wrapping.ChildReplacer.ShouldBeSameAs(child);
        multi.ChildReplacers[0].ShouldBeSameAs(child);
        wrapped.ShouldBe(offspring);
        first.ShouldBe(offspring);
    }

    [Fact]
    public void ReplacerCompositionFactories_InferRoleTypes()
    {
        IReplacer<RealVector> child = new OffspringReplacer();
        var counting = CountingReplacer.Create(child, new CountAccumulator(), OperatorCountMetric.Calls);
        var duration = DurationMeasuringReplacer.Create(child, new DurationAccumulator());
        counting.ChildReplacer.ShouldBeSameAs(child);
        duration.ChildReplacer.ShouldBeSameAs(child);
    }

    [Fact]
    public void StatelessInterceptor_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new IncrementingInterceptor());

        var transformed = execution.Transform(new CounterSearchState(1), previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void StatefulInterceptor_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var interceptor = new CountingStatefulInterceptor();
        var firstExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(interceptor);
        var secondExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(interceptor);

        var first = firstExecution.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecution.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecution.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.Value.ShouldBe(1);
        second.Value.ShouldBe(2);
        independent.Value.ShouldBe(1);
    }

    [Fact]
    public void ExplicitInterceptor_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new ForwardingInterceptor(new IncrementingInterceptor()));

        var transformed = execution.Transform(new CounterSearchState(1), previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void InterceptorTopologyBases_ExposeMatchingConfigurationAndExecutionShapes()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new IncrementingInterceptor();
        var wrapping = new ForwardingWrappingInterceptor(child);
        var multi = new FirstMultiInterceptor([child]);

        wrapping.ChildInterceptor.ShouldBeSameAs(child);
        multi.ChildInterceptors.ShouldBe([child]);
        wrapping.CreateExecutionInstance<BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(ResolutionScope.Create()).Transform(new CounterSearchState(1), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(2);
        multi.CreateExecutionInstance<BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(ResolutionScope.Create()).Transform(new CounterSearchState(1), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(2);
    }

    [Fact]
    public void Interceptor_ReducedArities_KeepOnlyConsumedContextOnAuthoringSurface()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        IInterceptor<RealVector> searchSpaceOnly = new SearchSpaceInterceptor();
        IInterceptor<RealVector> candidateOnly = new CandidateInterceptor();
        IInterceptor<RealVector> statefulSearchSpaceOnly = new StatefulSearchSpaceInterceptor();
        IInterceptor<RealVector> statefulCandidateOnly = new StatefulCandidateInterceptor();

        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(searchSpaceOnly).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(1);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(candidateOnly).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(1);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulSearchSpaceOnly).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(1);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulCandidateOnly).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(1);
    }

    [Fact]
    public void StatelessTerminator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new ValueTerminator(2));

        execution.IsTerminalState(new CounterSearchState(1), problem.SearchSpace, problem).ShouldBeFalse();
        execution.IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void StatefulTerminator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var terminator = new CountingStatefulTerminator();
        var firstExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(terminator);
        var secondExecution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(terminator);
        var state = new CounterSearchState(0);

        firstExecution.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeFalse();
        firstExecution.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeTrue();
        secondExecution.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeFalse();
    }

    [Fact]
    public void ExplicitTerminator_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new ForwardingTerminator(new ValueTerminator(2)));

        execution.IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void TerminatorTopologyBases_ExposeMatchingConfigurationAndExecutionShapes()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new ValueTerminator(2);
        var wrapping = new ForwardingWrappingTerminator(child);
        var multi = new FirstMultiTerminator([child]);

        wrapping.ChildTerminator.ShouldBeSameAs(child);
        multi.ChildTerminators.ShouldBe([child]);
        wrapping.CreateExecutionInstance<BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(ResolutionScope.Create()).IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
        multi.CreateExecutionInstance<BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(ResolutionScope.Create()).IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void Terminator_ReducedArities_IncludeStateAgnosticForm()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        ITerminator<RealVector> searchSpaceOnly = new SearchSpaceTerminator();
        ITerminator<RealVector> stateOnly = new StateTerminator();
        ITerminator<RealVector> stateAgnostic = new StateAgnosticTerminator();
        ITerminator<RealVector> statefulSearchSpaceOnly = new StatefulSearchSpaceTerminator();
        ITerminator<RealVector> statefulStateOnly = new StatefulStateTerminator();
        ITerminator<RealVector> statefulStateAgnostic = new StatefulStateAgnosticTerminator();

        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(searchSpaceOnly).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(stateOnly).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(stateAgnostic).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulSearchSpaceOnly).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulStateOnly).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulStateAgnostic).IsTerminalState(new CounterSearchState(0), problem.SearchSpace, problem).ShouldBeFalse();
    }

    [Fact]
    public async Task MultiMutator_AuthoringExample_CanBeUsedInsideHillClimber()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var algorithm = new HillClimber<RealVector>
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
        }.TerminatedAfterIterations(1);

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

    private static IMutatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> ResolveMutator(IMutator<RealVector> mutator)
    {
        return ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(mutator);
    }

    private static IRefinerExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> ResolveRefiner(IRefiner<RealVector> refiner)
    {
        return ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(refiner);
    }

    /// <summary>
    /// A wrapper that reads its problem. The composite base stays agnostic in the search space and problem, so this
    /// binds them on its own type and reconciles with the run's inside the override.
    /// </summary>
    private sealed record PrefixingWrappingCreator(ICreator<RealVector> Child)
        : WrappingCreator<RealVector>(Child)
    {
        protected override ICreatorExecution<RealVector, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ICreatorExecution<RealVector, TRunSearchSpace, TRunProblem> childCreator)
        {
            if (childCreator is not ICreatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> boundChild
                || new Execution(boundChild) is not ICreatorExecution<RealVector, TRunSearchSpace, TRunProblem> typed)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} reads {typeof(TestFunctionProblem).Name} and cannot run over {typeof(TRunProblem).Name}.");
            }

            return typed;
        }

        private sealed class Execution(ICreatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> childCreator)
            : WrappingCreatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(childCreator)
        {
            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var candidates = ChildCreator.Create(count, random, searchSpace, problem).ToArray();
                candidates[0] = RealVector.Repeat(0.0, problem.TestFunction.Dimension);
                return candidates;
            }
        }
    }

    private sealed record ConstantOriginCreator
        : SingleCandidateCreator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector CreateCandidate(
          IRandomNumberGenerator random,
          BoundedRealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return RealVector.Repeat(1.0, problem.TestFunction.Dimension);
        }
    }

    private sealed record CountingStatefulCreator : StatefulCreator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CountingStatefulCreator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<RealVector> Create(int count, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return Enumerable.Repeat(RealVector.Repeat(state.Calls, problem.TestFunction.Dimension), count).ToArray();
        }
    }

    private sealed record CountingStatefulCrossover : StatefulCrossover<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CountingStatefulCrossover.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return parents.Select(parent => new RealVector(parent.Parent1.Select(value => value + state.Calls))).ToArray();
        }
    }

    private sealed record FirstValueEvaluator : StatelessEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            candidates.Select(candidate => new ObjectiveVector(candidate[0])).ToArray();
    }

    private sealed record ConcurrentFirstValueEvaluator : SingleCandidateEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override ObjectiveVector EvaluateCandidate(RealVector candidate, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            new(candidate[0]);
    }

    private sealed record FirstCoordinateCacheKeySelector : ICacheKeySelector<RealVector, double>
    {
        public double SelectKey(RealVector candidate) => candidate[0];
    }

    private sealed record CountingStatefulEvaluator : StatefulEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CountingStatefulEvaluator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return candidates.Select(candidate => new ObjectiveVector(state.Calls)).ToArray();
        }
    }

    private sealed record ForwardingEvaluator(IEvaluator<RealVector> Inner)
        : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override EvaluatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ResolutionScope scope) =>
            new Execution(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

        private sealed class Execution(IEvaluatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
            : EvaluatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Evaluate(candidates, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingEvaluator
        : WrappingEvaluator<RealVector>
    {
        public ForwardingWrappingEvaluator(IEvaluator<RealVector> childEvaluator)
            : base(childEvaluator)
        {
        }

        protected override IEvaluatorExecution<RealVector, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorExecution<RealVector, TRunSearchSpace, TRunProblem> childEvaluator) =>
            new Execution<TRunSearchSpace, TRunProblem>(childEvaluator);

        private sealed class Execution<TSearchSpace, TProblem>(IEvaluatorExecution<RealVector, TSearchSpace, TProblem> childEvaluator)
            : WrappingEvaluatorExecution<RealVector, TSearchSpace, TProblem>(childEvaluator)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiEvaluator
        : MultiEvaluator<RealVector>
    {
        public FirstMultiEvaluator(IReadOnlyList<IEvaluator<RealVector>> childEvaluators)
            : base(childEvaluators)
        {
        }

        protected override IEvaluatorExecution<RealVector, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IEvaluatorExecution<RealVector, TRunSearchSpace, TRunProblem>> childEvaluators) =>
            new Execution<TRunSearchSpace, TRunProblem>(childEvaluators);

        private sealed class Execution<TSearchSpace, TProblem>(ImmutableArray<IEvaluatorExecution<RealVector, TSearchSpace, TProblem>> childEvaluators)
            : MultiEvaluatorExecution<RealVector, TSearchSpace, TProblem>(childEvaluators)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildEvaluators[0].Evaluate(candidates, random, searchSpace, problem);
        }
    }

    private sealed record FirstSelector : StatelessSelector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            population.Take(count).ToArray();
    }

    private sealed record RotatingStatefulSelector : StatefulSelector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, RotatingStatefulSelector.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            var start = state.Calls++ % population.Count;
            return Enumerable.Range(0, count).Select(index => population[(start + index) % population.Count]).ToArray();
        }
    }

    private sealed record ForwardingSelector(ISelector<RealVector> Inner)
        : Selector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override SelectorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ResolutionScope scope) =>
            new Execution(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

        private sealed class Execution(ISelectorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
            : SelectorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Select(population, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record LastSelector : StatelessSelector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            population.TakeLast(count).ToArray();
    }

    private sealed record DoublingWrappingSelector
        : WrappingSelector<RealVector>
    {
        public DoublingWrappingSelector(ISelector<RealVector> childSelector)
            : base(childSelector)
        {
        }

        protected override ISelectorExecution<RealVector, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(ISelectorExecution<RealVector, TRunSearchSpace, TRunProblem> childSelector) =>
            new Execution<TRunSearchSpace, TRunProblem>(childSelector);

        private sealed class Execution<TSearchSpace, TProblem>(ISelectorExecution<RealVector, TSearchSpace, TProblem> childSelector)
            : WrappingSelectorExecution<RealVector, TSearchSpace, TProblem>(childSelector)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var selected = ChildSelector.Select(population, objective, count, random, searchSpace, problem);
                return [.. selected, .. selected];
            }
        }
    }

    private sealed record PreferFirstMultiSelector
        : MultiSelector<RealVector>
    {
        public PreferFirstMultiSelector(ImmutableArray<ISelector<RealVector>> childSelectors)
            : base(childSelectors)
        {
        }

        protected override ISelectorExecution<RealVector, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ISelectorExecution<RealVector, TRunSearchSpace, TRunProblem>> childSelectors) =>
            new Execution<TRunSearchSpace, TRunProblem>(childSelectors);

        private sealed class Execution<TSearchSpace, TProblem>(ImmutableArray<ISelectorExecution<RealVector, TSearchSpace, TProblem>> childSelectors)
            : MultiSelectorExecution<RealVector, TSearchSpace, TProblem>(childSelectors)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildSelectors[0].Select(population, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record OffspringReplacer : StatelessReplacer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            offspringPopulation.Take(count).ToArray();
    }

    private sealed record AlternatingStatefulReplacer : StatefulReplacer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, AlternatingStatefulReplacer.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            state.Calls++ % 2 == 0 ? previousPopulation.Take(count).ToArray() : offspringPopulation.Take(count).ToArray();
    }

    private sealed record ForwardingReplacer(IReplacer<RealVector> Inner)
        : Replacer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override ReplacerExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ResolutionScope scope) =>
            new Execution(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

        private sealed class Execution(IReplacerExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
            : ReplacerExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingReplacer
        : WrappingReplacer<RealVector>
    {
        public ForwardingWrappingReplacer(IReplacer<RealVector> childReplacer)
            : base(childReplacer)
        {
        }

        protected override IReplacerExecution<RealVector, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IReplacerExecution<RealVector, TRunSearchSpace, TRunProblem> childReplacer) =>
            new Execution<TRunSearchSpace, TRunProblem>(childReplacer);

        private sealed class Execution<TSearchSpace, TProblem>(IReplacerExecution<RealVector, TSearchSpace, TProblem> childReplacer)
            : WrappingReplacerExecution<RealVector, TSearchSpace, TProblem>(childReplacer)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiReplacer
        : MultiReplacer<RealVector>
    {
        public FirstMultiReplacer(IReadOnlyList<IReplacer<RealVector>> childReplacers)
            : base(childReplacers)
        {
        }

        protected override IReplacerExecution<RealVector, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IReplacerExecution<RealVector, TRunSearchSpace, TRunProblem>> childReplacers) =>
            new Execution<TRunSearchSpace, TRunProblem>(childReplacers);

        private sealed class Execution<TSearchSpace, TProblem>(ImmutableArray<IReplacerExecution<RealVector, TSearchSpace, TProblem>> childReplacers)
            : MultiReplacerExecution<RealVector, TSearchSpace, TProblem>(childReplacers)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildReplacers[0].Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record CounterSearchState(int Value) : SearchState;

    private sealed record IncrementingInterceptor : StatelessInterceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record SearchSpaceInterceptor : StatelessInterceptor<RealVector, BoundedRealVectorSearchSpace, CounterSearchState>
    {
        public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record CandidateInterceptor : StatelessInterceptor<RealVector, CounterSearchState>
    {
        public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record StatefulSearchSpaceInterceptor : StatefulInterceptor<RealVector, BoundedRealVectorSearchSpace, CounterSearchState, StatefulSearchSpaceInterceptor.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record StatefulCandidateInterceptor : StatefulInterceptor<RealVector, CounterSearchState, StatefulCandidateInterceptor.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, ExecutionState state, IRandomNumberGenerator random) =>
            currentState with { Value = currentState.Value + 1 };
    }

    private sealed record CountingStatefulInterceptor : StatefulInterceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState, CountingStatefulInterceptor.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return currentState with { Value = currentState.Value + state.Calls };
        }
    }

    private sealed record ForwardingInterceptor(IInterceptor<RealVector> Inner)
        : Interceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override InterceptorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutionInstance(ResolutionScope scope) =>
            new Execution(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(Inner));

        private sealed class Execution(IInterceptorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> inner)
            : InterceptorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Transform(currentState, previousState, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingInterceptor
        : WrappingInterceptor<RealVector>
    {
        public ForwardingWrappingInterceptor(IInterceptor<RealVector> childInterceptor)
            : base(childInterceptor)
        {
        }

        protected override IInterceptorExecution<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(IInterceptorExecution<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> childInterceptor) =>
            new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptor);

        private sealed class Execution<TSearchSpace, TProblem, TSearchState>(IInterceptorExecution<RealVector, TSearchSpace, TProblem, TSearchState> childInterceptor)
            : WrappingInterceptorExecution<RealVector, TSearchSpace, TProblem, TSearchState>(childInterceptor)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiInterceptor
        : MultiInterceptor<RealVector>
    {
        public FirstMultiInterceptor(IReadOnlyList<IInterceptor<RealVector>> childInterceptors)
            : base(childInterceptors)
        {
        }

        protected override IInterceptorExecution<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> CombineExecutionInstances<TRunSearchSpace, TRunProblem, TRunSearchState>(ImmutableArray<IInterceptorExecution<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState>> childInterceptors) =>
            new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptors);

        private sealed class Execution<TSearchSpace, TProblem, TSearchState>(ImmutableArray<IInterceptorExecution<RealVector, TSearchSpace, TProblem, TSearchState>> childInterceptors)
            : MultiInterceptorExecution<RealVector, TSearchSpace, TProblem, TSearchState>(childInterceptors)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildInterceptors[0].Transform(currentState, previousState, random, searchSpace, problem);
        }
    }

    private sealed record ValueTerminator(int MaximumValue) : StatelessTerminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => state.Value >= MaximumValue;
    }

    private sealed record SearchSpaceTerminator : StatelessTerminator<RealVector, BoundedRealVectorSearchSpace, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state, BoundedRealVectorSearchSpace searchSpace) => false;
    }

    private sealed record StateTerminator : StatelessTerminator<RealVector, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state) => false;
    }

    private sealed record StateAgnosticTerminator : StatelessTerminator<RealVector>
    {
        public override bool IsTerminalState() => false;
    }

    private sealed record StatefulSearchSpaceTerminator : StatefulTerminator<RealVector, BoundedRealVectorSearchSpace, CounterSearchState, StatefulSearchSpaceTerminator.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override bool IsTerminalState(CounterSearchState searchState, ExecutionState state, BoundedRealVectorSearchSpace searchSpace) => false;
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

    private sealed record CountingStatefulTerminator : StatefulTerminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState, CountingStatefulTerminator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override bool IsTerminalState(CounterSearchState searchState, ExecutionState state, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => ++state.Calls >= 2;
    }

    private sealed record ForwardingTerminator(ITerminator<RealVector> Inner)
        : Terminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override TerminatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutionInstance(ResolutionScope scope) =>
            new Execution(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(Inner));

        private sealed class Execution(ITerminatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> inner)
            : TerminatorExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public override bool IsTerminalState(CounterSearchState state, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => inner.IsTerminalState(state, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingTerminator
        : WrappingTerminator<RealVector>
    {
        public ForwardingWrappingTerminator(ITerminator<RealVector> childTerminator)
            : base(childTerminator)
        {
        }

        protected override ITerminatorExecution<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> WrapExecutionInstance<TRunSearchSpace, TRunProblem, TRunSearchState>(ITerminatorExecution<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> childTerminator) =>
            new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminator);

        private sealed class Execution<TSearchSpace, TProblem, TSearchState>(ITerminatorExecution<RealVector, TSearchSpace, TProblem, TSearchState> childTerminator)
            : WrappingTerminatorExecution<RealVector, TSearchSpace, TProblem, TSearchState>(childTerminator)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
                ChildTerminator.IsTerminalState(state, searchSpace, problem);
        }
    }

    private sealed record FirstMultiTerminator
        : MultiTerminator<RealVector>
    {
        public FirstMultiTerminator(IReadOnlyList<ITerminator<RealVector>> childTerminators)
            : base(childTerminators)
        {
        }

        protected override ITerminatorExecution<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> CombineExecutionInstances<TRunSearchSpace, TRunProblem, TRunSearchState>(ImmutableArray<ITerminatorExecution<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState>> childTerminators) =>
            new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childTerminators);

        private sealed class Execution<TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminatorExecution<RealVector, TSearchSpace, TProblem, TSearchState>> childTerminators)
            : MultiTerminatorExecution<RealVector, TSearchSpace, TProblem, TSearchState>(childTerminators)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
                ChildTerminators[0].IsTerminalState(state, searchSpace, problem);
        }
    }

    private sealed record ForwardingCrossover(ICrossover<RealVector> Inner)
        : Crossover<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override CrossoverExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ResolutionScope scope) =>
            new Execution(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

        private sealed class Execution(ICrossoverExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
            : CrossoverExecution<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Cross(parents, random, searchSpace, problem);
        }
    }

    private sealed record ConstantOneCreator
        : SingleCandidateCreator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector CreateCandidate(
          IRandomNumberGenerator random,
          BoundedRealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return RealVector.Repeat(1.0, problem.TestFunction.Dimension);
        }
    }

    private sealed record NoChangeMutator
        : SingleCandidateMutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector MutateCandidate(
          RealVector parent,
          IRandomNumberGenerator random,
          BoundedRealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return parent;
        }
    }

    private sealed record PullTowardZeroMutator
        : SingleCandidateMutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector MutateCandidate(
          RealVector parent,
          IRandomNumberGenerator random,
          BoundedRealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return new RealVector(parent.Select(x => x * 0.5));
        }
    }

    private sealed record CountingStatefulMutator : StatefulMutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CountingStatefulMutator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return parents.Select(parent => new RealVector(parent.Select(value => value + state.Calls))).ToArray();
        }
    }

    private sealed record ApplyTwiceMutator
        : WrappingMutator<RealVector>
    {
        public ApplyTwiceMutator(IMutator<RealVector> childMutator)
            : base(childMutator)
        {
        }

        protected override WrappingMutatorExecution<RealVector, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IMutatorExecution<RealVector, TRunSearchSpace, TRunProblem> childMutator) =>
            new Execution<TRunSearchSpace, TRunProblem>(childMutator);

        private sealed class Execution<TSearchSpace, TProblem>(IMutatorExecution<RealVector, TSearchSpace, TProblem> childMutator)
            : WrappingMutatorExecution<RealVector, TSearchSpace, TProblem>(childMutator)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var first = ChildMutator.Mutate(parents, random, searchSpace, problem);
                return ChildMutator.Mutate(first, random, searchSpace, problem);
            }
        }
    }

    private sealed record HalveRefiner
        : SingleCandidateRefiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector RefineCandidate(
          RealVector candidate,
          IRandomNumberGenerator random,
          BoundedRealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return new RealVector(candidate.Select(x => x * 0.5));
        }
    }

    private sealed record CountingStatefulRefiner : StatefulRefiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CountingStatefulRefiner.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return candidates.Select(candidate => new RealVector(candidate.Select(value => value + state.Calls))).ToArray();
        }
    }

    private sealed record ApplyTwiceRefiner
        : WrappingRefiner<RealVector>
    {
        public ApplyTwiceRefiner(IRefiner<RealVector> childRefiner)
            : base(childRefiner)
        {
        }

        protected override IRefinerExecution<RealVector, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IRefinerExecution<RealVector, TRunSearchSpace, TRunProblem> childRefiner) =>
            new Execution<TRunSearchSpace, TRunProblem>(childRefiner);

        private sealed class Execution<TSearchSpace, TProblem>(IRefinerExecution<RealVector, TSearchSpace, TProblem> childRefiner)
            : WrappingRefinerExecution<RealVector, TSearchSpace, TProblem>(childRefiner)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var first = ChildRefiner.Refine(candidates, random, searchSpace, problem);
                return ChildRefiner.Refine(first, random, searchSpace, problem);
            }
        }
    }

    private sealed record PushAwayFromZeroMutator
        : SingleCandidateMutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector MutateCandidate(RealVector parent, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            return new RealVector(parent.Select(x => x * 2.0));
        }
    }

    private sealed record PreferFirstMultiMutator
        : MultiMutator<RealVector>
    {
        public PreferFirstMultiMutator(ImmutableArray<IMutator<RealVector>> childMutators)
            : base(childMutators)
        {
        }

        protected override MultiMutatorExecution<RealVector, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IMutatorExecution<RealVector, TRunSearchSpace, TRunProblem>> childMutators) =>
            new Execution<TRunSearchSpace, TRunProblem>(childMutators);

        private sealed class Execution<TSearchSpace, TProblem>(ImmutableArray<IMutatorExecution<RealVector, TSearchSpace, TProblem>> childMutators)
            : MultiMutatorExecution<RealVector, TSearchSpace, TProblem>(childMutators)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                ChildMutators[0].Mutate(parents, random, searchSpace, problem);
        }
    }
}
