using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Tests.Architecture;

public sealed class AssemblyDependencyTests
{
    [Fact]
    public void MainAndContracts_DoNotReferenceExperimental()
    {
        var assemblies = new[]
        {
            typeof(Algorithm<,,,,>).Assembly,
            typeof(IAlgorithm<,,,>).Assembly
        };

        foreach (var assembly in assemblies)
        {
            assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name)
                .ShouldNotContain("HEAL.HeuristicLib.Experimental");
        }
    }
}
