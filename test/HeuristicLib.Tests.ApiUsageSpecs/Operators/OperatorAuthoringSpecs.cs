using System.Collections.Immutable;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
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
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Operators;

public class OperatorAuthoringSpecs
{
    [Fact]
    public void StatelessMutator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new PullTowardZeroMutator();
        var instance = ResolveMutator(mutator, problem);

        var offspring = instance.Mutate(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(12),
            problem.SearchSpace,
            problem);

        offspring.ShouldBe([RealVector.Repeat(0.5, 3)]);
    }

    [Fact]
    public void StatefulMutator_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var mutator = new CountingStatefulMutator();
        var firstInstance = ResolveMutator(mutator, problem);
        var secondInstance = ResolveMutator(mutator, problem);
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
        var instance = ResolveMutator(mutator, problem);

        var offspring = instance.Mutate(
            [RealVector.Repeat(1.0, 3)],
            RandomNumberGenerator.Create(34),
            problem.SearchSpace,
            problem);

        offspring.ShouldBe([RealVector.Repeat(0.25, 3)]);
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
        var firstInstance = CreateRegistry(problem).Resolve(creator);
        var secondInstance = CreateRegistry(problem).Resolve(creator);

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
        var firstInstance = CreateRegistry(problem).Resolve(crossover);
        var secondInstance = CreateRegistry(problem).Resolve(crossover);
        var parents = new Parents<RealVector>(RealVector.Repeat(0.0, 3), RealVector.Repeat(10.0, 3));

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
        var crossover = new ForwardingCrossover(SelectFirstParentCrossover<RealVector>.Instance);
        var instance = CreateRegistry(problem).Resolve(crossover);
        var parents = new Parents<RealVector>(RealVector.Repeat(1.0, 3), RealVector.Repeat(2.0, 3));

        var offspring = instance.Cross([parents], RandomNumberGenerator.Create(4), problem.SearchSpace, problem);

        offspring.ShouldBe([RealVector.Repeat(1.0, 3)]);
    }

    [Fact]
    public void StatelessEvaluator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new FirstValueEvaluator();
        var instance = CreateRegistry(problem).Resolve(evaluator);

        var candidate = RealVector.Repeat(2.0, 3);
        var evaluatedCandidates = instance.Evaluate([candidate], RandomNumberGenerator.Create(5), problem.SearchSpace, problem);

        evaluatedCandidates.ShouldBe([candidate.ToEvaluated(new ObjectiveVector(2.0))]);
    }

    [Fact]
    public void StatefulEvaluator_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new CountingStatefulEvaluator();
        var firstInstance = CreateRegistry(problem).Resolve(evaluator);
        var secondInstance = CreateRegistry(problem).Resolve(evaluator);
        var candidates = new[] { RealVector.Repeat(0.0, 3) };

        var first = firstInstance.Evaluate(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        var second = firstInstance.Evaluate(candidates, RandomNumberGenerator.Create(2), problem.SearchSpace, problem);
        var independent = secondInstance.Evaluate(candidates, RandomNumberGenerator.Create(3), problem.SearchSpace, problem);

        first.ShouldBe([candidates[0].ToEvaluated(new ObjectiveVector(1.0))]);
        second.ShouldBe([candidates[0].ToEvaluated(new ObjectiveVector(2.0))]);
        independent.ShouldBe([candidates[0].ToEvaluated(new ObjectiveVector(1.0))]);
    }

    [Fact]
    public void ExplicitEvaluator_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var evaluator = new ForwardingEvaluator(new FirstValueEvaluator());
        var instance = CreateRegistry(problem).Resolve(evaluator);

        var candidate = RealVector.Repeat(4.0, 3);
        var evaluatedCandidates = instance.Evaluate([candidate], RandomNumberGenerator.Create(6), problem.SearchSpace, problem);

        evaluatedCandidates.ShouldBe([candidate.ToEvaluated(new ObjectiveVector(4.0))]);
    }

    [Fact]
    public void StatelessSelector_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new FirstSelector();
        var instance = CreateRegistry(problem).Resolve(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = instance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(7), problem.SearchSpace, problem);

        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void StatefulSelector_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var selector = new RotatingStatefulSelector();
        var firstInstance = CreateRegistry(problem).Resolve(selector);
        var secondInstance = CreateRegistry(problem).Resolve(selector);
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
        var instance = CreateRegistry(problem).Resolve(selector);
        var population = CreatePopulation(1.0, 2.0);

        var selected = instance.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(8), problem.SearchSpace, problem);

        selected.ShouldBe([population[0]]);
    }

    [Fact]
    public void StatelessReplacer_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var replacer = new OffspringReplacer();
        var instance = CreateRegistry(problem).Resolve(replacer);
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
        var firstInstance = CreateRegistry(problem).Resolve(replacer);
        var secondInstance = CreateRegistry(problem).Resolve(replacer);
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
        var instance = CreateRegistry(problem).Resolve(replacer);
        var previous = CreatePopulation(1.0);
        var offspring = CreatePopulation(2.0);

        var replaced = instance.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(10), problem.SearchSpace, problem);

        replaced.ShouldBe(offspring);
    }

    [Fact]
    public void StatelessInterceptor_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = CreateRegistry(problem).Resolve(new IncrementingInterceptor());

        var transformed = instance.Transform(new CounterSearchState(1), previousState: null, problem.SearchSpace, problem);

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void StatefulInterceptor_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var interceptor = new CountingStatefulInterceptor();
        var firstInstance = CreateRegistry(problem).Resolve(interceptor);
        var secondInstance = CreateRegistry(problem).Resolve(interceptor);

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
        var instance = CreateRegistry(problem).Resolve(new ForwardingInterceptor(new IncrementingInterceptor()));

        var transformed = instance.Transform(new CounterSearchState(1), previousState: null, problem.SearchSpace, problem);

        transformed.Value.ShouldBe(2);
    }

    [Fact]
    public void StatelessTerminator_AuthoringExample_UsesConfigurationAndExplicitInputs()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = CreateRegistry(problem).Resolve(new ValueTerminator(2));

        instance.IsTerminalState(new CounterSearchState(1), problem.SearchSpace, problem).ShouldBeFalse();
        instance.IsTerminalState(new CounterSearchState(2), problem.SearchSpace, problem).ShouldBeTrue();
    }

    [Fact]
    public void StatefulTerminator_AuthoringExample_GetsIndependentExecutionDataPerInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var terminator = new CountingStatefulTerminator();
        var firstInstance = CreateRegistry(problem).Resolve(terminator);
        var secondInstance = CreateRegistry(problem).Resolve(terminator);
        var state = new CounterSearchState(0);

        firstInstance.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeFalse();
        firstInstance.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeTrue();
        secondInstance.IsTerminalState(state, problem.SearchSpace, problem).ShouldBeFalse();
    }

    [Fact]
    public void ExplicitTerminator_AuthoringExample_OwnsResolvedChildInstance()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var instance = CreateRegistry(problem).Resolve(new ForwardingTerminator(new ValueTerminator(2)));

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

    private static IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> ResolveMutator(IMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem> mutator, TestFunctionProblem problem)
    {
        return CreateRegistry(problem).Resolve(mutator);
    }

    private static ExecutionInstanceRegistry CreateRegistry(TestFunctionProblem problem)
    {
        var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new ConstantOriginCreator(),
            Mutator = new NoChangeMutator(),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 1,
            MaxNeighbors = 1
        };

        return new ExecutionInstanceRegistry();
    }

    private sealed record PrefixingWrappingCreator(ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
      : WrappingCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>(Inner)
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

        protected override IReadOnlyList<RealVector> Cross(IReadOnlyList<IParents<RealVector>> parents, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return parents.Select(parent => new RealVector(parent.Parent1.Select(value => value + state.Calls))).ToArray();
        }
    }

    private sealed record FirstValueEvaluator : StatelessEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<RealVector>> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            candidates.Select(candidate => candidate.ToEvaluated(new ObjectiveVector(candidate[0]))).ToArray();
    }

    private sealed record CountingStatefulEvaluator : StatefulEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingStatefulEvaluator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<EvaluatedCandidate<RealVector>> Evaluate(IReadOnlyList<RealVector> candidates, ExecutionState state, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            state.Calls++;
            return candidates.Select(candidate => candidate.ToEvaluated(new ObjectiveVector(state.Calls))).ToArray();
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
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
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
        protected override SelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateSelectorInstance(ExecutionInstanceRegistry registry) =>
            new Instance(registry.Resolve(Inner));

        private sealed class Instance(ISelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : SelectorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<EvaluatedCandidate<RealVector>> Select(IReadOnlyList<EvaluatedCandidate<RealVector>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                inner.Select(population, objective, count, random, searchSpace, problem);
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
            public override IReadOnlyList<RealVector> Cross(IReadOnlyList<IParents<RealVector>> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
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
      : SingleSolutionMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector Mutate(
          RealVector parent,
          IRandomNumberGenerator random,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return parent;
        }
    }

    private sealed record PullTowardZeroMutator
      : SingleSolutionMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector Mutate(
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

    private sealed record ApplyTwiceMutator(IMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
        : Mutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        protected override IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateMutatorInstance(ExecutionInstanceRegistry registry) =>
            new Instance(registry.Resolve(Inner));

        private sealed class Instance(IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
            : MutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                var first = inner.Mutate(parents, random, searchSpace, problem);
                return inner.Mutate(first, random, searchSpace, problem);
            }
        }
    }

    private sealed record PushAwayFromZeroMutator
        : SingleSolutionMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector Mutate(RealVector parent, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
        {
            return new RealVector(parent.Select(x => x * 2.0));
        }
    }

    private sealed record PreferFirstMultiMutator(ImmutableArray<IMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>> Inner)
        : MultiMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>(Inner)
    {
        protected override MultiMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateMutatorInstance(ImmutableArray<IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> innerMutators) =>
            new Instance(innerMutators);

        private sealed class Instance(ImmutableArray<IMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>> innerMutators)
            : MultiMutatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>(innerMutators)
        {
            public override IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                InnerMutators[0].Mutate(parents, random, searchSpace, problem);
        }
    }
}
