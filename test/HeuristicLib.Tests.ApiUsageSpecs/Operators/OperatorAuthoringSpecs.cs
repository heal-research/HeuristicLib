using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
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
    public void AdaptiveMutator_ResolvesAdaptationAndInvokesTheObservedExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var adaptation = new ScaleAdaptation();
        var gaussian = new GaussianMutator(1.0, 3.0) { MutationStrengthAdaptation = adaptation };
        IOperator<Mutation, RealVector> mutator = gaussian;
        var observedCalls = 0;
        var observed = mutator.AnalyzedBy(mutator.Invocations.Observing(_ => observedCalls++));
        var scope = ResolutionScope.Create();
        var executor = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem).Resolve(observed);
        scope.Resolve(adaptation).Factor *= 0.0;

        var offspring = executor.Mutate([RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(12));

        offspring.ShouldBe([RealVector.Repeat(1.0, 3)]);
        observedCalls.ShouldBe(1);
        gaussian.MutationStrength.ShouldBe(3.0);
    }

    [Fact]
    public void StatelessMutator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var mutator = new PullTowardZeroMutator();
        var executor = ResolveMutator(mutator);

        var offspring = executor.Mutate(
            [RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(12));

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
    public void MutableMutator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var mutator = new CountingMutator();
        var firstExecutor = ResolveMutator(mutator);
        var secondExecutor = ResolveMutator(mutator);
        var parent = RealVector.Repeat(0.0, 3);

        var first = firstExecutor.Mutate([parent], RandomNumberGenerator.Create(1));
        var second = firstExecutor.Mutate([parent], RandomNumberGenerator.Create(2));
        var independent = secondExecutor.Mutate([parent], RandomNumberGenerator.Create(3));

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void MutableMutator_AuthoringExample_EachExecutorStartsFresh()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        IOperator<Mutation, RealVector> mutator = new CountingMutator();
        var runScope = ResolutionScope.Create();
        var outer = runScope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem).Resolve(mutator);
        var inner = runScope.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem).Resolve(mutator);
        var anotherRun = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem).Resolve(mutator);
        var parent = RealVector.Repeat(0.0, 3);

        var first = outer.Mutate([parent], RandomNumberGenerator.Create(1));
        var shared = inner.Mutate([parent], RandomNumberGenerator.Create(2));
        var independent = anotherRun.Mutate([parent], RandomNumberGenerator.Create(3));

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        shared.ShouldBe([RealVector.Repeat(1.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitMutator_AuthoringExample_OwnsResolvedChildExecution()
    {
        var mutator = new ApplyTwiceMutator(new PullTowardZeroMutator());
        var executor = ResolveMutator(mutator);

        var offspring = executor.Mutate(
            [RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(34));

        offspring.ShouldBe([RealVector.Repeat(0.25, 3)]);
    }

    [Fact]
    public void TopologyMutators_AcceptProblemSpecificChildren()
    {
        // Topology bases forward the run's types to their problem-specific children.
        var pipeline = PipelineMutator.Create(
            new PullTowardZeroMutator(),
            NoChangeMutator<RealVector>.Instance);

        var offspring = ResolveMutator(pipeline).Mutate(
            [RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(34));

        offspring.ShouldBe([RealVector.Repeat(0.5, 3)]);
    }

    [Fact]
    public void StatefulWrapper_KeepsAPrivateCounterOnEachExecutor()
    {
        var problem = CreateRastriginProblem(dimension: 1);
        var mutator = new CallOffsetMutator(new PullTowardZeroMutator());
        var scope = ResolutionScope.Create();
        var outer = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(mutator, problem);
        var inner = scope.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(mutator, problem);
        var independent = ResolveMutator(mutator);

        var parent = RealVector.Repeat(1.0, 1);
        var random = RandomNumberGenerator.Create(34);
        outer.Mutate([parent], random).ShouldBe([RealVector.Repeat(1.5, 1)]);
        inner.Mutate([parent], random).ShouldBe([RealVector.Repeat(1.5, 1)]);
        outer.Mutate([parent], random).ShouldBe([RealVector.Repeat(2.5, 1)]);
        independent.Mutate([parent], random).ShouldBe([RealVector.Repeat(1.5, 1)]);
    }

    [Fact]
    public void MutatorCompositionFactories_InferRoleTypes()
    {
        IOperator<Mutation, RealVector> childMutator = new PullTowardZeroMutator();
        var counting = childMutator.Invocations.Count();
        childMutator.AnalyzedBy(counting).ShouldNotBeSameAs(childMutator);
    }

    [Fact]
    public void StatelessRefiner_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var refiner = new HalveRefiner();
        var executor = ResolveRefiner(refiner);

        var refined = executor.Refine(
            [RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(12));

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
    public void MutableRefiner_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var refiner = new CountingRefiner();
        var firstExecutor = ResolveRefiner(refiner);
        var secondExecutor = ResolveRefiner(refiner);
        var candidate = RealVector.Repeat(0.0, 3);

        var first = firstExecutor.Refine([candidate], RandomNumberGenerator.Create(1));
        var second = firstExecutor.Refine([candidate], RandomNumberGenerator.Create(2));
        var independent = secondExecutor.Refine([candidate], RandomNumberGenerator.Create(3));

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitRefiner_AuthoringExample_OwnsResolvedChildExecution()
    {
        var refiner = new ApplyTwiceRefiner(new HalveRefiner());
        var executor = ResolveRefiner(refiner);

        var refined = executor.Refine(
            [RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(34));

        refined.ShouldBe([RealVector.Repeat(0.25, 3)]);
    }

    [Fact]
    public void TopologyRefiners_AcceptProblemSpecificChildren()
    {
        // Refinement is composed through operator topologies rather than through repeated lifecycle placement, so an
        // ordered pipeline such as repair, simplification and constant optimization is an ordinary configuration.
        var pipeline = PipelineRefiner.Create(
            new HalveRefiner(),
            NoChangeRefiner<RealVector>.Instance);

        var refined = ResolveRefiner(pipeline).Refine(
            [RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(34));

        refined.ShouldBe([RealVector.Repeat(0.5, 3)]);
    }

    [Fact]
    public void IteratedRefiner_RepeatsOneRefinerInsteadOfPlacingItTwice()
    {
        var refiner = new HalveRefiner().AsIterated(3);

        var refined = ResolveRefiner(refiner).Refine(
            [RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(34));

        refined.ShouldBe([RealVector.Repeat(0.125, 3)]);
    }

    [Fact]
    public void RefinerCompositionFactories_InferRoleTypes()
    {
        IOperator<Refinement, RealVector> childRefiner = new HalveRefiner();
        var counting = childRefiner.Invocations.Count();
        var iterated = IteratedRefiner.Create(childRefiner, 2);
        childRefiner.AnalyzedBy(counting).ShouldNotBeSameAs(childRefiner);
        iterated.ChildOperator.ShouldBeSameAs(childRefiner);
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

        var objectiveVectors = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator, problem).Evaluate(
            [candidate], RandomNumberGenerator.Create(7));

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
    public void MutableCreator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var creator = new CountingCreator();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(creator, problem);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(creator, problem);

        var first = firstExecutor.Create(1, RandomNumberGenerator.Create(1));
        var second = firstExecutor.Create(1, RandomNumberGenerator.Create(2));
        var independent = secondExecutor.Create(1, RandomNumberGenerator.Create(3));

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void MutableCrossover_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var crossover = new CountingCrossover();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(crossover, problem);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(crossover, problem);
        var parents = Parents.From(RealVector.Repeat(0.0, 3), RealVector.Repeat(10.0, 3));

        var first = firstExecutor.Cross([parents], RandomNumberGenerator.Create(1));
        var second = firstExecutor.Cross([parents], RandomNumberGenerator.Create(2));
        var independent = secondExecutor.Cross([parents], RandomNumberGenerator.Create(3));

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitCrossover_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var crossover = new ForwardingCrossover(SelectFirstParentCrossover.For(problem));
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(crossover, problem);
        var parents = Parents.From(RealVector.Repeat(1.0, 3), RealVector.Repeat(2.0, 3));

        var offspring = executor.Cross([parents], RandomNumberGenerator.Create(4));

        offspring.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void StatelessEvaluator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new FirstValueEvaluator();
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator, problem);

        var candidate = RealVector.Repeat(2.0, 3);
        var evaluatedCandidates = executor.Evaluate([candidate], RandomNumberGenerator.Create(5));

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
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator, problem);

        var objectives = executor.Evaluate(
            [RealVector.Repeat(2.0, 3), RealVector.Repeat(4.0, 3)], RandomNumberGenerator.Create(5));

        objectives.ShouldBe([new ObjectiveVector(2.0), new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void MutableEvaluator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new CountingEvaluator();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator, problem);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator, problem);
        var candidates = new[] { RealVector.Repeat(0.0, 3) };

        var first = firstExecutor.Evaluate(candidates, RandomNumberGenerator.Create(1));
        var second = firstExecutor.Evaluate(candidates, RandomNumberGenerator.Create(2));
        var independent = secondExecutor.Evaluate(candidates, RandomNumberGenerator.Create(3));

        first.ShouldBe([new ObjectiveVector(1.0)]);
        second.ShouldBe([new ObjectiveVector(2.0)]);
        independent.ShouldBe([new ObjectiveVector(1.0)]);
    }

    [Fact]
    public void ExplicitEvaluator_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new ForwardingEvaluator(new FirstValueEvaluator());
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator, problem);

        var candidate = RealVector.Repeat(4.0, 3);
        var evaluatedCandidates = executor.Evaluate([candidate], RandomNumberGenerator.Create(6));

        evaluatedCandidates.ShouldBe([new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void EvaluatorTopologies_ExposeAndResolveConfiguredChildren()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new FirstValueEvaluator();
        var wrapping = new ForwardingWrappingEvaluator(child);
        var multi = new FirstMultiEvaluator([child, new FirstValueEvaluator()]);

        var wrapped = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(wrapping, problem)
            .Evaluate([RealVector.Repeat(3.0, 3)], RandomNumberGenerator.Create(1));
        var first = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(multi, problem)
            .Evaluate([RealVector.Repeat(4.0, 3)], RandomNumberGenerator.Create(2));

        wrapping.ChildOperator.ShouldBeSameAs(child);
        multi.ChildOperators[0].ShouldBeSameAs(child);
        wrapped.ShouldBe([new ObjectiveVector(3.0)]);
        first.ShouldBe([new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void EvaluatorCompositionFactories_InferRoleTypes()
    {
        IOperator<Evaluation, RealVector> child = new FirstValueEvaluator();
        var counting = child.Invocations.Count();
        var duration = child.Invocations.Duration();
        var repeating = child.AsRepeated(3, ObjectiveVectorAggregation.Median);
        var caching = child.Cached(new FirstCoordinateCacheKeySelector());
        child.AnalyzedBy(counting).ShouldNotBeSameAs(child);
        child.AnalyzedBy(duration).ShouldNotBeSameAs(child);
        repeating.ChildOperator.ShouldBeSameAs(child);
        repeating.Aggregator.ShouldBe(ObjectiveVectorAggregation.Median);
        caching.ChildOperator.ShouldBeSameAs(child);
        caching.KeySelector.ShouldBe(new FirstCoordinateCacheKeySelector());
    }

    [Fact]
    public void StatelessSelector_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new FirstSelector();
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector, problem);
        var population = CreatePopulation(1.0, 2.0);

        var selected = executor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(7));

        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void MutableSelector_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new RotatingSelector();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector, problem);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector, problem);
        var population = CreatePopulation(1.0, 2.0);

        var first = firstExecutor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(1));
        var second = firstExecutor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(2));
        var independent = secondExecutor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(3));

        first.ShouldBe([population[0]]);
        second.ShouldBe([population[1]]);
        independent.ShouldBe([population[0]]);
    }

    [Fact]
    public void ExplicitSelector_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new ForwardingSelector(new FirstSelector());
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector, problem);
        var population = CreatePopulation(1.0, 2.0);

        var selected = executor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(8));

        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void WrappingSelector_AuthoringExample_ResolvesItsChildOnce()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var childSelector = new FirstSelector();
        var selector = new DoublingWrappingSelector(childSelector);
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector, problem);
        var population = CreatePopulation(1.0, 2.0);

        var selected = executor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(11));

        selector.ChildOperator.ShouldBeSameAs(childSelector);
        selected.ShouldBe([population[0], population[0]]);
    }

    [Fact]
    public void MultiSelector_AuthoringExample_ResolvesEveryChildOnce()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var first = new FirstSelector();
        var last = new LastSelector();
        var selector = new PreferFirstMultiSelector([first, last]);
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector, problem);
        var population = CreatePopulation(1.0, 2.0);

        var selected = executor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(12));

        selector.ChildOperators.ShouldBe([first, last]);
        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void SelectorCompositionFactories_InferRoleTypes()
    {
        IOperator<Selection, RealVector> childSelector = new FirstSelector();
        var counting = childSelector.Invocations.Count();
        var chooseOne = ChoosePerInvocation.Create(childSelector, new LastSelector());
        childSelector.AnalyzedBy(counting).ShouldNotBeSameAs(childSelector);
        chooseOne.ChildOperators[0].ShouldBeSameAs(childSelector);
    }

    [Fact]
    public void StatelessReplacer_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new OffspringReplacer();
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer, problem);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var replaced = executor.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(9));

        replaced.ShouldBe(offspring);
    }

    [Fact]
    public void MutableReplacer_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new AlternatingReplacer();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer, problem);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer, problem);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var first = firstExecutor.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1));
        var second = firstExecutor.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(2));
        var independent = secondExecutor.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(3));

        first.ShouldBe(previous);
        second.ShouldBe(offspring);
        independent.ShouldBe(previous);
    }

    [Fact]
    public void ExplicitReplacer_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new ForwardingReplacer(new OffspringReplacer());
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer, problem);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var replaced = executor.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(10));

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

        var wrapped = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(wrapping, problem)
            .Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1));
        var first = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(multi, problem)
            .Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(2));

        wrapping.ChildOperator.ShouldBeSameAs(child);
        multi.ChildOperators[0].ShouldBeSameAs(child);
        wrapped.ShouldBe(offspring);
        first.ShouldBe(offspring);
    }

    [Fact]
    public void ReplacerCompositionFactories_InferRoleTypes()
    {
        IOperator<Replacement, RealVector> child = new OffspringReplacer();
        var counting = child.Invocations.Count();
        var duration = child.Invocations.Duration();
        child.AnalyzedBy(counting).ShouldNotBeSameAs(child);
        child.AnalyzedBy(duration).ShouldNotBeSameAs(child);
    }

    [Fact]
    public void StatelessInterceptor_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new IncrementingInterceptor(), problem);

        var transformed = executor.Transform(new CounterSearchState(1), previousState: null, RandomNumberGenerator.Create(1));

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void MutableInterceptor_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var interceptor = new CountingInterceptor();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(interceptor, problem);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(interceptor, problem);

        var first = firstExecutor.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(1));
        var second = firstExecutor.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(2));
        var independent = secondExecutor.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(3));

        first.Value.ShouldBe(1);
        second.Value.ShouldBe(2);
        independent.Value.ShouldBe(1);
    }

    [Fact]
    public void ExplicitInterceptor_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new ForwardingInterceptor(new IncrementingInterceptor()), problem);

        var transformed = executor.Transform(new CounterSearchState(1), previousState: null, RandomNumberGenerator.Create(1));

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void InterceptorTopologyBases_ExposeMatchingConfigurationAndExecutionShapes()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new IncrementingInterceptor();
        var wrapping = new ForwardingWrappingInterceptor(child);
        var multi = new FirstMultiInterceptor([child]);

        wrapping.ChildOperator.ShouldBeSameAs(child);
        multi.ChildOperators[0].ShouldBeSameAs(child);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(wrapping, problem).Transform(new CounterSearchState(1), null, RandomNumberGenerator.Create(1)).Value.ShouldBe(2);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(multi, problem).Transform(new CounterSearchState(1), null, RandomNumberGenerator.Create(1)).Value.ShouldBe(2);
    }

    [Fact]
    public void Interceptor_ReducedArities_KeepOnlyConsumedContextOnAuthoringSurface()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        IOperator<Interception<CounterSearchState>, RealVector> searchSpaceOnly = new SearchSpaceInterceptor();
        IOperator<Interception<CounterSearchState>, RealVector> candidateOnly = new CandidateInterceptor();
        IOperator<Interception<CounterSearchState>, RealVector> statefulSearchSpaceOnly = new ExplicitSearchSpaceInterceptor();
        IOperator<Interception<CounterSearchState>, RealVector> statefulCandidateOnly = new ExplicitCandidateInterceptor();

        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(searchSpaceOnly, problem).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1)).Value.ShouldBe(1);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(candidateOnly, problem).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1)).Value.ShouldBe(1);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulSearchSpaceOnly, problem).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1)).Value.ShouldBe(1);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulCandidateOnly, problem).Transform(new CounterSearchState(0), null, RandomNumberGenerator.Create(1)).Value.ShouldBe(1);
    }

    [Fact]
    public void StatelessTerminator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new ValueTerminator(2), problem);

        executor.IsTerminalState(new CounterSearchState(1), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        executor.IsTerminalState(new CounterSearchState(2), RandomNumberGenerator.Create(0)).ShouldBeTrue();
    }

    [Fact]
    public void MutableTerminator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var terminator = new CountingTerminator();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(terminator, problem);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(terminator, problem);
        var state = new CounterSearchState(0);

        firstExecutor.IsTerminalState(state, RandomNumberGenerator.Create(0)).ShouldBeFalse();
        firstExecutor.IsTerminalState(state, RandomNumberGenerator.Create(0)).ShouldBeTrue();
        secondExecutor.IsTerminalState(state, RandomNumberGenerator.Create(0)).ShouldBeFalse();
    }

    [Fact]
    public void ExplicitTerminator_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new ForwardingTerminator(new ValueTerminator(2)), problem);

        executor.IsTerminalState(new CounterSearchState(2), RandomNumberGenerator.Create(0)).ShouldBeTrue();
    }

    [Fact]
    public void TerminatorTopologyBases_ExposeMatchingConfigurationAndExecutionShapes()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var child = new ValueTerminator(2);
        var wrapping = new ForwardingWrappingTerminator(child);
        var multi = new FirstMultiTerminator([child]);

        wrapping.ChildOperator.ShouldBeSameAs(child);
        multi.ChildOperators[0].ShouldBeSameAs(child);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(wrapping, problem).IsTerminalState(new CounterSearchState(2), RandomNumberGenerator.Create(0)).ShouldBeTrue();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(multi, problem).IsTerminalState(new CounterSearchState(2), RandomNumberGenerator.Create(0)).ShouldBeTrue();
    }

    [Fact]
    public void Terminator_ReducedArities_IncludeStateAgnosticForm()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        IOperator<Termination, RealVector> searchSpaceOnly = new SearchSpaceTerminator();
        IOperator<Termination, RealVector> stateOnly = new StateTerminator();
        IOperator<Termination, RealVector> stateAgnostic = new StateAgnosticTerminator();
        IOperator<Termination, RealVector> statefulSearchSpaceOnly = new ExplicitSearchSpaceTerminator();
        IOperator<Termination, RealVector> statefulStateOnly = new ExplicitStateTerminator();
        IOperator<Termination, RealVector> statefulStateAgnostic = new ExplicitStateAgnosticTerminator();

        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(searchSpaceOnly, problem).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(stateOnly, problem).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(stateAgnostic, problem).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulSearchSpaceOnly, problem).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulStateOnly, problem).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0)).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulStateAgnostic, problem).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0)).ShouldBeFalse();
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

    private static IMutationExecutor<RealVector> ResolveMutator(IOperator<Mutation, RealVector> mutator)
    {
        return ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(mutator, CreateRastriginProblem(3));
    }

    private static IRefinementExecutor<RealVector> ResolveRefiner(IOperator<Refinement, RealVector> refiner)
    {
        return ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(refiner, CreateRastriginProblem(3));
    }

    /// <summary>
    /// A wrapper that reads a specific problem uses the fixed-signature authoring base. Its factory resolves the
    /// child with that search space and problem after the common compatibility check.
    /// </summary>
    private sealed record PrefixingWrappingCreator(IOperator<Creation, RealVector> Child)
        : Creator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ICreationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
            new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Child, scope.Problem), scope.Problem);

        private sealed class Executor(ICreationExecutor<RealVector> childCreator, TestFunctionProblem problem)
            : ICreationExecutor<RealVector>
        {
            public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random)
            {
                var candidates = childCreator.Create(count, random).ToArray();
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

    private sealed record CountingCreator : Creator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ICreationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : CreationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random)
            {
                invocations++;
                return Enumerable.Repeat(RealVector.Repeat(invocations, Problem.TestFunction.Dimension), count).ToArray();
            }
        }
    }

    private sealed record CountingCrossover : Crossover<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ICrossoverExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : CrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random)
            {
                invocations++;
                return parents.Select(parent => new RealVector(parent.Parent1.Select(value => value + invocations))).ToArray();
            }
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

    private sealed record CountingEvaluator : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override IEvaluationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : EvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random)
            {
                invocations++;
                return candidates.Select(candidate => new ObjectiveVector(invocations)).ToArray();
            }
        }
    }

    private sealed record ForwardingEvaluator(IOperator<Evaluation, RealVector> Inner)
        : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override EvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
            new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner, scope.Problem), scope.Problem);

        private sealed class Executor(IEvaluationExecutor<RealVector> inner, TestFunctionProblem problem)
            : EvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem)
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) =>
                inner.Evaluate(candidates, random);
        }
    }

    private sealed record ForwardingWrappingEvaluator
        : WrappingOperator<Evaluation, RealVector>
    {
        public ForwardingWrappingEvaluator(IOperator<Evaluation, RealVector> childEvaluator)
            : base(childEvaluator)
        {
        }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            var child = scope.Resolve(ChildOperator);
            return new Executor(child);
        }

        private sealed class Executor(IEvaluationExecutor<RealVector> childEvaluator)
            : IEvaluationExecutor<RealVector>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) =>
                childEvaluator.Evaluate(candidates, random);
        }
    }

    private sealed record FirstMultiEvaluator
        : MultiOperator<Evaluation, RealVector>
    {
        public FirstMultiEvaluator(IReadOnlyList<IOperator<Evaluation, RealVector>> childEvaluators)
            : base(childEvaluators)
        {
        }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            ImmutableArray<IEvaluationExecutor<RealVector>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor(children);
        }

        private sealed class Executor(ImmutableArray<IEvaluationExecutor<RealVector>> childEvaluators)
            : IEvaluationExecutor<RealVector>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random) =>
                childEvaluators[0].Evaluate(candidates, random);
        }
    }

    private sealed record FirstSelector : StatelessSelector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            population.Take(count).ToArray();
    }

    private sealed record RotatingSelector : Selector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ISelectionExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : SelectionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
            {
                var start = invocations++ % population.Count;
                return Enumerable.Range(0, count).Select(index => population[(start + index) % population.Count]).ToArray();
            }
        }
    }

    private sealed record ForwardingSelector(IOperator<Selection, RealVector> Inner)
        : Selector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override SelectionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
            new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner, scope.Problem), scope.Problem);

        private sealed class Executor(ISelectionExecutor<RealVector> inner, TestFunctionProblem problem)
            : SelectionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem)
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
                inner.Select(population, objective, count, random);
        }
    }

    private sealed record LastSelector : StatelessSelector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            population.TakeLast(count).ToArray();
    }

    private sealed record DoublingWrappingSelector
        : WrappingOperator<Selection, RealVector>
    {
        public DoublingWrappingSelector(IOperator<Selection, RealVector> childSelector)
            : base(childSelector)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            return new Executor(scope.Resolve(ChildOperator));
        }

        private sealed class Executor(ISelectionExecutor<RealVector> childSelector)
            : ISelectionExecutor<RealVector>
        {
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
            {
                var selected = childSelector.Select(population, objective, count, random);
                return [.. selected, .. selected];
            }
        }
    }

    private sealed record PreferFirstMultiSelector
        : MultiOperator<Selection, RealVector>
    {
        public PreferFirstMultiSelector(ImmutableArray<IOperator<Selection, RealVector>> childSelectors)
            : base(childSelectors)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            ImmutableArray<ISelectionExecutor<RealVector>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor(children);
        }

        private sealed class Executor(ImmutableArray<ISelectionExecutor<RealVector>> childSelectors)
            : ISelectionExecutor<RealVector>
        {
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
                childSelectors[0].Select(population, objective, count, random);
        }
    }

    private sealed record OffspringReplacer : StatelessReplacer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            offspringPopulation.Take(count).ToArray();
    }

    private sealed record AlternatingReplacer : Replacer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override IReplacementExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : ReplacementExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
                invocations++ % 2 == 0 ? previousPopulation.Take(count).ToArray() : offspringPopulation.Take(count).ToArray();
        }
    }

    private sealed record ForwardingReplacer(IOperator<Replacement, RealVector> Inner)
        : Replacer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ReplacementExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
            new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner, scope.Problem), scope.Problem);

        private sealed class Executor(IReplacementExecutor<RealVector> inner, TestFunctionProblem problem)
            : ReplacementExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem)
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
                inner.Replace(previousPopulation, offspringPopulation, objective, count, random);
        }
    }

    private sealed record ForwardingWrappingReplacer
        : WrappingOperator<Replacement, RealVector>
    {
        public ForwardingWrappingReplacer(IOperator<Replacement, RealVector> childReplacer)
            : base(childReplacer)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            return new Executor(scope.Resolve(ChildOperator));
        }

        private sealed class Executor(IReplacementExecutor<RealVector> childReplacer)
            : IReplacementExecutor<RealVector>
        {
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
                childReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random);
        }
    }

    private sealed record FirstMultiReplacer
        : MultiOperator<Replacement, RealVector>
    {
        public FirstMultiReplacer(IReadOnlyList<IOperator<Replacement, RealVector>> childReplacers)
            : base(childReplacers)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            ImmutableArray<IReplacementExecutor<RealVector>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor(children);
        }

        private sealed class Executor(ImmutableArray<IReplacementExecutor<RealVector>> childReplacers)
            : IReplacementExecutor<RealVector>
        {
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
                childReplacers[0].Replace(previousPopulation, offspringPopulation, objective, count, random);
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

    private sealed record ExplicitSearchSpaceInterceptor : Interceptor<RealVector, BoundedRealVectorSearchSpace, CounterSearchState>
    {
        protected override IInterceptionExecutor<CounterSearchState> CreateExecutor(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>, CounterSearchState> scope) =>
                new Executor(searchSpace: scope.SearchSpace);

        private sealed class Executor : InterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, CounterSearchState>
        {
            public Executor(BoundedRealVectorSearchSpace searchSpace) : base(searchSpace: searchSpace) { }

            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random) =>
                currentState with { Value = currentState.Value + 1 };
        }
    }

    private sealed record ExplicitCandidateInterceptor : Interceptor<RealVector, CounterSearchState>
    {
        protected override IInterceptionExecutor<CounterSearchState> CreateExecutor(ResolutionScope<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, CounterSearchState> scope) =>
                new Executor();

        private sealed class Executor : InterceptionExecutor<CounterSearchState>
        {
            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random) =>
                currentState with { Value = currentState.Value + 1 };
        }
    }

    private sealed record CountingInterceptor : Interceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        protected override IInterceptionExecutor<CounterSearchState> CreateExecutor(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : InterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random)
            {
                invocations++;
                return currentState with { Value = currentState.Value + invocations };
            }
        }
    }

    private sealed record ForwardingInterceptor(IOperator<Interception<CounterSearchState>, RealVector> Inner)
        : Interceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        protected override InterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutor(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> scope) =>
            new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(Inner, scope.Problem), scope.Problem);

        private sealed class Executor(IInterceptionExecutor<CounterSearchState> inner, TestFunctionProblem problem)
            : InterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(problem)
        {
            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random) =>
                inner.Transform(currentState, previousState, random);
        }
    }

    private sealed record ForwardingWrappingInterceptor
        : WrappingOperator<Interception<CounterSearchState>, RealVector>
    {
        public ForwardingWrappingInterceptor(IOperator<Interception<CounterSearchState>, RealVector> childInterceptor)
            : base(childInterceptor)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            return new Executor(scope.Resolve(ChildOperator));
        }

        private sealed class Executor(IInterceptionExecutor<CounterSearchState> childInterceptor)
            : IInterceptionExecutor<CounterSearchState>
        {
            public CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random) =>
                childInterceptor.Transform(currentState, previousState, random);
        }
    }

    private sealed record FirstMultiInterceptor
        : MultiOperator<Interception<CounterSearchState>, RealVector>
    {
        public FirstMultiInterceptor(IReadOnlyList<IOperator<Interception<CounterSearchState>, RealVector>> childInterceptors)
            : base(childInterceptors)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            ImmutableArray<IInterceptionExecutor<CounterSearchState>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor(children);
        }

        private sealed class Executor(ImmutableArray<IInterceptionExecutor<CounterSearchState>> childInterceptors)
            : IInterceptionExecutor<CounterSearchState>
        {
            public CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random) =>
                childInterceptors[0].Transform(currentState, previousState, random);
        }
    }

    private sealed record ValueTerminator(int MaximumValue) : StatelessTerminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => state.Value >= MaximumValue;
    }

    private sealed record SearchSpaceTerminator : StatelessTerminator<RealVector, BoundedRealVectorSearchSpace, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) => false;
    }

    private sealed record StateTerminator : StatelessTerminator<RealVector, CounterSearchState>
    {
        public override bool IsTerminalState(CounterSearchState state, IRandomNumberGenerator random) => false;
    }

    private sealed record StateAgnosticTerminator : StatelessTerminator<RealVector>
    {
        public override bool IsTerminalState() => false;
    }

    private sealed record ExplicitSearchSpaceTerminator : Terminator<RealVector, BoundedRealVectorSearchSpace, CounterSearchState>
    {
        protected override ITerminationExecutor<CounterSearchState> CreateExecutor(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>, CounterSearchState> scope) =>
                new Executor(searchSpace: scope.SearchSpace);

        private sealed class Executor : TerminationExecutor<RealVector, BoundedRealVectorSearchSpace, CounterSearchState>
        {
            public Executor(BoundedRealVectorSearchSpace searchSpace) : base(searchSpace: searchSpace) { }

            public override bool IsTerminalState(CounterSearchState state, IRandomNumberGenerator random) => false;
        }
    }

    private sealed record ExplicitStateTerminator : Terminator<RealVector, CounterSearchState>
    {
        protected override ITerminationExecutor<CounterSearchState> CreateExecutor(ResolutionScope<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, CounterSearchState> scope) =>
                new Executor();

        private sealed class Executor : TerminationExecutor<CounterSearchState>
        {
            public override bool IsTerminalState(CounterSearchState state, IRandomNumberGenerator random) => false;
        }
    }

    private sealed record ExplicitStateAgnosticTerminator : Terminator<RealVector>
    {
        protected override ITerminationExecutor<ISearchState> CreateExecutor(ResolutionScope<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>, ISearchState> scope) =>
                new Executor();

        private sealed class Executor : TerminationExecutor
        {
            public override bool IsTerminalState() => false;
        }
    }

    private sealed record CountingTerminator : Terminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        protected override ITerminationExecutor<CounterSearchState> CreateExecutor(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : TerminationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override bool IsTerminalState(CounterSearchState state, IRandomNumberGenerator random) => ++invocations >= 2;
        }
    }

    private sealed record ForwardingTerminator(IOperator<Termination, RealVector> Inner)
        : Terminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        protected override TerminationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> CreateExecutor(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> scope) =>
            new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(Inner, scope.Problem), scope.Problem);

        private sealed class Executor(ITerminationExecutor<CounterSearchState> inner, TestFunctionProblem problem)
            : TerminationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(problem)
        {
            public override bool IsTerminalState(CounterSearchState state, IRandomNumberGenerator random) => inner.IsTerminalState(state, random);
        }
    }

    private sealed record ForwardingWrappingTerminator
        : WrappingOperator<Termination, RealVector>
    {
        public ForwardingWrappingTerminator(IOperator<Termination, RealVector> childTerminator)
            : base(childTerminator)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            return new Executor<TRunSearchState>(scope.Resolve(ChildOperator));
        }

        private sealed class Executor<TSearchState>(ITerminationExecutor<TSearchState> childTerminator)
            : ITerminationExecutor<TSearchState>
            where TSearchState : class, ISearchState
        {
            public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) =>
                childTerminator.IsTerminalState(state, random);
        }
    }

    private sealed record FirstMultiTerminator
        : MultiOperator<Termination, RealVector>
    {
        public FirstMultiTerminator(IReadOnlyList<IOperator<Termination, RealVector>> childTerminators)
            : base(childTerminators)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            ImmutableArray<ITerminationExecutor<TRunSearchState>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor<TRunSearchState>(children);
        }

        private sealed class Executor<TSearchState>(ImmutableArray<ITerminationExecutor<TSearchState>> childTerminators)
            : ITerminationExecutor<TSearchState>
            where TSearchState : class, ISearchState
        {
            public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) =>
                childTerminators[0].IsTerminalState(state, random);
        }
    }

    private sealed record ForwardingCrossover(IOperator<Crossover, RealVector> Inner)
        : Crossover<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override CrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
            new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner, scope.Problem), scope.Problem);

        private sealed class Executor(ICrossoverExecutor<RealVector> inner, TestFunctionProblem problem)
            : CrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem)
        {
            public override IReadOnlyList<RealVector> Cross(IReadOnlyList<Parents<RealVector>> parents, IRandomNumberGenerator random) =>
                inner.Cross(parents, random);
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

    private sealed record CountingMutator : Mutator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override IMutationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : MutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random)
            {
                invocations++;
                return parents.Select(parent => new RealVector(parent.Select(value => value + invocations))).ToArray();
            }
        }
    }

    private sealed record ApplyTwiceMutator
        : WrappingOperator<Mutation, RealVector>
    {
        public ApplyTwiceMutator(IOperator<Mutation, RealVector> childMutator)
            : base(childMutator)
        {
        }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            var child = scope.Resolve(ChildOperator);
            return new Executor(child);
        }

        private sealed class Executor(IMutationExecutor<RealVector> childMutator)
            : IMutationExecutor<RealVector>
        {
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random)
            {
                var first = childMutator.Mutate(parents, random);
                return childMutator.Mutate(first, random);
            }
        }
    }

    private sealed record CallOffsetMutator : WrappingOperator<Mutation, RealVector>
    {
        public CallOffsetMutator(IOperator<Mutation, RealVector> childMutator) : base(childMutator)
        {
        }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            var child = scope.Resolve(ChildOperator);
            return new Executor(child);
        }

        private sealed class Executor(IMutationExecutor<RealVector> child)
            : IMutationExecutor<RealVector>
        {
            private int invocations;

            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random)
            {
                var children = child.Mutate(parents, random);
                invocations++;
                return children.Select(child => new RealVector(child.Select(value => value + invocations))).ToArray();
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

    private sealed record CountingRefiner : Refiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override IRefinementExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
                new Executor(scope.Problem);

        private sealed class Executor : RefinementExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public Executor(TestFunctionProblem problem) : base(problem) { }

            private int invocations;

            public override IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random)
            {
                invocations++;
                return candidates.Select(candidate => new RealVector(candidate.Select(value => value + invocations))).ToArray();
            }
        }
    }

    private sealed record ApplyTwiceRefiner
        : WrappingOperator<Refinement, RealVector>
    {
        public ApplyTwiceRefiner(IOperator<Refinement, RealVector> childRefiner)
            : base(childRefiner)
        {
        }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
        {
            return new Executor(scope.Resolve(ChildOperator));
        }

        private sealed class Executor(IRefinementExecutor<RealVector> childRefiner)
            : IRefinementExecutor<RealVector>
        {
            public IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random)
            {
                var first = childRefiner.Refine(candidates, random);
                return childRefiner.Refine(first, random);
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
        : MultiOperator<Mutation, RealVector>
    {
        public PreferFirstMultiMutator(ImmutableArray<IOperator<Mutation, RealVector>> childMutators)
            : base(childMutators)
        {
        }

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            ImmutableArray<IMutationExecutor<RealVector>> children = [.. ChildOperators.Select(child => scope.Resolve(child))];
            return new Executor(children);
        }

        private sealed class Executor(ImmutableArray<IMutationExecutor<RealVector>> childMutators)
            : IMutationExecutor<RealVector>
        {
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
                childMutators[0].Mutate(parents, random);
        }
    }
}
