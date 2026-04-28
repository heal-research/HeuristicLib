using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.ApiSpecs;

public class PractitionerUsageSpecs
{
  [Fact]
  public void StatelessOperators_CanBeInvokedDirectly_WithoutInstantiation()
  {
    var problem = CreateRastriginProblem(dimension: 3);
    var realSearchSpace = new RealVectorSearchSpace(3, new RealVector(-1.0), new RealVector(1.0));
    var integerSearchSpace = new IntegerVectorSearchSpace(3, new IntegerVector(-2), new IntegerVector(2));

    var randomIntegerVector = RandomNumberGenerator.Create(2025).NextIntegerVectorUniform(new IntegerVector(-2), new IntegerVector(2), length: 4);
    randomIntegerVector.Count.ShouldBe(4);
    randomIntegerVector.All(x => x is >= -2 and <= 2).ShouldBeTrue();

    var randomIntegerVectorFromSearchSpace = RandomNumberGenerator.Create(2025).NextIntegerVectorUniform(integerSearchSpace);
    randomIntegerVectorFromSearchSpace.Count.ShouldBe(3);
    randomIntegerVectorFromSearchSpace.All(x => x is >= -2 and <= 2).ShouldBeTrue();

    var randomRealVector = RandomNumberGenerator.Create(2025).NextRealVectorUniform(new RealVector(-0.5), new RealVector(0.5), length: 3);
    randomRealVector.Count.ShouldBe(3);
    randomRealVector.All(x => x is >= -0.5 and <= 0.5).ShouldBeTrue();

    var randomRealVectorFromSearchSpace = RandomNumberGenerator.Create(2025).NextRealVectorUniform(realSearchSpace);
    randomRealVectorFromSearchSpace.Count.ShouldBe(3);
    randomRealVectorFromSearchSpace.All(x => x is >= -1.0 and <= 1.0).ShouldBeTrue();

    RandomNumberGenerator.Create(2025).NextBools(3, probability: 0.0).ShouldBe([false, false, false]);
    RandomNumberGenerator.Create(2025).NextNormals(3, mu: 0.25, sigma: 0.0).ShouldBe([0.25, 0.25, 0.25]);

    var roundedVector = new RealVector(1.6, -2.8, 0.2).RoundToIntegerVector(new IntegerVector(-2), new IntegerVector(2));
    roundedVector.ShouldBe(new IntegerVector(2, -2, 0));

    var floorVector = new RealVector(1.6, -2.8, 0.2).FloorToIntegerVector(new IntegerVector(-2), new IntegerVector(2));
    floorVector.ShouldBe(new IntegerVector(1, -2, 0));

    var ceilingVector = new RealVector(1.2, -1.8, 0.2).CeilToIntegerVector(integerSearchSpace.Minimum, integerSearchSpace.Maximum);
    ceilingVector.ShouldBe(new IntegerVector(2, -1, 1));

    var ceiledRealVector = new RealVector(1.2, -1.8, 0.2).Ceil();
    ceiledRealVector.ShouldBe(new RealVector(2.0, -1.0, 1.0));

    var permutation = RandomPermutationCreator.Create(RandomNumberGenerator.Create(2026), 5);
    permutation.Count.ShouldBe(5);
    permutation.Order().ToArray().ShouldBe([0, 1, 2, 3, 4]);

    var directPermutation = RandomNumberGenerator.Create(2026).NextPermutation(5);
    directPermutation.Order().ToArray().ShouldBe([0, 1, 2, 3, 4]);

    var directPermutationFromSearchSpace = RandomNumberGenerator.Create(2026).NextPermutation(new PermutationSearchSpace(5));
    directPermutationFromSearchSpace.Order().ToArray().ShouldBe([0, 1, 2, 3, 4]);

    var integerVector = Operators.Creators.IntegerVectorCreators.UniformDistributedCreator.Create(
      RandomNumberGenerator.Create(2027),
      length: 4,
      minimum: -2,
      maximum: 2);
    integerVector.Count.ShouldBe(4);
    integerVector.All(x => x is >= -2 and <= 2).ShouldBeTrue();

    var searchSpaceIntegerVector = Operators.Creators.IntegerVectorCreators.UniformDistributedCreator.Create(
      integerSearchSpace,
      RandomNumberGenerator.Create(2027));
    searchSpaceIntegerVector.Count.ShouldBe(3);
    searchSpaceIntegerVector.All(x => x is >= -2 and <= 2).ShouldBeTrue();

    var boundedRealVector = UniformDistributedCreator.Create(
      RandomNumberGenerator.Create(2027),
      length: 3,
      minimum: new RealVector(-0.5),
      maximum: new RealVector(0.5));
    boundedRealVector.Count.ShouldBe(3);
    boundedRealVector.All(x => x is >= -0.5 and <= 0.5).ShouldBeTrue();

    var boundedRealVectorFromSearchSpace = UniformDistributedCreator.Create(
      RandomNumberGenerator.Create(2027),
      realSearchSpace,
      minimum: new RealVector(-0.5),
      maximum: new RealVector(0.5));
    boundedRealVectorFromSearchSpace.Count.ShouldBe(3);
    boundedRealVectorFromSearchSpace.All(x => x is >= -0.5 and <= 0.5).ShouldBeTrue();

    var normalRealVector = NormalDistributedCreator.Create(
      RandomNumberGenerator.Create(2028),
      length: 3,
      means: new RealVector(0.25),
      sigmas: new RealVector(0.0),
      minimum: new RealVector(-1.0),
      maximum: new RealVector(1.0));
    normalRealVector.ShouldBe(RealVector.Repeat(0.25, 3));

    var normalRealVectorFromSearchSpace = NormalDistributedCreator.Create(
      RandomNumberGenerator.Create(2028),
      realSearchSpace,
      means: new RealVector(0.25),
      sigmas: new RealVector(0.0));
    normalRealVectorFromSearchSpace.ShouldBe(RealVector.Repeat(0.25, 3));

    var randomNormalRealVectorFromSearchSpace = RandomNumberGenerator.Create(2028).NextRealVectorNormal(
      realSearchSpace,
      means: new RealVector(0.25),
      sigmas: new RealVector(0.0));
    randomNormalRealVectorFromSearchSpace.ShouldBe(RealVector.Repeat(0.25, 3));

    var normalIntegerVector = Operators.Creators.IntegerVectorCreators.NormalDistributedCreator.Create(
      RandomNumberGenerator.Create(2029),
      length: 3,
      means: new RealVector(1.6),
      sigmas: new RealVector(0.0),
      minimum: integerSearchSpace.Minimum,
      maximum: integerSearchSpace.Maximum);
    normalIntegerVector.ShouldBe(new IntegerVector(2, 2, 2));

    var normalIntegerVectorFromSearchSpace = Operators.Creators.IntegerVectorCreators.NormalDistributedCreator.Create(
      RandomNumberGenerator.Create(2029),
      integerSearchSpace,
      means: new RealVector(1.6),
      sigmas: new RealVector(0.0));
    normalIntegerVectorFromSearchSpace.ShouldBe(new IntegerVector(2, 2, 2));

    var randomNormalIntegerVectorFromSearchSpace = RandomNumberGenerator.Create(2029).NextIntegerVectorNormal(
      integerSearchSpace,
      means: new RealVector(1.6),
      sigmas: new RealVector(0.0));
    randomNormalIntegerVectorFromSearchSpace.ShouldBe(new IntegerVector(2, 2, 2));

    var parent = new RealVector(1.0, 2.0, 3.0);
    var otherParent = new RealVector(9.0, 9.0, 9.0);

    NoChangeMutator.Mutate(parent, RandomNumberGenerator.Create(2030)).ShouldBe(parent);
    var gaussianChild = GaussianMutator.Mutate(parent, RandomNumberGenerator.Create(2031), realSearchSpace, mutationRate: 1.0, mutationStrength: 100.0);
    realSearchSpace.Contains(gaussianChild).ShouldBeTrue();

    SelectFirstParentCrossover.Cross(new Parents<RealVector>(parent, otherParent), RandomNumberGenerator.Create(2032)).ShouldBe(parent);
    SelectSecondParentCrossover.Cross(new Parents<RealVector>(parent, otherParent), RandomNumberGenerator.Create(2032)).ShouldBe(otherParent);

    var edgeChild = EdgeRecombinationCrossover.Cross(
      new Permutation(0, 1, 2, 3),
      new Permutation(0, 2, 1, 3),
      RandomNumberGenerator.Create(2033));
    edgeChild.Order().ToArray().ShouldBe([0, 1, 2, 3]);

    var evaluations = DirectEvaluator.Evaluate([parent], RandomNumberGenerator.Create(2034), problem);
    evaluations.Count.ShouldBe(1);

    IReadOnlyList<ISolution<RealVector>> solutions = [
      new Solution<RealVector>(parent, new ObjectiveVector(2.0)),
      new Solution<RealVector>(otherParent, new ObjectiveVector(1.0))
    ];

    RandomSelector.Select(solutions, count: 2, RandomNumberGenerator.Create(2035)).Count.ShouldBe(2);
    ProportionalSelector.Select(solutions, problem.Objective, count: 2, RandomNumberGenerator.Create(2036), windowing: true).Count.ShouldBe(2);
    CommaSelectionReplacer.Replace(solutions, problem.Objective, count: 1).Single().ShouldBe(solutions[1]);

    IReadOnlyList<ISolution<RealVector>> offspring = [
      new Solution<RealVector>(new RealVector(5.0, 5.0, 5.0), new ObjectiveVector(0.5)),
      new Solution<RealVector>(new RealVector(7.0, 7.0, 7.0), new ObjectiveVector(3.0))
    ];
    var paretoReplacement = ParetoCrowdingReplacer.Replace(solutions, offspring, problem.Objective, count: 2, dominateOnEqualities: false);
    paretoReplacement.Select(solution => solution.ObjectiveVector[0]).Order().ToArray().ShouldBe([0.5, 1.0]);

    NeverTerminator.ShouldTerminate().ShouldBeFalse();

    var state = new SingleSolutionState<RealVector> {
      Population = Population.From([parent], [evaluations[0]])
    };
    IdentityInterceptor.Transform(state, previousState: null).ShouldBe(state);
  }

