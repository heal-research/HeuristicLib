using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace HEAL.HeuristicLib.Tests.Architecture;

public class OperatorNamingTests {
  private static readonly ArchUnitNET.Domain. Architecture Architecture =
    new ArchLoader().LoadAssemblies(
      typeof(IAlgorithm<,,,>).Assembly  
    ).Build();
  
  // [Fact]
  // public void OperatorNamesAreEndingWithOperator() {
  //   var rule = Types()
  //     .That().ImplementInterface(typeof(IOperator))
  //     .Should().HaveName(".+Operator(`\\d+)?", useRegularExpressions: true);
  //     //.HaveNameEndingWith("Operator(`\\d+)?");
  //
  //   rule.Check(Architecture);
  // }

  // [Fact]
  // public void OperatorReferencesAreNotEndingWithOperator() {
  //   var rule = Types()
  //     .That().AreAssignableTo(typeof(OperatorName)).And().AreNot(typeof(OperatorName))
  //     .Should().NotHaveName(".+Operator(`\\d+)?", useRegularExpressions: true);
  //   
  //   rule.Check(Architecture);
  // }
}
