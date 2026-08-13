using System.Reflection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;

namespace HEAL.HeuristicLib.Tests.Operators;

public class OperatorTopologyTests
{
    [Theory]
    [InlineData(typeof(IMutator<,,>))]
    [InlineData(typeof(IMutatorInstance<,,>))]
    [InlineData(typeof(ISelector<,,>))]
    [InlineData(typeof(ISelectorInstance<,,>))]
    [InlineData(typeof(ICrossover<,,>))]
    [InlineData(typeof(ICrossoverInstance<,,>))]
    [InlineData(typeof(ICreator<,,>))]
    [InlineData(typeof(ICreatorInstance<,,>))]
    public void OperatorRoleContracts_PreserveCandidateAndUseContravariantContext(Type roleContract)
    {
        var typeParameters = roleContract.GetGenericArguments();

        (typeParameters[0].GenericParameterAttributes & GenericParameterAttributes.VarianceMask)
            .ShouldBe(GenericParameterAttributes.None);
        (typeParameters[1].GenericParameterAttributes & GenericParameterAttributes.VarianceMask)
            .ShouldBe(GenericParameterAttributes.Contravariant);
        (typeParameters[2].GenericParameterAttributes & GenericParameterAttributes.VarianceMask)
            .ShouldBe(GenericParameterAttributes.Contravariant);
    }

    [Theory]
    [InlineData("HEAL.HeuristicLib.Operators.Mutators")]
    [InlineData("HEAL.HeuristicLib.Operators.Selectors")]
    [InlineData("HEAL.HeuristicLib.Operators.Crossovers")]
    [InlineData("HEAL.HeuristicLib.Operators.Creators")]
    public void OperatorConfigurations_RetainedChildOperatorsArePubliclyInspectable(string roleNamespace)
    {
        var hiddenChildProperties = OperatorTypesIn(roleNamespace)
            .Where(type => typeof(IOperator).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly))
            .Where(property => property.Name.StartsWith("Child", StringComparison.Ordinal))
            .Where(property => property.GetMethod is not { IsPublic: true })
            .Select(property => $"{property.DeclaringType}.{property.Name}")
            .ToArray();

        hiddenChildProperties.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("HEAL.HeuristicLib.Operators.Mutators")]
    [InlineData("HEAL.HeuristicLib.Operators.Selectors")]
    [InlineData("HEAL.HeuristicLib.Operators.Crossovers")]
    [InlineData("HEAL.HeuristicLib.Operators.Creators")]
    public void OperatorExecutionInstances_DoNotPubliclyExposeChildMachinery(string roleNamespace)
    {
        var publicChildProperties = OperatorTypesIn(roleNamespace)
            .Where(type => typeof(IOperatorInstance).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.Name.StartsWith("Child", StringComparison.Ordinal))
            .Select(property => $"{property.DeclaringType}.{property.Name}")
            .ToArray();

        publicChildProperties.ShouldBeEmpty();
    }

    private static IEnumerable<Type> OperatorTypesIn(string roleNamespace) =>
        typeof(Mutator<,,>).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith(roleNamespace, StringComparison.Ordinal) == true);
}
