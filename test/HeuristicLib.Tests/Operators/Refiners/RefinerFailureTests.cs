using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class RefinerFailureTests
{
    [Fact]
    public void WhenOneCandidateFails_SequentialBatchingThrowsThatFailure()
    {
        var refiner = new FailingOnValueRefiner(2);

        Should.Throw<InvalidOperationException>(() => refiner.Refine([1, 2, 3], RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance))
            .Message.ShouldBe("Refinement failed.");
    }

    // Concurrent batching keeps the execution layer's TPL behavior rather than adding a second convention of its own.
    [Fact]
    public void WhenOneCandidateFails_ConcurrentBatchingReportsTheFailureAsAnAggregate()
    {
        var refiner = new FailingOnValueRefiner(2) { Concurrency = ExecutionConcurrency.Concurrent(2) };

        var exception = Should.Throw<AggregateException>(() => refiner.Refine([1, 2, 3], RandomNumberGenerator.Create(42), DummySearchSpace<int>.Instance));

        exception.InnerExceptions.ShouldContain(inner => inner is InvalidOperationException);
    }

    [Fact]
    public void InARun_AFailingRefinerPropagatesOutOfComplete()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { Refiner = new FailingRefiner() };

        Should.Throw<InvalidOperationException>(() => algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void InARun_AFailingRefinerPropagatesOutOfTheStream()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { Refiner = new FailingRefiner() };

        Should.Throw<InvalidOperationException>(() =>
        {
            foreach (var _ in algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken))
            {
                // The failure happens while producing the first state.
            }
        });
    }

    [Fact]
    public void WhenARunFailsPartWay_InstrumentationKeepsWhatItRecordedBefore()
    {
        var problem = CreateProblem();
        var counter = new ObservationCounter();
        var algorithm = CreateAlgorithm(problem) with { Refiner = new FailingAfterBatchesRefiner(2).CountCalls(counter) };

        Should.Throw<InvalidOperationException>(() => algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken));

        // The two completed batches are counted; the failing one is not, because the counter increments on success.
        counter.CurrentCount.ShouldBe(2);
    }

    private static TestFunctionProblem CreateProblem() => new(new SphereFunction(dimension: 3));

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(TestFunctionProblem problem) =>
        new()
        {
            PopulationSize = 5,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = RandomSelector.For(problem),
            Elites = 0,
            MaximumGenerations = 4
        };

    private sealed record FailingOnValueRefiner(int FailingValue) : SingleCandidateRefiner<int, DummySearchSpace<int>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) =>
            candidate == FailingValue ? throw new InvalidOperationException("Refinement failed.") : candidate;
    }

    private sealed record FailingRefiner : StatelessRefiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
            throw new InvalidOperationException("Refinement failed.");
    }

    private sealed record FailingAfterBatchesRefiner(int SuccessfulBatches) : Refiner<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        private readonly Counter counter = new();

        public override IRefinerInstance<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(counter, SuccessfulBatches);

        private sealed class Counter
        {
            public int Batches;
        }

        private sealed class Instance(Counter counter, int successfulBatches) : IRefinerInstance<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public IReadOnlyList<RealVector> Refine(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem) =>
                counter.Batches++ < successfulBatches ? candidates : throw new InvalidOperationException("Refinement failed.");
        }
    }
}
