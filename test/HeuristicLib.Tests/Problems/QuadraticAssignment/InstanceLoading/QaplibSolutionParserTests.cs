using System.Text;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;

namespace HEAL.HeuristicLib.Tests.Problems.QuadraticAssignment.InstanceLoading;

public sealed class QaplibSolutionParserTests
{
    [Fact]
    public void Parse_ShouldReadQualityAndAssignment_WhenValuesAreLocations()
    {
        using var stream = ToStream("""
                                    4 1234
                                    2 4 1 3
                                    """);

        var parser = new QaplibSolutionParser(stream);
        parser.Parse(valueAsLocation: false);

        parser.Size.ShouldBe(4);
        parser.Quality.ShouldBe(1234);
        parser.Assignment.ShouldBe([1, 3, 0, 2]);
    }

    [Fact]
    public void Parse_ShouldInvertAssignment_WhenValuesAreFacilities()
    {
        using var stream = ToStream("""
                                    4 1234.5
                                    2 4 1 3
                                    """);

        var parser = new QaplibSolutionParser(stream);
        parser.Parse(valueAsLocation: true);

        parser.Quality.ShouldBe(1234.5);
        parser.Assignment.ShouldBe([2, 0, 3, 1]);
    }

    [Fact]
    public void Parse_ShouldAcceptCommaSeparatedZeroBasedAssignments()
    {
        using var stream = ToStream("""
                                    4 1234
                                    1,3,0,2
                                    """);

        var parser = new QaplibSolutionParser(stream);
        parser.Parse(valueAsLocation: false);

        parser.Assignment.ShouldBe([1, 3, 0, 2]);
    }

    private static MemoryStream ToStream(string content) => new(Encoding.UTF8.GetBytes(content));
}