  [Fact(Explicit = true)]
  public async Task GeneticAlgorithm_BenchmarkExample_RunsToCompletion()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      PopulationSize = 24,
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Crossover = new AlphaBetaBlendCrossover(alpha: 0.7),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Selector = new TournamentSelector<RealVector>(tournamentSize: 2),
      MutationRate = 0.2,
      Elites = 1
    }.WithMaxIterations(8);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(123),
      ct: TestContext.Current.CancellationToken);

    finalState.Population.Solutions.Length.ShouldBe(24);
    finalState.Population.Solutions.All(solution => problem.SearchSpace.Contains(solution.Genotype)).ShouldBeTrue();
  }

  [Fact(Explicit = true)]
  public async Task HillClimber_BenchmarkExample_RunsToCompletion()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 4,
      MaxNeighbors = 12
    }.WithMaxIterations(10);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(321),
      ct: TestContext.Current.CancellationToken);

    problem.SearchSpace.Contains(finalState.Solution.Genotype).ShouldBeTrue();
  }

  [Fact(Explicit = true)]
  public async Task RepeatedExecution_Example_RunsEachRepetition()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 4,
      MaxNeighbors = 12
    }.WithMaxIterations(6);
    var repeated = new RepeatAlgorithm<
      RealVector,
      RealVectorSearchSpace,
      TestFunctionProblem,
      SingleSolutionState<RealVector>,
      TerminatableAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>> {
      Algorithm = algorithm,
      Repetitions = 3
    };

    var results = await MultiStreamAlgorithmExtensions.RunToCompletionAsync<
      RealVector,
      RealVectorSearchSpace,
      TestFunctionProblem,
      SingleSolutionState<RealVector>,
      TerminatableAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>,
      int>(
      repeated,
      problem,
      RandomNumberGenerator.Create(999),
      cancellationToken: TestContext.Current.CancellationToken);

    results.Count.ShouldBe(3);
    results.Select(result => result.Key).ShouldBe([0, 1, 2]);
    results.All(result => problem.SearchSpace.Contains(result.Value.Solution.Genotype)).ShouldBeTrue();
  }

  [Fact(Explicit = true)]
  public async Task EvolutionStrategy_BenchmarkExample_RunsToCompletion()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new EvolutionStrategy<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      PopulationSize = 8,
      NumberOfChildren = 8,
      Strategy = EvolutionStrategyType.Plus,
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Crossover = null,
      Selector = new TournamentSelector<RealVector>(tournamentSize: 2),
      InitialMutationStrength = 0.15
    }.WithMaxIterations(5);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(555),
      ct: TestContext.Current.CancellationToken);

    finalState.Population.Solutions.Length.ShouldBe(8);
    finalState.Population.Solutions.All(solution => problem.SearchSpace.Contains(solution.Genotype)).ShouldBeTrue();
  }

  private static TestFunctionProblem CreateRastriginProblem(int dimension)
  {
    return new TestFunctionProblem(new RastriginFunction(dimension));
  }
}
