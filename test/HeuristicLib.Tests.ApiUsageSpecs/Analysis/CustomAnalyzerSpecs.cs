using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

// Analyzers and counters define data collected during a run. Attach them with CreateRun or place their
// wrappers explicitly, and read each run's own result from the run.
public class CustomAnalyzerSpecs
{
    [Fact]
    public void AMixedClockListCanBeReadByAnExternalAnalyzer()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 3 };
        var iterations = new Counter();
        var external = new ExternalClock();
        IReadOnlyList<IClock> clocks = [iterations.Clock, external, Clock.Elapsed];
        var analyzer = new MixedClockAnalyzer<AlgorithmObservation<RealVector, PopulationState<RealVector>>>(algorithm.Iterations, clocks);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42), [analyzer, algorithm.Iterations.Counting(iterations)]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var rows = run.Result(analyzer).Rows;
        rows.Count.ShouldBe(3);
        rows.Select(row => row[0].ShouldBeOfType<long>()).ShouldBe([1L, 2L, 3L]);
        rows.Select(row => row[1].ShouldBeOfType<int>()).ShouldBe([1, 2, 3]);
        rows.Select(row => row[2].ShouldBeOfType<TimeSpan>()).ShouldAllBe(time => time > TimeSpan.Zero);
        external.Bindings.ShouldBe(1);
        run.Result(iterations).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public async Task ACounterDrivesATerminatorAndClocksATrace()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);

        var evaluations = new Counter();
        var best = Analyzer.BestSoFar(algorithm.Evaluator, clocks: [evaluations.Clock, Clock.Elapsed]);
        var run = algorithm.Substitute(best, algorithm.Evaluator.Invocations.CountingCandidates(evaluations))
            .TerminatedBy(AfterOperatorCountTerminator.For(problem, evaluations, 48))
            .CreateRun(problem, RandomNumberGenerator.Create(42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var curve = run.Result(best).Select(point => new TracePoint<long, ObjectiveVector>(point.At(evaluations.Clock), point.Value)).ToArray();
        run.Result(evaluations).CurrentCount.ShouldBe(48);
        curve.ShouldNotBeEmpty();
        run.Result(best).Select(point => new TracePoint<TimeSpan, ObjectiveVector>(point.At(Clock.Elapsed), point.Value)).ShouldAllBe(point => point.Time > TimeSpan.Zero);
    }

    [Fact]
    public async Task AnAnalyzerIsPlacedExplicitlyWhereItIsWritten()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);

        var mutations = new Counter();
        var offspring = algorithm.Mutator.Invocations.Trace(observation => observation.Offspring.Count, [mutations.Clock]);
        var placed = algorithm with { Mutator = algorithm.Mutator.Counted(mutations).Observed(offspring) };
        var run = placed.CreateRun(problem, RandomNumberGenerator.Create(42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(offspring).By(mutations.Clock).Select(point => point.Time).ShouldBeInOrder();
        run.Result(mutations).CurrentCount.ShouldBe(run.Result(offspring).SampleCount);
    }

    // A one-line analyzer creates and updates plain state; Observed can place it explicitly too.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnAnalyzerOfYourOwnIsAttachedOrPlaced(bool attached)
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var sizes = algorithm.Mutator.Invocations.Aggregate(
            () => new Dictionary<int, int>(),
            (sizes, observation) => sizes[observation.Offspring.Count] = sizes.GetValueOrDefault(observation.Offspring.Count) + 1);
        var mutations = new Counter();

        IAlgorithm<RealVector, PopulationState<RealVector>> configured = attached
            ? algorithm.Substitute(sizes, algorithm.Mutator.Invocations.Counting(mutations))
            : (algorithm with { Mutator = algorithm.Mutator.Observed(sizes) }).Substitute(algorithm.Mutator.Invocations.Counting(mutations));
        var run = configured.CreateRun(problem, RandomNumberGenerator.Create(42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(mutations).CurrentCount.ShouldBeGreaterThan(0);
        run.Result(sizes).Value.Values.Sum().ShouldBe((int)run.Result(mutations).CurrentCount);
    }

    [Fact]
    public void ExternalReadOnlyClockBindsOnceAndRecordsEachRow()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 3 };
        var clock = new ExternalClock();
        var trace = algorithm.Iterations.Trace(observation => observation.Iteration, [clock]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), [trace]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        clock.Bindings.ShouldBe(1);
        run.Result(trace).Snapshot().Select(entry => entry.At(clock)).ShouldBe([1, 2, 3]);
        run.Result(trace).By(clock).Select(point => point.Time).ShouldBe([1, 2, 3]);
        Should.Throw<InvalidOperationException>(() => run.Result(trace).Latest!.Value.At(new ExternalClock()))
            .Message.ShouldBe("The trace does not use the requested clock.");
    }

    [Fact]
    public void EqualClockRecordsIdentifyOneAxisAndOneColumn()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 3 };
        var first = new ReadCountClock();
        var equal = new ReadCountClock();
        var single = algorithm.Iterations.Trace(observation => observation.Iteration, [first]);
        var duplicate = algorithm.Iterations.Trace(observation => observation.Iteration, [first, equal]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), [single, duplicate]);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(single).By(new ReadCountClock()).Select(point => point.Time).ShouldBe([2, 4, 6]);
        run.Result(duplicate).Snapshot().Select(entry => entry.At(equal)).ShouldBe([1, 3, 5]);
        // Only the first instance was read, once by each trace per observation.
        run.Result<IClockReading<int>>(first).Read().ShouldBe(7);
        run.Result<IClockReading<int>>(equal).Read().ShouldBe(1);
    }

    private sealed record ReadCountClock : IClock<int>
    {
        public IClockReading<int> CreateExecutionNode(ResolutionScope scope) => new Reading();

        private sealed class Reading : IClockReading<int>
        {
            private int count;

            public int Read() => Interlocked.Increment(ref count);
        }
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(TestFunctionProblem problem) =>
        new()
        {
            PopulationSize = 16,
            MaximumGenerations = 4,
            MutationRate = 1.0,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = new GaussianMutator(0.2, 0.15),
            Selector = TournamentSelector.For(problem, 2),
            Elites = 1
        };

    // This assembly has no internals access; clocks use the public configuration and reading contracts.
    private sealed class ClockRows : IRunResult
    {
        public List<object[]> Rows { get; } = [];
    }

    private sealed class MixedClockAnalyzer<TObservation>(ObservationSource<TObservation> source, IReadOnlyList<IClock> clocks)
        : Analyzer<ClockRows>, IObservationReceiver<TObservation>
    {
        protected override IReadOnlyList<ISubstitution> CreateSubstitutions() =>
            [source.RecordingInto(this)];

        public override ClockRows CreateExecutionNode(ResolutionScope scope) => new();

        public Action<TObservation> CreateRecorder(RunResults results)
        {
            var rows = results.Result(this);
            var readers = clocks.Select(clock => results.Result(clock)).ToArray();
            return _ => rows.Rows.Add([.. readers.Select(reading => reading.Read())]);
        }
    }

    private sealed class ExternalClock : IClock<int>
    {
        public int Bindings { get; private set; }

        public IClockReading<int> CreateExecutionNode(ResolutionScope scope)
        {
            Bindings++;
            return new Reading();
        }

        private sealed class Reading : IClockReading<int>
        {
            private int time;

            public int Read() => Interlocked.Increment(ref time);
        }
    }
}
