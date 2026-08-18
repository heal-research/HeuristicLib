using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Refiners;

public class ImprovementCheckingCompositionTests
{
    [Fact]
    public void OnAMaximizedObjective_TheHigherObjectiveVectorIsKept()
    {
        var problem = new FuncProblem<int, DummySearchSpace<int>>(static (int candidate) => (double)candidate, DummySearchSpace<int>.Instance, SingleObjective.Maximize);

        Refine(new AddOffsetRefiner(5).WithImprovementCheck(), problem, 10).ShouldBe([15]);
        Refine(new AddOffsetRefiner(-5).WithImprovementCheck(), problem, 10).ShouldBe([10]);
    }

    // Without a total order the default criterion falls back to dominance, which rejects the trade-off; a declared
    // order accepts the same one.
    [Fact]
    public void TheDefaultCriterion_FollowsTheProblemsObjectiveOrder()
    {
        var refiner = new AddOffsetRefiner(-5).WithImprovementCheck();

        // Lowering the candidate improves the first objective and worsens the second.
        Refine(refiner, CreateTradeOffProblem(MultiObjective.Create(ObjectiveDirection.Minimize, ObjectiveDirection.Minimize)), 10)
            .ShouldBe([10]);
        Refine(refiner, CreateTradeOffProblem(MultiObjective.WeightedSum([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], [1.0, 0.0])), 10)
            .ShouldBe([5]);
    }

    [Fact]
    public void AThresholdCriterion_MeasuresTheMarginAgainstTheProblemsObjective()
    {
        var problem = CreateProblem();

        Refine(new AddOffsetRefiner(-3).WithImprovementCheck(ImprovementChecking.MinimumImprovement(5.0)), problem, 10).ShouldBe([10]);
        Refine(new AddOffsetRefiner(-6).WithImprovementCheck(ImprovementChecking.MinimumImprovement(5.0)), problem, 10).ShouldBe([4]);
    }

    [Fact]
    public void ARelativeThresholdCriterion_ScalesTheMarginWithTheOriginalObjective()
    {
        var problem = CreateProblem();

        Refine(new AddOffsetRefiner(-4).WithImprovementCheck(ImprovementChecking.MinimumRelativeImprovement(0.5)), problem, 10).ShouldBe([10]);
        Refine(new AddOffsetRefiner(-6).WithImprovementCheck(ImprovementChecking.MinimumRelativeImprovement(0.5)), problem, 10).ShouldBe([4]);
    }

    // NSGA2 declares no total order, so acceptance is dominance-based and the worsened point never survives.
    [Fact]
    public void UnderNsga2_ADominatedRefinementNeverEntersThePopulation()
    {
        var problem = new MultiObjectiveTestFunctionProblem(new Zdt1(dimension: 3));
        var worsened = RealVector.Repeat(problem.SearchSpace.GetMaximum(0), problem.SearchSpace.Length);
        var algorithm = new NSGA2<RealVector, RealVectorSearchSpace, MultiObjectiveTestFunctionProblem>
        {
            PopulationSize = 6,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            Selector = RandomSelector.For(problem),
            Replacer = ParetoCrowdingReplacer.For(problem, dominateOnEqualities: true),
            MaximumGenerations = 3,
            Refiner = new UpperBoundRefiner().WithImprovementCheck()
        };

        var result = algorithm.Complete(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.ShouldAllBe(candidate => candidate.Candidate != worsened);
    }

    private static IReadOnlyList<int> Refine(
        IRefiner<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> refiner,
        FuncProblem<int, DummySearchSpace<int>> problem,
        params int[] candidates) =>
        refiner.CreateExecutionInstance().Refine(candidates, RandomNumberGenerator.Create(42), problem.SearchSpace, problem);

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => (double)candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    // Every change improves one objective exactly as much as it worsens the other, so only the declared order can decide.
    private static FuncProblem<int, DummySearchSpace<int>> CreateTradeOffProblem(ObjectiveDirections objective) =>
        new(static (int candidate) => new double[] { candidate, -candidate }, DummySearchSpace<int>.Instance, objective);

    private sealed record AddOffsetRefiner(int Offset) : SingleCandidateRefiner<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {
        public override int RefineCandidate(int candidate, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            candidate + Offset;
    }

    private sealed record UpperBoundRefiner : SingleCandidateRefiner<RealVector, RealVectorSearchSpace, MultiObjectiveTestFunctionProblem>
    {
        public override RealVector RefineCandidate(RealVector candidate, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, MultiObjectiveTestFunctionProblem problem) =>
            RealVector.Repeat(searchSpace.GetMaximum(0), candidate.Count);
    }
}
