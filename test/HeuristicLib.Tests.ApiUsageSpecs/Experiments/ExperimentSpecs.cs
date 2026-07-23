using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Experiments;

public class ExperimentSpecs
{
    [Fact]
    public async Task RepeatedExperiment_UsesIndependentAlgorithmRuns()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        IAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> algorithm = CreateSimpleHillClimber(problem).WithMaxIterations(6);
        var experiment = new RepeatedExperiment<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>,
            IAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>>(algorithm, 3);

        var results = await experiment.CompleteAsync(problem, RandomNumberGenerator.Create(999), cancellationToken: TestContext.Current.CancellationToken);

        results.Count.ShouldBe(3);
        results.Select(result => result.Trial.Key).ShouldBe([0, 1, 2]);
        results.All(result => problem.SearchSpace.Contains(result.State.EvaluatedCandidate.Candidate)).ShouldBeTrue();
        results.Select(result => result.Trial.Run).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    [Fact]
    public void GridAndRepetition_ComposeWithTypedDeterministicKeys()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var grid = new GridExperiment<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>,
            HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>>(CreateSimpleHillClimber(problem))
            .VaryBy([2, 4], (algorithm, batchSize) => algorithm with { BatchSize = batchSize })
            .VaryBy([8, 12], (algorithm, maximumNeighbors) => algorithm with { MaxNeighbors = maximumNeighbors });
        var experiment = new RepeatedExperiment<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>,
            HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>, HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>>(grid, 2);

        var run = experiment.CreateRun(problem, RandomNumberGenerator.Create(123));

        run.Trials.Count.ShouldBe(8);
        run.Trials.Select(trial => (trial.Key.Inner.BatchSize, trial.Key.Inner.MaxNeighbors, trial.Key.Repetition)).ShouldBe([
            (2, 8, 0), (2, 8, 1), (2, 12, 0), (2, 12, 1),
            (4, 8, 0), (4, 8, 1), (4, 12, 0), (4, 12, 1)
        ]);
    }

    [Fact]
    public async Task ExperimentAnalysis_BindsOneAnalyzerToEachConcreteAlgorithm()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var experiment = new GridExperiment<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>,
            HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>>(CreateSimpleHillClimber(problem))
            .VaryBy([4, 8], (algorithm, maximumNeighbors) => algorithm with { MaxNeighbors = maximumNeighbors });
        var bestQuality = ExperimentAnalysis.ForEach(
            (HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> algorithm) => algorithm.Evaluator,
            evaluator => Analyzer.BestQuality(evaluator));
        var run = experiment.CreateRun(problem, RandomNumberGenerator.Create(456)).WithAnalysis(bestQuality);

        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var results = run.GetResults(bestQuality);

        results.Count.ShouldBe(2);
        results.Select(result => result.Trial.Key.MaxNeighbors).ShouldBe([4, 8]);
        results.All(result => result.Result.CurrentScore is not null).ShouldBeTrue();
    }

    private static TestFunctionProblem CreateRastriginProblem(int dimension) => new(new RastriginFunction(dimension));

    private static HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateSimpleHillClimber(TestFunctionProblem problem) => new()
    {
        Creator = new UniformDistributedCreator(problem.SearchSpace),
        Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
        Direction = LocalSearchDirection.FirstImprovement,
        BatchSize = 4,
        MaxNeighbors = 12
    };
}
