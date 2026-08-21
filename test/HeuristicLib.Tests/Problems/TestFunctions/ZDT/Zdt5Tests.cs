using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.ZDT;

public class Zdt5Tests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenDimensionIsLessThan35()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Zdt5(34));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDimensionDoesNotMatchRequiredEncoding()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Zdt5(36));
    }

    [Fact]
    public void Evaluate_ShouldReturnExpectedValue_ForMinimumValidAllZeroVector()
    {
        var problem = new Zdt5(35);
        var solution = new BoolVector(new bool[35]);
        var rng = DummyRandomNumberGenerator.Instance;

        var result = problem.Evaluate(solution, rng);

        // n = 35 -> numberOfVariables = 2
        // u0 = 0 => f1 = 1 + 0 = 1
        // u1 = 0 => v = 2 + 0 = 2
        // g = 2
        // h = 1 / f1 = 1
        // f2 = g * h = 2
        result[0].ShouldBe(1.0, 1e-12);
        result[1].ShouldBe(2.0, 1e-12);
    }

    [Fact]
    public void Evaluate_ShouldReturnExpectedValue_ForMinimumValidAllOneVector()
    {
        var problem = new Zdt5(35);
        var bits = new bool[35];
        Array.Fill(bits, true);
        var solution = new BoolVector(bits);
        var rng = DummyRandomNumberGenerator.Instance;

        var result = problem.Evaluate(solution, rng);

        // u0 = 30 => f1 = 31
        // u1 = 5 => v = 1
        // g = 1
        // h = 1 / 31
        // f2 = 1 / 31
        result[0].ShouldBe(31.0, 1e-12);
        result[1].ShouldBe(1.0 / 31.0, 1e-12);
    }

    [Fact]
    public void Evaluate_ShouldReturnExpectedValue_ForKnownMixedEncoding()
    {
        var problem = new Zdt5(40); // numberOfVariables = 3
        var bits = new bool[40];

        // first block (30 bits): set 3 ones
        bits[0] = true;
        bits[1] = true;
        bits[2] = true;

        // second block (5 bits at 30..34): set all 5 ones => contribution 1
        for (var i = 30; i < 35; i++)
        {
            bits[i] = true;
        }

        // third block (5 bits at 35..39): set 2 ones => contribution 2 + 2 = 4
        bits[35] = true;
        bits[36] = true;

        var solution = new BoolVector(bits);
        var rng = DummyRandomNumberGenerator.Instance;

        var result = problem.Evaluate(solution, rng);

        // u0 = 3 => f1 = 4
        // g = 1 + 4 = 5
        // h = 1 / 4
        // f2 = 5 / 4
        result[0].ShouldBe(4.0, 1e-12);
        result[1].ShouldBe(1.25, 1e-12);
    }

    [Fact]
    public void Evaluate_ShouldUseSpecialCase_WhenFiveBitBlockContainsFiveOnes()
    {
        var problem = new Zdt5(40); // 30 + 5 + 5
        var bits = new bool[40];

        // keep first block at one 1-bit so f1 = 2
        bits[0] = true;

        // first 5-bit block: all 5 ones => contribution 1
        for (var i = 30; i < 35; i++)
        {
            bits[i] = true;
        }

        // second 5-bit block: four ones => contribution 6
        for (var i = 35; i < 39; i++)
        {
            bits[i] = true;
        }

        var solution = new BoolVector(bits);
        var rng = DummyRandomNumberGenerator.Instance;

        var result = problem.Evaluate(solution, rng);

        // f1 = 1 + 1 = 2
        // g = 1 + (2 + 4) = 7
        // f2 = 7 * (1/2) = 3.5
        result[0].ShouldBe(2.0, 1e-12);
        result[1].ShouldBe(3.5, 1e-12);
    }
}
