using HEAL.HeuristicLib.Problems.QuadraticAssignment;

namespace HEAL.HeuristicLib.Tests.Problems.QuadraticAssignment.InstanceLoading;

public sealed class QaplibQapInstanceProviderTests
{
    [Fact]
    public void LoadData_ShouldReadProblemAndSolutionFiles()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var problemFile = Path.Combine(directory.FullName, "sample.dat");
            var solutionFile = Path.Combine(directory.FullName, "sample.sln");
            File.WriteAllText(problemFile, """
                                           2
                                           0 1
                                           2 0
                                           0 3
                                           4 0
                                           """);
            File.WriteAllText(solutionFile, """
                                            2 8
                                            2 1
                                            """);

            var data = QaplibQapInstanceProvider.LoadData(problemFile, solutionFile);

            data.Name.ShouldBe("sample");
            data.Dimension.ShouldBe(2);
            data.Flows[1, 0].ShouldBe(2);
            data.Distances[0, 1].ShouldBe(3);
            data.BestKnownQuality.ShouldBe(8);
            data.BestKnownAssignment.ShouldBe([1, 0]);
            data.ToProblemData().Size.ShouldBe(2);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void LoadData_ShouldReverseKnownReversedSolutions()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var problemFile = Path.Combine(directory.FullName, "nug12.dat");
            var solutionFile = Path.Combine(directory.FullName, "nug12.sln");
            File.WriteAllText(problemFile, """
                                           3
                                           0 1 2
                                           3 0 4
                                           5 6 0
                                           0 10 20
                                           30 0 40
                                           50 60 0
                                           """);
            File.WriteAllText(solutionFile, """
                                            3 99
                                            2 3 1
                                            """);

            var data = QaplibQapInstanceProvider.LoadData(problemFile, solutionFile);

            data.BestKnownAssignment.ShouldBe([1, 2, 0]);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
