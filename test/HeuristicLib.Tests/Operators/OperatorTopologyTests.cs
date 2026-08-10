using System.Reflection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;

namespace HEAL.HeuristicLib.Tests.Operators;

public class OperatorTopologyTests
{
    [Fact]
    public void MutatorConfigurations_RetainedChildMutatorsArePubliclyInspectable()
    {
        var hiddenChildProperties = typeof(Mutator<,,>).Assembly
            .GetTypes()
            .Where(type => typeof(IOperator).IsAssignableFrom(type))
            .Where(IsMutatorType)
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

    [Fact]
    public void MutatorExecutionInstances_DoNotPubliclyExposeChildMachinery()
    {
        var publicChildProperties = typeof(Mutator<,,>).Assembly
            .GetTypes()
            .Where(type => typeof(IOperatorInstance).IsAssignableFrom(type))
            .Where(IsMutatorType)
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.Name.StartsWith("Child", StringComparison.Ordinal))
            .Select(property => $"{property.DeclaringType}.{property.Name}")
            .ToArray();

        publicChildProperties.ShouldBeEmpty();
    }

    private static bool IsMutatorType(Type type) =>
        type.Namespace?.StartsWith("HEAL.HeuristicLib.Operators.Mutators", StringComparison.Ordinal) == true;
}
