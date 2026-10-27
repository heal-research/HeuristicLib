using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Creators.PermutationCreators;

public class PredefinedPermutationCreatorTests
{
    [Fact]
    public void Create_CyclesAcrossBatchesAndRebindingWhileIndependentScopesRestart()
    {
        Permutation first = [0, 1, 2];
        Permutation second = [2, 1, 0];
        var creator = new PredefinedPermutationCreator([first, second]);
        var problem = FuncProblem.Create((Permutation candidate) => candidate[0], new PermutationSearchSpace(3), SingleObjective.Minimize);
        var scope = ResolutionScope.Create().For<Permutation, PermutationSearchSpace, FuncProblem<Permutation, PermutationSearchSpace>>();
        var random = new SequenceRandomNumberGenerator();

        scope.Resolve(creator).Create(3, random, problem.SearchSpace, problem).ShouldBe([first, second, first]);
        scope.Resolve(creator).Create(2, random, problem.SearchSpace, problem).ShouldBe([second, first]);
        var independent = ResolutionScope.Create().For<Permutation, PermutationSearchSpace, FuncProblem<Permutation, PermutationSearchSpace>>();
        independent.Resolve(creator).Create(1, random, problem.SearchSpace, problem).ShouldBe([first]);
    }

    [Fact]
    public void Create_SamplesWithReplacementWhenWithRandomIsEnabled()
    {
        Permutation first = [0, 1, 2];
        Permutation second = [2, 1, 0];
        var creator = new PredefinedPermutationCreator([first, second]) { WithRandom = true };
        var problem = FuncProblem.Create((Permutation candidate) => candidate[0], new PermutationSearchSpace(3), SingleObjective.Minimize);
        var scope = ResolutionScope.Create().For<Permutation, PermutationSearchSpace, FuncProblem<Permutation, PermutationSearchSpace>>();

        scope.Resolve(creator).Create(4, new SequenceRandomNumberGenerator(0.9, 0.9, 0.1, 0.9), problem.SearchSpace, problem)
            .ShouldBe([second, second, first, second]);
    }

    [Fact]
    public void Configuration_SnapshotsInputAndUsesStructuralEquality()
    {
        Permutation[] input = [Permutation.Range(3)];
        var creator = new PredefinedPermutationCreator(input);
        input[0] = Permutation.Create(2, 1, 0);

        creator.PredefinedPermutations[0].ShouldBe(Permutation.Range(3));
        creator.ShouldBe(new PredefinedPermutationCreator([Permutation.Range(3)]));
        (creator with { WithRandom = true }).ShouldNotBe(creator);
    }

    [Fact]
    public void Preparation_RejectsAnEmptyList() =>
        Should.Throw<InvalidOperationException>(() => new PredefinedPermutationCreator([])
            .CreateExecutionFactory<PermutationSearchSpace, FuncProblem<Permutation, PermutationSearchSpace>>());

    [Fact]
    public void Create_RejectsInvalidRequestsBeforeRandomnessOrCursorChanges()
    {
        var first = Permutation.Range(3);
        var second = Permutation.Create(2, 1, 0);
        var creator = new PredefinedPermutationCreator([first, second]);
        var problem = FuncProblem.Create((Permutation candidate) => candidate[0], new PermutationSearchSpace(3), SingleObjective.Minimize);
        var scope = ResolutionScope.Create().For<Permutation, PermutationSearchSpace, FuncProblem<Permutation, PermutationSearchSpace>>();
        var execution = scope.Resolve(creator);
        var random = new SequenceRandomNumberGenerator();

        Should.Throw<ArgumentOutOfRangeException>(() => execution.Create(-1, random, problem.SearchSpace, problem));
        Should.Throw<ArgumentException>(() => execution.Create(1, random, new PermutationSearchSpace(4), problem));
        execution.Create(0, random, problem.SearchSpace, problem).ShouldBeEmpty();
        execution.Create(1, random, problem.SearchSpace, problem).ShouldBe([first]);

        var randomExecution = scope.Resolve(creator with { WithRandom = true });
        Should.Throw<ArgumentException>(() => randomExecution.Create(1, random, new PermutationSearchSpace(4), problem));
    }
}
