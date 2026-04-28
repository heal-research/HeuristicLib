using HEAL.HeuristicLib.Genotypes.Vectors;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Creators.IntegerVectorCreators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.Mocks;

namespace HEAL.HeuristicLib.Tests;

public class OperatorStaticMethodGuidelineTests
{
  [Fact]
  public void TournamentSelector_InstanceAndStaticProduceSameSelection()
  {
    var population = CreateSingleObjectivePopulation();
    var objective = CreateSingleObjective();

    var viaInstance = new TournamentSelector<int>(tournamentSize: 3)
      .Select(population, objective, count: 4, RandomNumberGenerator.Create(123));
    var viaStatic = TournamentSelector.Select(population, objective, count: 4, RandomNumberGenerator.Create(123), tournamentSize: 3);

    viaStatic.ShouldBe(viaInstance);
  }

  [Fact]
  public void ParetoCrowdingTournamentSelector_InstanceAndStaticProduceSameSelection()
  {
    IReadOnlyList<ISolution<int>> population = [
      new Solution<int>(0, new ObjectiveVector(0.0, 10.0)),
      new Solution<int>(1, new ObjectiveVector(10.0, 0.0)),
      new Solution<int>(2, new ObjectiveVector(2.0, 9.0)),
      new Solution<int>(3, new ObjectiveVector(9.0, 2.0)),
      new Solution<int>(4, new ObjectiveVector(5.0, 5.0))
    ];
    var objective = CreateBiObjective();

    var viaInstance = new ParetoCrowdingTournamentSelector<int>(dominateOnEqualities: false, tournamentSize: 3)
      .Select(population, objective, count: 4, RandomNumberGenerator.Create(456));
    var viaStatic = ParetoCrowdingTournamentSelector.Select(population, objective, count: 4, RandomNumberGenerator.Create(456), dominateOnEqualities: false, tournamentSize: 3);

    viaStatic.ShouldBe(viaInstance);
  }

  [Fact]
  public void TargetTerminator_StaticMethodMatchesInstanceBehavior()
  {
    var objective = CreateSingleObjective();
    var problem = new DummyProblem<int>(DummySearchSpace<int>.Instance, objective);
    var state = new PopulationState<int> {
      Population = Population.From<int>([
        new Solution<int>(1, new ObjectiveVector(2.0)),
        new Solution<int>(2, new ObjectiveVector(0.5))
      ])
    };
    var target = new ObjectiveVector(1.0);

    var viaInstance = new TargetTerminator<int>(target).ShouldTerminate(state, DummySearchSpace<int>.Instance, problem);
    var viaCore = TargetTerminator.ShouldTerminate(state, problem, target);

    viaCore.ShouldBe(viaInstance);
  }

  [Fact]
  public void RemoveDuplicatesInterceptor_StaticOverloadMatchesInstanceBehavior()
  {
    var state = new PopulationState<string> {
      Population = Population.From<string>([
        new Solution<string>("A", new ObjectiveVector(1.0)),
        new Solution<string>("a", new ObjectiveVector(2.0)),
        new Solution<string>("B", new ObjectiveVector(3.0))
      ])
    };
    var comparer = CaseInsensitiveStringComparer.Instance;

    var viaInstance = new RemoveDuplicatesInterceptor<string, PopulationState<string>>(comparer).Transform(state, previousState: null);
    var viaStatic = RemoveDuplicatesInterceptor.Transform(state, previousState: null, comparer);

    viaStatic.Population.ShouldBe(viaInstance.Population);
    viaStatic.Population.Select(x => x.Genotype).ShouldBe(["A", "B"]);
  }

  [Fact]
  public void EdgeRecombinationCrossover_StaticMethodProducesPermutation()
  {
    var child = EdgeRecombinationCrossover.Cross(
      new Permutation(0, 1, 2, 3),
      new Permutation(0, 2, 1, 3),
      RandomNumberGenerator.Create(789));

    child.Order().ToArray().ShouldBe([0, 1, 2, 3]);
  }

  [Fact]
  public void ChangedOperatorRecords_ExposeOnlyImmutableConfigurationProperties()
  {
    AssertImmutableDeclaredProperties(typeof(Operators.Creators.RealVectorCreators.NormalDistributedCreator));
    AssertImmutableDeclaredProperties(typeof(Operators.Creators.IntegerVectorCreators.NormalDistributedCreator));
    AssertImmutableDeclaredProperties(typeof(BalancedTreeCreator));
    AssertImmutableDeclaredProperties(typeof(SelfAdaptiveSimulatedBinaryCrossover));
    AssertImmutableDeclaredProperties(typeof(SubtreeCrossover));
    AssertImmutableDeclaredProperties(typeof(TournamentSelector<int>));
    AssertImmutableDeclaredProperties(typeof(ParetoCrowdingTournamentSelector<int>));
    AssertImmutableDeclaredProperties(typeof(ProportionalSelector<int>));
    AssertImmutableDeclaredProperties(typeof(TargetTerminator<int>));
    AssertImmutableDeclaredProperties(typeof(RemoveDuplicatesInterceptor<string, PopulationState<string>>));
  }

  private static IReadOnlyList<ISolution<int>> CreateSingleObjectivePopulation()
  {
    return [
      new Solution<int>(0, new ObjectiveVector(4.0)),
      new Solution<int>(1, new ObjectiveVector(2.0)),
      new Solution<int>(2, new ObjectiveVector(1.0)),
      new Solution<int>(3, new ObjectiveVector(3.0))
    ];
  }

  private static Objective CreateSingleObjective()
    => new([ObjectiveDirection.Minimize], Comparer<ObjectiveVector>.Create((left, right) => left[0].CompareTo(right[0])));

  private static Objective CreateBiObjective()
    => new(
      [ObjectiveDirection.Minimize, ObjectiveDirection.Minimize],
      Comparer<ObjectiveVector>.Create((left, right) => {
        var first = left[0].CompareTo(right[0]);
        return first != 0 ? first : left[1].CompareTo(right[1]);
      }));

  private sealed class DummyProblem<TGenotype>(ISearchSpace<TGenotype> searchSpace, Objective objective)
    : IProblem<TGenotype, ISearchSpace<TGenotype>>
  {
    public ISearchSpace<TGenotype> SearchSpace { get; } = searchSpace;
    public Objective Objective { get; } = objective;

    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random)
      => throw new NotSupportedException();
  }

  private sealed class CaseInsensitiveStringComparer : IEqualityComparer<string>
  {
    public static readonly CaseInsensitiveStringComparer Instance = new();

    public bool Equals(string? x, string? y)
      => StringComparer.OrdinalIgnoreCase.Equals(x, y);

    public int GetHashCode(string obj)
      => StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
  }

  private static void AssertImmutableDeclaredProperties(Type type)
  {
    var mutableProperties = type
                            .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
                            .Where(property => property.SetMethod is not null && !IsInitOnly(property))
                            .Select(property => property.Name)
                            .ToArray();

    mutableProperties.ShouldBeEmpty();
  }

  private static bool IsInitOnly(System.Reflection.PropertyInfo property)
  {
    return property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)) == true;
  }
}
