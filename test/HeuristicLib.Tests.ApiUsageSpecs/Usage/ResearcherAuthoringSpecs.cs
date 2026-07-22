using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Usage;

public class ResearcherAuthoringSpecs
{
    [Fact]
    public async Task CustomMutator_AuthoringExample_RunsInHillClimber()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new PullTowardZeroMutator(),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 4,
            MaxNeighbors = 12
        }.WithMaxIterations(5);

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(1234),
          ct: TestContext.Current.CancellationToken);

        problem.SearchSpace.Contains(finalState.EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    [Fact]
    public async Task CustomTerminator_AuthoringExample_CanStopAlgorithm()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var innerAlgorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 4,
            MaxNeighbors = 12
        };

        var algorithm = new StateTerminatedAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
        {
            Algorithm = innerAlgorithm,
            Terminator = new FirstEvaluatedStateTerminator()
        };

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(4321),
          ct: TestContext.Current.CancellationToken);

        problem.SearchSpace.Contains(finalState.EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    [Fact]
    public void CancellationTokenTerminator_Example_StopsGracefullyAfterProducedState()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        using var stopAfterCurrentState = new CancellationTokenSource();
        stopAfterCurrentState.Cancel();
        var innerAlgorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 4,
            MaxNeighbors = 12
        };

        var algorithm = new StateTerminatedAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
        {
            Algorithm = innerAlgorithm,
            Terminator = new CancellationTokenTerminator<RealVector>(stopAfterCurrentState.Token)
        };

        var states = algorithm.Stream(
          problem,
          RandomNumberGenerator.Create(2468),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Count.ShouldBe(1);
        problem.SearchSpace.Contains(states.Single().EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    [Fact]
    public void AfterElapsedTimeTerminator_Example_StopsGracefullyAfterProducedState()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var timeProvider = new AdvancingTimeProvider(TimeSpan.FromSeconds(2));
        var innerAlgorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 4,
            MaxNeighbors = 12
        };

        var algorithm = new StateTerminatedAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
        {
            Algorithm = innerAlgorithm,
            Terminator = new AfterElapsedTimeTerminator<RealVector>(
              TimeSpan.FromSeconds(1),
              timeProvider)
        };

        var states = algorithm.Stream(
          problem,
          RandomNumberGenerator.Create(8642),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Count.ShouldBe(1);
        problem.SearchSpace.Contains(states.Single().EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    [Fact]
    public async Task ProblemSpecificOperator_AuthoringExample_CanUseProblemType()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new TestFunctionOriginCreator(),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 4,
            MaxNeighbors = 12
        };

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(9876),
          ct: TestContext.Current.CancellationToken);

        finalState.EvaluatedCandidate.Candidate.ShouldBe(RealVector.Repeat(0.0, problem.TestFunction.Dimension));
    }

    private static TestFunctionProblem CreateRastriginProblem(int dimension)
    {
        return new TestFunctionProblem(new RastriginFunction(dimension));
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
            var moved = new RealVector(parent.Select(x => x * 0.5));
            return RealVector.Clamp(moved, searchSpace.Minimum, searchSpace.Maximum);
        }
    }

    private sealed record FirstEvaluatedStateTerminator
      : StatelessTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public override bool IsTerminalState(
          SingleSolutionState<RealVector> state,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return state.EvaluatedCandidate.ObjectiveVector[0] >= 0.0;
        }
    }

    private sealed record TestFunctionOriginCreator
      : SingleSolutionCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override RealVector Create(
          IRandomNumberGenerator random,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            return RealVector.Repeat(0.0, problem.TestFunction.Dimension);
        }
    }

    private sealed class AdvancingTimeProvider(TimeSpan step) : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            var current = timestamp;
            timestamp += step.Ticks;
            return current;
        }
    }
}
