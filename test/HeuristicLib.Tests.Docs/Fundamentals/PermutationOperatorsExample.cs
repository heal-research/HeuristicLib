using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Docs.Fundamentals;

public class PermutationOperatorsExample
{
    [Fact]
    public void ComposeAndExecutePermutationOperators()
    {
        #region permutation-operators
        var searchSpace = new PermutationSearchSpace(8);
        var problem = FuncProblem.Create((Permutation candidate) => candidate[0], searchSpace, SingleObjective.Minimize);
        var creator = new PredefinedPermutationCreator([
            Permutation.Range(8),
            Permutation.Create(3, 7, 5, 1, 6, 0, 2, 4)
        ]);
        var crossover = ChooseOneCrossover.Create<Permutation>([
            new OrderCrossover2(), new PartiallyMatchedCrossover(), new CyclicCrossover()
        ]);
        var mutator = ChooseOneMutator.Create<Permutation>([
            new InsertionMutator(), new ScrambleMutator(), new TranslocationInversionMutator()
        ]);

        var scope = ResolutionScope.Create()
            .For<Permutation, PermutationSearchSpace, FuncProblem<Permutation, PermutationSearchSpace>>();
        var random = RandomNumberGenerator.Create(42);
        var parents = scope.Resolve(creator).Create(2, random, searchSpace, problem);
        var offspring = scope.Resolve(crossover).Cross(
            [new Parents<Permutation>(parents[0], parents[1])], random, searchSpace, problem);
        var mutated = scope.Resolve(mutator).Mutate(offspring, random, searchSpace, problem);
        #endregion

        Assert.Single(mutated);
        Assert.True(searchSpace.Contains(mutated[0]));
    }
}
