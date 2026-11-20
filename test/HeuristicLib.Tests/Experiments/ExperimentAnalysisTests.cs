using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Tests.Experiments.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Experiments;

public class ExperimentAnalysisTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task RepeatedTrialsKeepIndependentResultsOfOneDefinition(int concurrency)
    {
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var calls = new Counter();
        var run = algorithm.Repeat(3).CreateRun(
            MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42), [algorithm.Evaluator.Invocations.Counting(calls)]);

        await run.CompleteAsync(ExecutionConcurrency.Concurrent(concurrency), cancellationToken: TestContext.Current.CancellationToken);

        run.Trials.Select(trial => trial.Repetition).ShouldBe([0, 1, 2]);
        run.Trials.ShouldAllBe(trial => trial.Run.Result(calls).CurrentCount == 1);
        run.Trials.Select(trial => trial.Run.Result(calls)).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    [Fact]
    public async Task ATrialResultIsReadableIndependentlyAsThatTrialFinishes()
    {
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var calls = new Counter();
        var run = algorithm.Substitute(algorithm.Evaluator.Invocations.Counting(calls)).Repeat(2).CreateRun(
            MetaAlgorithmTestHelpers.CreateIntegerProblem(), RandomNumberGenerator.Create(42));

        await run.Trials[0].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        run.Trials[0].Run.Result(calls).CurrentCount.ShouldBe(1);
        run.Trials[1].Run.Result(calls).CurrentCount.ShouldBe(0);

        await run.Trials[1].Run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        run.Trials[1].Run.Result(calls).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public async Task DefaultAnalyzerObservesTheEvaluatorOfEachParameterCandidate()
    {
        var algorithm = new AdditiveStepAlgorithm(1);
        var first = new ProblemEvaluator<int>();
        var second = new CountingResolutionEvaluator();
        var best = Analyzer.BestSoFar<int>();
        var grid = algorithm.AsParameterGrid().VaryBy(AdditiveStepAlgorithm.EvaluatorParameter, [first, second]);
        var experiment = Experiment.Create(grid, repetitions: 2);
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var run = experiment.CreateRun([problem, MetaAlgorithmTestHelpers.CreateIntegerProblem()], RandomNumberGenerator.Create(42), [best]);

        run.Trials[0].Algorithm.Evaluator.ShouldBeSameAs(first);
        run.Trials[2].Algorithm.Evaluator.ShouldBeSameAs(second);
        run.Trials.ShouldAllBe(trial => !ReferenceEquals(trial.Algorithm, trial.Run.Algorithm));
        foreach (var candidateTrials in run.Trials.GroupBy(trial => trial.ParameterCandidateIndex))
            candidateTrials.Select(trial => trial.Run.Algorithm).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(1);
        foreach (var trial in run.Trials)
            Should.Throw<InvalidOperationException>(() => trial.Algorithm.CreateRun(trial.Problem, RandomNumberGenerator.Create(42)).Result(best));

        await run.CompleteAsync(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

        run.Trials.ShouldAllBe(trial => trial.Run.Result(best).Count > 0);
        run.Trials.Select(trial => trial.Run.Result(best)).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(8);
    }

    [Fact]
    public void PlacementFailureNamesTheUnmatchedParameterCandidate()
    {
        var algorithm = ExperimentTestSupport.CreateGeneticAlgorithm();
        var problems = new[] { new TestFunctionProblem(new RastriginFunction(2)), new TestFunctionProblem(new RastriginFunction(3)) };
        var quality = algorithm.Iterations.TracePopulationQuality();
        var varied = algorithm.AsParameterGrid().VaryBy(GeneticAlgorithm<RealVector>.PopulationSizeParameter, [10]);
        var experiment = Experiment.Create([algorithm.AsParameterGrid(), varied], repetitions: 2);

        var failure = Should.Throw<ExperimentTrialException>(() => experiment.CreateRun(problems, RandomNumberGenerator.Create(42), [quality]));

        (failure.ProblemPosition, failure.ParameterCandidateIndex, failure.Repetition).ShouldBe((0, 1, 0));
        failure.Message.ShouldContain("problem 0, parameter candidate 1, repetition 0");
        failure.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldContain("selects nothing");
    }

    [Fact]
    public void AbsentAnalyzerFailsTheHeldCheckEvenWhenValidationIsDisabled()
    {
        var experiment = new AdditiveStepAlgorithm(1).Repeat(2);
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var best = Analyzer.BestSoFar<RealVector>();

        var failure = Should.Throw<ExperimentTrialException>(() =>
            experiment.CreateRun(problem, RandomNumberGenerator.Create(42), [best], validate: false));

        (failure.ProblemPosition, failure.ParameterCandidateIndex, failure.Repetition).ShouldBe((0, 0, 0));
        failure.InnerException.ShouldBeOfType<InvalidOperationException>().Message.ShouldContain("does not hold");
    }

    [Fact]
    public void HeldCheckFailureReportsTheAffectedRepetition()
    {
        var experiment = new ProbeAlgorithm(1).Repeat(3);
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var cause = new InvalidOperationException("Expansion failed during the second held check.");
        var substitution = new FailingHeldCheck(cause);

        var failure = Should.Throw<ExperimentTrialException>(() =>
            experiment.CreateRun(problem, RandomNumberGenerator.Create(42), [substitution]));

        (failure.ProblemPosition, failure.ParameterCandidateIndex, failure.Repetition).ShouldBe((0, 0, 1));
        failure.InnerException.ShouldBeSameAs(cause);
    }

    [Fact]
    public void ValidationRunsOncePerProblemAndParameterCandidateAcrossRepetitions()
    {
        var first = new ValidationProbeProblem();
        var second = new ValidationProbeProblem();
        var experiment = Experiment.Create([new ProbeAlgorithm(1).AsParameterGrid(), new ProbeAlgorithm(2).AsParameterGrid()], repetitions: 3);

        var run = experiment.CreateRun([first, second], RandomNumberGenerator.Create(42));

        run.Trials.Length.ShouldBe(12);
        first.SearchSpaceReads.ShouldBe(2);
        second.SearchSpaceReads.ShouldBe(2);
    }

    [Fact]
    public void ValidationChecksEachParameterCandidateAgainstItsProblemAndCanBeSkipped()
    {
        var wide = FuncProblem.Create((RealVector value) => value[0], new BoundedRealVectorSearchSpace(2, -10, 10), SingleObjective.Minimize);
        var narrow = FuncProblem.Create((RealVector value) => value[0], new BoundedRealVectorSearchSpace(2, -1, 1), SingleObjective.Minimize);
        var algorithm = ExperimentTestSupport.CreateGeneticAlgorithm();
        var grid = algorithm.AsParameterGrid()
            .VaryBy(GeneticAlgorithm<RealVector>.CreatorParameter, [new UniformDistributedCreator(), new UniformDistributedCreator(wide.SearchSpace)]);
        var experiment = Experiment.Create(grid, repetitions: 2);

        var failure = Should.Throw<ExperimentTrialException>(() => experiment.CreateRun([wide, narrow], RandomNumberGenerator.Create(42)));

        (failure.ProblemPosition, failure.ParameterCandidateIndex, failure.Repetition).ShouldBe((1, 1, 0));
        failure.Message.ShouldContain("problem 1, parameter candidate 1, repetition 0");
        failure.InnerException.ShouldBeOfType<InvalidOperationException>();

        var uncheckedRun = experiment.CreateRun([wide, narrow], RandomNumberGenerator.Create(42), validate: false);
        uncheckedRun.Trials.Length.ShouldBe(8);
        var rawRun = new ExperimentRun<RealVector, BoundedRealVectorSearchSpace, FuncProblem<RealVector, BoundedRealVectorSearchSpace>,
            PopulationState<RealVector>, GeneticAlgorithm<RealVector>>(experiment, narrow, RandomNumberGenerator.Create(42));
        rawRun.Trials.Length.ShouldBe(4);
    }

    private sealed class FailingHeldCheck(InvalidOperationException cause) : ISubstitution, IConfigurationNode
    {
        private int expansions;

        // Placement and the first trial expand successfully. The second trial's held check throws.
        public IReadOnlyList<Substitution> Expand()
        {
            if (++expansions == 3)
                throw cause;
            return [];
        }
    }

    private sealed class ValidationProbeProblem : IProblem<int, DummySearchSpace<int>>
    {
        public int SearchSpaceReads { get; private set; }
        public ObjectiveDirections Objective => SingleObjective.Minimize;

        public DummySearchSpace<int> SearchSpace
        {
            get
            {
                SearchSpaceReads++;
                return DummySearchSpace<int>.Instance;
            }
        }

        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
        {
            throw new NotSupportedException();
        }
    }
}
