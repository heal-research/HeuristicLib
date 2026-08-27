using System.Text;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;

namespace HEAL.HeuristicLib.Tests.Problems.QuadraticAssignment.InstanceLoading;

public sealed class QaplibParserTests
{
    [Fact]
    public void Parse_ShouldReadSizeFlowsAndDistances()
    {
        using var stream = ToStream("""
                                    3

                                    0 1 2
                                    3 0 4
                                    5 6 0

                                    0 10 20
                                    30 0 40
                                    50 60 0
                                    """);

        var parser = new QaplibParser(stream);
        parser.Parse();

        parser.Size.ShouldBe(3);
        parser.Flows[0, 1].ShouldBe(1);
        parser.Flows[2, 1].ShouldBe(6);
        parser.Distances[0, 2].ShouldBe(20);
        parser.Distances[2, 0].ShouldBe(50);
    }

    [Fact]
    public void Parse_ShouldReadMatrixAcrossLineBreaks()
    {
        using var stream = ToStream("""
                                    2
                                    0 1 2 0
                                    0 3
                                    4 0
                                    """);

        var parser = new QaplibParser(stream);
        parser.Parse();

        parser.Flows[1, 0].ShouldBe(2);
        parser.Distances[1, 0].ShouldBe(4);
    }

    [Fact]
    public void Parse_ShouldThrow_WhenMatrixIsIncomplete()
    {
        using var stream = ToStream("""
                                    2
                                    0 1
                                    1 0
                                    0 1
                                    """);

        var parser = new QaplibParser(stream);

        Should.Throw<InvalidDataException>(parser.Parse).Message.ShouldContain("distance matrix");
    }

    private static MemoryStream ToStream(string content) => new(Encoding.UTF8.GetBytes(content));
}
