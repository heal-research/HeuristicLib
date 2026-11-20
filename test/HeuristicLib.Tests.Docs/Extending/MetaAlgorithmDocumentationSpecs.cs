#region meta-algorithm-turns-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
#endregion meta-algorithm-turns-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

public sealed class MetaAlgorithmDocumentationSpecs
{
    [Fact]
    public void ANewProblemObjectGetsANewExecutorWhileTheScopeKeepsTheOldOne()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var subproblem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var anotherSubproblem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var random = RandomNumberGenerator.Create(seed: 42);

        #region new-problem-per-turn
        // The creator remembers how many of its predefined candidates it has already returned.
        var creator = PredefinedCandidatesCreator.Create<RealVector>(
            [new RealVector(1.0), new RealVector(2.0)], new UniformDistributedCreator());
        var scope = ResolutionScope.Create();

        var turn = scope.For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(subproblem);
        var executor = turn.Resolve(creator);
        var first = executor.Create(1, random)[0];

        // A new problem object gets a new executor with empty fields, and the scope keeps the first one.
        var forAnotherProblem = scope
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(anotherSubproblem)
            .Resolve(creator);
        var restarted = forAnotherProblem.Create(1, random)[0];

        // The same problem object returns the first executor, and its fields are where they were.
        var again = turn.Resolve(creator);
        var continued = again.Create(1, random)[0];

        // A child scope builds its own executor, which goes with the scope when the turn ends.
        var inChildScope = scope.CreateChildScope()
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(subproblem)
            .Resolve(creator);
        #endregion new-problem-per-turn

        first.ShouldBe(new RealVector(1.0));
        forAnotherProblem.ShouldNotBeSameAs(executor);
        restarted.ShouldBe(new RealVector(1.0));
        again.ShouldBeSameAs(executor);
        continued.ShouldBe(new RealVector(2.0));
        inChildScope.ShouldNotBeSameAs(executor);
    }

    [Fact]
    public async Task TwoStagesResolvedInOneScopeShareTheExecutorOfAConfigurationBothHold()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var creator = PredefinedCandidatesCreator.Create<RealVector>(
            [new RealVector(1.0), new RealVector(2.0)], new UniformDistributedCreator());
        var explore = new GeneticAlgorithm<RealVector>
        {
            Creator = creator,
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            PopulationSize = 1,
            MaximumGenerations = 1
        };
        var exploit = explore with { MutationRate = 0.5 };
        var scope = ResolutionScope.Create()
            .For<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>, PopulationState<RealVector>>(problem);

        var first = scope.Resolve(explore);
        var second = scope.Resolve(exploit);
        var firstState = await first.RunToCompletionAsync(RandomNumberGenerator.Create(seed: 42), ct: TestContext.Current.CancellationToken);
        var secondState = await second.RunToCompletionAsync(RandomNumberGenerator.Create(seed: 42), ct: TestContext.Current.CancellationToken);

        // The stages are different configurations, but both hold the creator by reference, so its one executor continues.
        second.ShouldNotBeSameAs(first);
        firstState.Population.EvaluatedCandidates[0].Candidate.ShouldBe(new RealVector(1.0));
        secondState.Population.EvaluatedCandidates[0].Candidate.ShouldBe(new RealVector(2.0));
    }

    [Fact]
    public void ANullMaximumOfCyclesIsUnlimitedAndANonpositiveOneRunsNoCycles()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        var inner = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            PopulationSize = 4,
            MaximumGenerations = 2
        };
        IEnumerable<PopulationState<RealVector>> innerStates = inner.RunStreaming(problem, RandomNumberGenerator.Create(seed: 42), ct: TestContext.Current.CancellationToken);
        var statesPerCycle = innerStates.Count();

        int StatesOf(int? maximumCycles, int limit)
        {
            IEnumerable<PopulationState<RealVector>> states =
                (CycleAlgorithm.Create(inner) with { MaximumCycles = maximumCycles })
                .RunStreaming(problem, RandomNumberGenerator.Create(seed: 42), ct: TestContext.Current.CancellationToken);
            return states.Take(limit).Count();
        }

        StatesOf(maximumCycles: 2, limit: 10 * statesPerCycle).ShouldBe(2 * statesPerCycle);
        StatesOf(maximumCycles: 0, limit: 10 * statesPerCycle).ShouldBe(0);
        StatesOf(maximumCycles: -1, limit: 10 * statesPerCycle).ShouldBe(0);
        StatesOf(maximumCycles: null, limit: 10 * statesPerCycle).ShouldBe(10 * statesPerCycle);
    }
}
