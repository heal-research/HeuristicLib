using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

// Instruments define what a run collects, such as a counter or a trace. Attach them with Advised or place them
// with a wrapper where they are written, and read each run's own result from the run.
public class InstrumentSpecs
{
    [Fact]
    public async Task ACounterDrivesATerminatorAndClocksATrace()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);

        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var best = algorithm.Evaluator.Invocations.TraceBestSoFar(clocks: [evaluations, Clock.Elapsed]);
        var run = algorithm.Advised(best)
            .TerminatedBy(AfterOperatorCountTerminator.For(problem, evaluations, 48))
            .CreateRun(problem, RandomNumberGenerator.Create(42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var curve = run.Result(best).By(evaluations);
        run.Result(evaluations).CurrentCount.ShouldBe(48);
        curve.ShouldNotBeEmpty();
        run.Result(best).By(Clock.Elapsed).ShouldAllBe(point => point.Time > TimeSpan.Zero);
    }

    [Fact]
    public async Task AnInstrumentIsPlacedExplicitlyWhereItIsWritten()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);

        var mutations = algorithm.Mutator.Invocations.Count();
        var offspring = algorithm.Mutator.Invocations.Trace(observation => observation.Offspring.Count, [mutations]);
        var placed = algorithm with { Mutator = algorithm.Mutator.Counted(mutations).Observed(offspring) };
        var run = placed.CreateRun(problem, RandomNumberGenerator.Create(42));

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(offspring).By(mutations).Select(point => point.Time).ShouldBeInOrder();
        run.Result(mutations).CurrentCount.ShouldBe(run.Result(offspring).SampleCount);
    }

    // An instrument of your own derives from Instrument<TResult>, creates its aspect from the library's
    // sources and implements IObservationInstrument, so the library's Observed wrappers can place it too.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnInstrumentOfYourOwnIsAttachedOrPlaced(bool attached)
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var sizes = new OffspringSizes(algorithm.Mutator);
        var mutations = algorithm.Mutator.Invocations.Count();

        IAlgorithm<RealVector, PopulationState<RealVector>> configured = attached
            ? algorithm.Advised(sizes, mutations)
            : (algorithm with { Mutator = algorithm.Mutator.Observed(sizes) }).Advised(mutations);
        var run = configured.CreateRun(problem, RandomNumberGenerator.Create(42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(mutations).CurrentCount.ShouldBeGreaterThan(0);
        run.Result(sizes).Values.Sum().ShouldBe((int)run.Result(mutations).CurrentCount);
    }

    // A node of your own attaches instruments the way Advised does, through the scope it is resolved in.
    [Fact]
    public async Task ANodeOfYourOwnAttachesInstruments()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(4));
        var algorithm = CreateAlgorithm(problem);
        var sizes = new OffspringSizes(algorithm.Mutator);
        var mutations = algorithm.Mutator.Invocations.Count();

        var measured = new Measured<RealVector, PopulationState<RealVector>>(new Measured<RealVector, PopulationState<RealVector>>(algorithm, sizes), mutations);
        var run = measured.CreateRun(problem, RandomNumberGenerator.Create(42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(mutations).CurrentCount.ShouldBeGreaterThan(0);
        run.Result(sizes).Values.Sum().ShouldBe((int)run.Result(mutations).CurrentCount);
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

    // Counts how often each number of offspring was mutated in one call.
    private sealed class OffspringSizes(IOperator<Mutation, RealVector> mutator)
        : Instrument<Dictionary<int, int>>, IObservationInstrument<MutationObservation<RealVector>>
    {
        protected override IReadOnlyList<Substitution> CreateSubstitutions() => [mutator.Invocations.RecordingInto(this)];

        public Action<MutationObservation<RealVector>> CreateRecorder(RunResults results) => Recorder(results.Result(this));

        protected override Dictionary<int, int> CreateResult(RunResults results) => [];

        // The aspect records into the result it receives, which the run publishes together with the aspect.
        protected override Aspect CreateAspect(Dictionary<int, int> result, RunResults results) =>
            Aspect.Observe(mutator.Invocations, Recorder(result));

        private static Action<MutationObservation<RealVector>> Recorder(Dictionary<int, int> sizes) =>
            observation =>
            {
                lock (sizes)
                    sizes[observation.Offspring.Count] = sizes.GetValueOrDefault(observation.Offspring.Count) + 1;
            };
    }

    // Attaches its instruments to the scope it is resolved in, then resolves its child there.
    private sealed record Measured<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> ChildAlgorithm, Instrument Instrument)
        : WrappingAlgorithm<TCandidate, TSearchState>(ChildAlgorithm)
        where TSearchState : class, ISearchState
    {
        protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        {
            scope.AddAspect(scope.Results.Aspect(Instrument));
            return scope.Resolve(ChildAlgorithm);
        }
    }
}
