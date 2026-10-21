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
    public void AdaptiveMutator_ProjectsStrengthControlAndInvokesTheObservedExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var gaussian = new GaussianMutator(1.0, 3.0);
        IOperator<Mutation, RealVector> mutator = gaussian;
        var observedCalls = 0;
        var scope = ResolutionScope.Create(Aspect.Observe(mutator, _ => observedCalls++));
        var executor = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(mutator, out IMutationStrengthControl? strength);
        strength.ShouldNotBeNull();
        strength.CurrentMutationStrength = 0.0;

        var offspring = executor.Mutate([RealVector.Repeat(1.0, 3)], RandomNumberGenerator.Create(12), problem.SearchSpace, problem);

        offspring.ShouldBe([RealVector.Repeat(1.0, 3)]);
        observedCalls.ShouldBe(1);
        gaussian.MutationStrength.ShouldBe(3.0);
    }

    [Fact]
    public void StatelessMutator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new PullTowardZeroMutator();
        var executor = ResolveMutator(mutator);

        var offspring = executor.Mutate(
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
        var firstExecutor = ResolveMutator(mutator);
        var secondExecutor = ResolveMutator(mutator);
        var parent = RealVector.Repeat(0.0, 3);

        var first = firstExecutor.Mutate([parent], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecutor.Mutate([parent], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecutor.Mutate([parent], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void StatefulMutator_AuthoringExample_SharesAncestorStateWhileIndependentRunsStartFresh()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        IOperator<Mutation, RealVector> mutator = new CountingStatefulMutator();
        var runScope = ResolutionScope.Create();
        var outer = runScope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(mutator);
        var inner = runScope.CreateChildScope().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(mutator);
        var anotherRun = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>().Resolve(mutator);
        var parent = RealVector.Repeat(0.0, 3);

        var first = outer.Mutate([parent], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var shared = inner.Mutate([parent], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = anotherRun.Mutate([parent], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        shared.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitMutator_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new ApplyTwiceMutator(new PullTowardZeroMutator());
        var executor = ResolveMutator(mutator);

        var offspring = executor.Mutate(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(34),
            problem.SearchSpace,
            problem);

        offspring.ShouldBe([RealVector.Repeat(0.25, 3)]);
    }

    [Fact]
    public void TopologyMutators_AcceptProblemSpecificChildren()
    {
        // Topology bases forward the run's types to their problem-specific children.
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
    public void StatefulWrapper_PreparesItsCounterOnceAndIndependentRunsStartFresh()
    {
        var problem = CreateRastriginProblem(dimension: 1);
        var mutator = new CallOffsetMutator(new PullTowardZeroMutator());
        var scope = ResolutionScope.Create();
        var outer = scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(mutator);
        var inner = scope.CreateChildScope().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(mutator);
        var independent = ResolveMutator(mutator);

        var parent = RealVector.Repeat(1.0, 1);
        var random = RandomNumberGenerator.Create(34);
        outer.Mutate([parent], random, problem.SearchSpace, problem).ShouldBe([RealVector.Repeat(1.5, 1)]);
        inner.Mutate([parent], random, problem.SearchSpace, problem).ShouldBe([RealVector.Repeat(2.5, 1)]);
        outer.Mutate([parent], random, problem.SearchSpace, problem).ShouldBe([RealVector.Repeat(3.5, 1)]);
        independent.Mutate([parent], random, problem.SearchSpace, problem).ShouldBe([RealVector.Repeat(1.5, 1)]);
    }

    [Fact]
    public void MutatorCompositionFactories_InferRoleTypes()
    {
        IOperator<Mutation, RealVector> childMutator = new PullTowardZeroMutator();
        var counting = Aspect.CountCalls(childMutator, new CountAccumulator());
        counting.Advices.Single().Pointcut.Matches(childMutator).ShouldBeTrue();
    }

    [Fact]
    public void StatelessRefiner_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new HalveRefiner();
        var executor = ResolveRefiner(refiner);

        var refined = executor.Refine(
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
        var firstExecutor = ResolveRefiner(refiner);
        var secondExecutor = ResolveRefiner(refiner);
        var candidate = RealVector.Repeat(0.0, 3);

        var first = firstExecutor.Refine([candidate], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecutor.Refine([candidate], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecutor.Refine([candidate], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void ExplicitRefiner_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var refiner = new ApplyTwiceRefiner(new HalveRefiner());
        var executor = ResolveRefiner(refiner);

        var refined = executor.Refine(
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
        IOperator<Refinement, RealVector> childRefiner = new HalveRefiner();
        var counting = Aspect.CountCalls(childRefiner, new CountAccumulator());
        var iterated = IteratedRefiner.Create(childRefiner, 2);
        counting.Advices.Single().Pointcut.Matches(childRefiner).ShouldBeTrue();
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
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(creator);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(creator);

        var first = firstExecutor.Create(1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecutor.Create(1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecutor.Create(1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([RealVector.Repeat(1.0, 3)]);
        second.ShouldBe([RealVector.Repeat(2.0, 3)]);
        independent.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void StatefulCrossover_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var crossover = new CountingStatefulCrossover();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(crossover);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(crossover);
        var parents = Parents.From(RealVector.Repeat(0.0, 3), RealVector.Repeat(10.0, 3));

        var first = firstExecutor.Cross([parents], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecutor.Cross([parents], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecutor.Cross([parents], RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

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
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator);

        var objectives = executor.Evaluate(
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
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(evaluator);
        var candidates = new[] { RealVector.Repeat(0.0, 3) };

        var first = firstExecutor.Evaluate(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecutor.Evaluate(candidates, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecutor.Evaluate(candidates, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

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

        var wrapped = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(wrapping)
            .Evaluate([RealVector.Repeat(3.0, 3)], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var first = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(multi)
            .Evaluate([RealVector.Repeat(4.0, 3)], RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        wrapping.ChildOperator.ShouldBeSameAs(child);
        multi.ChildOperators[0].ShouldBeSameAs(child);
        wrapped.ShouldBe([new ObjectiveVector(3.0)]);
        first.ShouldBe([new ObjectiveVector(4.0)]);
    }

    [Fact]
    public void EvaluatorCompositionFactories_InferRoleTypes()
    {
        IOperator<Evaluation, RealVector> child = new FirstValueEvaluator();
        var counting = Aspect.CountCalls(child, new CountAccumulator());
        var duration = Aspect.MeasureDuration(child, new DurationAccumulator());
        var repeating = child.AsRepeated(3, ObjectiveVectorAggregation.Median);
        var caching = child.Cached(new FirstCoordinateCacheKeySelector());
        counting.Advices.Single().Pointcut.Matches(child).ShouldBeTrue();
        duration.Advices.Single().Pointcut.Matches(child).ShouldBeTrue();
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
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var population = CreatePopulation(1.0, 2.0);

        var first = firstExecutor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecutor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecutor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

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
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = executor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(11), problem.SearchSpace, problem);

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
        var executor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = executor.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(12), problem.SearchSpace, problem);

        selector.ChildOperators.ShouldBe([first, last]);
        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void SelectorCompositionFactories_InferRoleTypes()
    {
        IOperator<Selection, RealVector> childSelector = new FirstSelector();
        var counting = Aspect.CountCalls(childSelector, new CountAccumulator());
        var chooseOne = ChoosePerCall.Create(childSelector, new LastSelector());
        counting.Advices.Single().Pointcut.Matches(childSelector).ShouldBeTrue();
        chooseOne.ChildOperators[0].ShouldBeSameAs(childSelector);
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
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(replacer);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var first = firstExecutor.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecutor.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecutor.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

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

        var wrapped = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(wrapping)
            .Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var first = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(multi)
            .Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);

        wrapping.ChildOperator.ShouldBeSameAs(child);
        multi.ChildOperators[0].ShouldBeSameAs(child);
        wrapped.ShouldBe(offspring);
        first.ShouldBe(offspring);
    }

    [Fact]
    public void ReplacerCompositionFactories_InferRoleTypes()
    {
        IOperator<Replacement, RealVector> child = new OffspringReplacer();
        var counting = Aspect.CountCalls(child, new CountAccumulator());
        var duration = Aspect.MeasureDuration(child, new DurationAccumulator());
        counting.Advices.Single().Pointcut.Matches(child).ShouldBeTrue();
        duration.Advices.Single().Pointcut.Matches(child).ShouldBeTrue();
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
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(interceptor);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(interceptor);

        var first = firstExecutor.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstExecutor.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondExecutor.Transform(new CounterSearchState(0), previousState: null, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

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

        wrapping.ChildOperator.ShouldBeSameAs(child);
        multi.ChildOperators[0].ShouldBeSameAs(child);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(wrapping).Transform(new CounterSearchState(1), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(2);
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(multi).Transform(new CounterSearchState(1), null, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Value.ShouldBe(2);
    }

    [Fact]
    public void Interceptor_ReducedArities_KeepOnlyConsumedContextOnAuthoringSurface()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        IOperator<Interception<CounterSearchState>, RealVector> searchSpaceOnly = new SearchSpaceInterceptor();
        IOperator<Interception<CounterSearchState>, RealVector> candidateOnly = new CandidateInterceptor();
        IOperator<Interception<CounterSearchState>, RealVector> statefulSearchSpaceOnly = new StatefulSearchSpaceInterceptor();
        IOperator<Interception<CounterSearchState>, RealVector> statefulCandidateOnly = new StatefulCandidateInterceptor();

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

        execution.IsTerminalState(new CounterSearchState(1), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        execution.IsTerminalState(new CounterSearchState(2), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void StatefulTerminator_AuthoringExample_GetsIndependentExecutionDataPerExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var terminator = new CountingStatefulTerminator();
        var firstExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(terminator);
        var secondExecutor = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(terminator);
        var state = new CounterSearchState(0);

        firstExecutor.IsTerminalState(state, RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        firstExecutor.IsTerminalState(state, RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();
        secondExecutor.IsTerminalState(state, RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
    }

    [Fact]
    public void ExplicitTerminator_AuthoringExample_OwnsResolvedChildExecution()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var execution = ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(new ForwardingTerminator(new ValueTerminator(2)));

        execution.IsTerminalState(new CounterSearchState(2), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();
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
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(wrapping).IsTerminalState(new CounterSearchState(2), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(multi).IsTerminalState(new CounterSearchState(2), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void Terminator_ReducedArities_IncludeStateAgnosticForm()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        IOperator<Termination, RealVector> searchSpaceOnly = new SearchSpaceTerminator();
        IOperator<Termination, RealVector> stateOnly = new StateTerminator();
        IOperator<Termination, RealVector> stateAgnostic = new StateAgnosticTerminator();
        IOperator<Termination, RealVector> statefulSearchSpaceOnly = new StatefulSearchSpaceTerminator();
        IOperator<Termination, RealVector> statefulStateOnly = new StatefulStateTerminator();
        IOperator<Termination, RealVector> statefulStateAgnostic = new StatefulStateAgnosticTerminator();

        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(searchSpaceOnly).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(stateOnly).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(stateAgnostic).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulSearchSpaceOnly).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulStateOnly).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
        ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(statefulStateAgnostic).IsTerminalState(new CounterSearchState(0), RandomNumberGenerator.Create(0), problem.SearchSpace, problem).ShouldBeFalse();
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

    private static IMutationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> ResolveMutator(IOperator<Mutation, RealVector> mutator)
    {
        return ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(mutator);
    }

    private static IRefinementExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> ResolveRefiner(IOperator<Refinement, RealVector> refiner)
    {
        return ResolutionScope.Create().Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(refiner);
    }

    /// <summary>
    /// A wrapper that reads a specific problem uses the fixed-signature authoring base. Its factory resolves the
    /// child with that search space and problem after the common compatibility check.
    /// </summary>
    private sealed record PrefixingWrappingCreator(IOperator<Creation, RealVector> Child)
        : Creator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ExecutorFactory<ICreationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>> CreateFactory() =>
            scope => new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Child));

        private sealed class Executor(ICreationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> childCreator)
            : ICreationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var candidates = childCreator.Create(count, random, searchSpace, problem).ToArray();
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

    private sealed record ForwardingEvaluator(IOperator<Evaluation, RealVector> Inner)
        : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ExecutorFactory<EvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>> CreateFactory() =>
            scope => new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

        private sealed class Executor(IEvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
            : EvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Evaluate(candidates, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingEvaluator
        : WrappingOperator<Evaluation, RealVector>
    {
        public ForwardingWrappingEvaluator(IOperator<Evaluation, RealVector> childEvaluator)
            : base(childEvaluator)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory()
        {
            return new Factory(this);
        }

        private sealed class Factory(ForwardingWrappingEvaluator source) : OperatorExecutorFactory<Evaluation, RealVector>
        {
            protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            {
                var child = scope.Resolve(source.ChildOperator);
                return new Executor<TSearchSpace, TProblem>(child);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<RealVector, TSearchSpace, TProblem> childEvaluator)
            : IEvaluationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childEvaluator.Evaluate(candidates, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiEvaluator
        : MultiOperator<Evaluation, RealVector>
    {
        public FirstMultiEvaluator(IReadOnlyList<IOperator<Evaluation, RealVector>> childEvaluators)
            : base(childEvaluators)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory()
        {
            return new Factory(this);
        }

        private sealed class Factory(FirstMultiEvaluator source) : OperatorExecutorFactory<Evaluation, RealVector>
        {
            protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            {
                System.Collections.Immutable.ImmutableArray<IEvaluationExecutor<RealVector, TSearchSpace, TProblem>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TSearchSpace, TProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IEvaluationExecutor<RealVector, TSearchSpace, TProblem>> childEvaluators)
            : IEvaluationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childEvaluators[0].Evaluate(candidates, random, searchSpace, problem);
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

    private sealed record ForwardingSelector(IOperator<Selection, RealVector> Inner)
        : Selector<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ExecutorFactory<SelectionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>> CreateFactory() =>
            scope => new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

        private sealed class Executor(ISelectionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
            : SelectionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
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
        : WrappingOperator<Selection, RealVector>
    {
        public DoublingWrappingSelector(IOperator<Selection, RealVector> childSelector)
            : base(childSelector)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(DoublingWrappingSelector source) : OperatorExecutorFactory<Selection, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                return new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(source.ChildOperator));
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ISelectionExecutor<RealVector, TSearchSpace, TProblem> childSelector)
            : ISelectionExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var selected = childSelector.Select(population, objective, count, random, searchSpace, problem);
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

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(PreferFirstMultiSelector source) : OperatorExecutorFactory<Selection, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                ImmutableArray<ISelectionExecutor<RealVector, TRunSearchSpace, TRunProblem>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TRunSearchSpace, TRunProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<ISelectionExecutor<RealVector, TSearchSpace, TProblem>> childSelectors)
            : ISelectionExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childSelectors[0].Select(population, objective, count, random, searchSpace, problem);
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

    private sealed record ForwardingReplacer(IOperator<Replacement, RealVector> Inner)
        : Replacer<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ExecutorFactory<ReplacementExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>> CreateFactory() =>
            scope => new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

        private sealed class Executor(IReplacementExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
            : ReplacementExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingReplacer
        : WrappingOperator<Replacement, RealVector>
    {
        public ForwardingWrappingReplacer(IOperator<Replacement, RealVector> childReplacer)
            : base(childReplacer)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(ForwardingWrappingReplacer source) : OperatorExecutorFactory<Replacement, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                return new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(source.ChildOperator));
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(IReplacementExecutor<RealVector, TSearchSpace, TProblem> childReplacer)
            : IReplacementExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childReplacer.Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiReplacer
        : MultiOperator<Replacement, RealVector>
    {
        public FirstMultiReplacer(IReadOnlyList<IOperator<Replacement, RealVector>> childReplacers)
            : base(childReplacers)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(FirstMultiReplacer source) : OperatorExecutorFactory<Replacement, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                ImmutableArray<IReplacementExecutor<RealVector, TRunSearchSpace, TRunProblem>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TRunSearchSpace, TRunProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IReplacementExecutor<RealVector, TSearchSpace, TProblem>> childReplacers)
            : IReplacementExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<EvaluatedCandidate<RealVector>> Replace(IReadOnlyList<EvaluatedCandidate<RealVector>> previousPopulation, IReadOnlyList<EvaluatedCandidate<RealVector>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childReplacers[0].Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
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

    private sealed record ForwardingInterceptor(IOperator<Interception<CounterSearchState>, RealVector> Inner)
        : Interceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        protected override ExecutorFactory<InterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>> CreateFactory() =>
            scope => new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(Inner));

        private sealed class Executor(IInterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> inner)
            : InterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public override CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Transform(currentState, previousState, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingInterceptor
        : WrappingOperator<Interception<CounterSearchState>, RealVector>
    {
        public ForwardingWrappingInterceptor(IOperator<Interception<CounterSearchState>, RealVector> childInterceptor)
            : base(childInterceptor)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(ForwardingWrappingInterceptor source) : OperatorExecutorFactory<Interception<CounterSearchState>, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                return new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(source.ChildOperator));
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(IInterceptionExecutor<RealVector, TSearchSpace, TProblem, CounterSearchState> childInterceptor)
            : IInterceptionExecutor<RealVector, TSearchSpace, TProblem, CounterSearchState>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childInterceptor.Transform(currentState, previousState, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiInterceptor
        : MultiOperator<Interception<CounterSearchState>, RealVector>
    {
        public FirstMultiInterceptor(IReadOnlyList<IOperator<Interception<CounterSearchState>, RealVector>> childInterceptors)
            : base(childInterceptors)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(FirstMultiInterceptor source) : OperatorExecutorFactory<Interception<CounterSearchState>, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                ImmutableArray<IInterceptionExecutor<RealVector, TRunSearchSpace, TRunProblem, CounterSearchState>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TRunSearchSpace, TRunProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IInterceptionExecutor<RealVector, TSearchSpace, TProblem, CounterSearchState>> childInterceptors)
            : IInterceptionExecutor<RealVector, TSearchSpace, TProblem, CounterSearchState>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public CounterSearchState Transform(CounterSearchState currentState, CounterSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childInterceptors[0].Transform(currentState, previousState, random, searchSpace, problem);
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

    private sealed record StatefulSearchSpaceTerminator : StatefulTerminator<RealVector, BoundedRealVectorSearchSpace, CounterSearchState, StatefulSearchSpaceTerminator.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override bool IsTerminalState(CounterSearchState searchState, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) => false;
    }

    private sealed record StatefulStateTerminator : StatefulTerminator<RealVector, CounterSearchState, StatefulStateTerminator.ExecutionState>
    {
        public sealed class ExecutionState { }

        protected override ExecutionState CreateInitialState() => new();

        protected override bool IsTerminalState(CounterSearchState searchState, ExecutionState state, IRandomNumberGenerator random) => false;
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

        protected override bool IsTerminalState(CounterSearchState searchState, ExecutionState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => ++state.Calls >= 2;
    }

    private sealed record ForwardingTerminator(IOperator<Termination, RealVector> Inner)
        : Terminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
    {
        protected override ExecutorFactory<TerminationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>> CreateFactory() =>
            scope => new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>(Inner));

        private sealed class Executor(ITerminationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState> inner)
            : TerminationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CounterSearchState>
        {
            public override bool IsTerminalState(CounterSearchState state, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) => inner.IsTerminalState(state, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingWrappingTerminator
        : WrappingOperator<Termination, RealVector>
    {
        public ForwardingWrappingTerminator(IOperator<Termination, RealVector> childTerminator)
            : base(childTerminator)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(ForwardingWrappingTerminator source) : OperatorExecutorFactory<Termination, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                return new Executor<TRunSearchSpace, TRunProblem, TRunSearchState>(scope.Resolve(source.ChildOperator));
            }
        }

        private sealed class Executor<TSearchSpace, TProblem, TSearchState>(ITerminationExecutor<RealVector, TSearchSpace, TProblem, TSearchState> childTerminator)
            : ITerminationExecutor<RealVector, TSearchSpace, TProblem, TSearchState>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childTerminator.IsTerminalState(state, random, searchSpace, problem);
        }
    }

    private sealed record FirstMultiTerminator
        : MultiOperator<Termination, RealVector>
    {
        public FirstMultiTerminator(IReadOnlyList<IOperator<Termination, RealVector>> childTerminators)
            : base(childTerminators)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(FirstMultiTerminator source) : OperatorExecutorFactory<Termination, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                ImmutableArray<ITerminationExecutor<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TRunSearchSpace, TRunProblem, TRunSearchState>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem, TSearchState>(ImmutableArray<ITerminationExecutor<RealVector, TSearchSpace, TProblem, TSearchState>> childTerminators)
            : ITerminationExecutor<RealVector, TSearchSpace, TProblem, TSearchState>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            public bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childTerminators[0].IsTerminalState(state, random, searchSpace, problem);
        }
    }

    private sealed record ForwardingCrossover(IOperator<Crossover, RealVector> Inner)
        : Crossover<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        protected override ExecutorFactory<CrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>> CreateFactory() =>
            scope => new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

        private sealed class Executor(ICrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
            : CrossoverExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
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
        : WrappingOperator<Mutation, RealVector>
    {
        public ApplyTwiceMutator(IOperator<Mutation, RealVector> childMutator)
            : base(childMutator)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory()
        {
            return new Factory(this);
        }

        private sealed class Factory(ApplyTwiceMutator source) : OperatorExecutorFactory<Mutation, RealVector>
        {
            protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            {
                var child = scope.Resolve(source.ChildOperator);
                return new Executor<TSearchSpace, TProblem>(child);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(IMutationExecutor<RealVector, TSearchSpace, TProblem> childMutator)
            : IMutationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var first = childMutator.Mutate(parents, random, searchSpace, problem);
                return childMutator.Mutate(first, random, searchSpace, problem);
            }
        }
    }

    private sealed record CallOffsetMutator : WrappingOperator<Mutation, RealVector>
    {
        public CallOffsetMutator(IOperator<Mutation, RealVector> childMutator) : base(childMutator)
        {
        }

        private sealed class ExecutionState
        {
            public int Calls;
        }

        protected override OperatorExecutorFactory CreateOperatorFactory()
        {
            var state = new ExecutionState();
            return new Factory(this, state);
        }

        private sealed class Factory(CallOffsetMutator source, ExecutionState state) : OperatorExecutorFactory<Mutation, RealVector>
        {
            protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            {
                var child = scope.Resolve(source.ChildOperator);
                return new Executor<TSearchSpace, TProblem>(child, state);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(IMutationExecutor<RealVector, TSearchSpace, TProblem> child, ExecutionState state)
            : IMutationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var children = child.Mutate(parents, random, searchSpace, problem);
                state.Calls++;
                return children.Select(child => new RealVector(child.Select(value => value + state.Calls))).ToArray();
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
        : WrappingOperator<Refinement, RealVector>
    {
        public ApplyTwiceRefiner(IOperator<Refinement, RealVector> childRefiner)
            : base(childRefiner)
        {
        }

        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(this);

        private sealed class Factory(ApplyTwiceRefiner source) : OperatorExecutorFactory<Refinement, RealVector>
        {
            protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
                ResolutionScope<RealVector, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
            {
                return new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve(source.ChildOperator));
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(IRefinementExecutor<RealVector, TSearchSpace, TProblem> childRefiner)
            : IRefinementExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var first = childRefiner.Refine(candidates, random, searchSpace, problem);
                return childRefiner.Refine(first, random, searchSpace, problem);
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

        protected override OperatorExecutorFactory CreateOperatorFactory()
        {
            return new Factory(this);
        }

        private sealed class Factory(PreferFirstMultiMutator source) : OperatorExecutorFactory<Mutation, RealVector>
        {
            protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
                ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            {
                System.Collections.Immutable.ImmutableArray<IMutationExecutor<RealVector, TSearchSpace, TProblem>> children = [.. source.ChildOperators.Select(child => scope.Resolve(child))];
                return new Executor<TSearchSpace, TProblem>(children);
            }
        }

        private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IMutationExecutor<RealVector, TSearchSpace, TProblem>> childMutators)
            : IMutationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                childMutators[0].Mutate(parents, random, searchSpace, problem);
        }
    }
}
