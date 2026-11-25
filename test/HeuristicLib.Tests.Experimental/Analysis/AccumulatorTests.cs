using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Covers what the accumulators behind the experimental analyzers collect. They are mutable and read once a run has
/// finished, so these tests drive them directly rather than through a run.
/// </summary>
public class AccumulatorTests
{
    private static readonly ObjectiveDirections TwoMinimized =
        new([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], NoTotalOrderComparer.Instance);

    [Fact]
    public void AParetoFront_KeepsWhatIsNotDominated()
    {
        var front = new ParetoFront<int>(new ObjectiveVector(9, 9), TwoMinimized);

        front.AddPoints([new EvaluatedCandidate<int>(1, new ObjectiveVector(1, 4))]).ShouldBeTrue();
        front.AddPoints([new EvaluatedCandidate<int>(2, new ObjectiveVector(4, 1))]).ShouldBeTrue();

        // Dominated by both of the above, so the front does not move.
        front.AddPoints([new EvaluatedCandidate<int>(3, new ObjectiveVector(8, 8))]).ShouldBeFalse();

        front.Points.Select(evaluated => evaluated.Candidate).ShouldBe([1, 2], ignoreOrder: true);
        front.Count.ShouldBe(2);
    }

    [Fact]
    public void AGenealogyGraph_RecordsDescentInBothDirections()
    {
        var graph = new GenealogyGraph<int>(EqualityComparer<int>.Default);
        graph.SetAsNewGeneration([1, 2]);
        graph.AddConnection([1, 2], 3);
        graph.SetAsNewGeneration([3]);
        graph.AddConnection([3], 5);

        var root = graph.Generation(1).Single(node => node.Value == 1);
        var leaf = graph.Generation(2).Single(node => node.Value == 5);

        root.Descendants().Select(node => node.Value).ShouldContain(5);
        leaf.Ancestors().Select(node => node.Value).ShouldBe([3, 3, 1, 2], ignoreOrder: true);
        root.Children.Single().Value.ShouldBe(3);
        leaf.Parents.Single().Value.ShouldBe(3);
    }

    [Fact]
    public void AGenealogyGraph_ClosesOneGenerationPerSurvivingPopulation()
    {
        var graph = new GenealogyGraph<int>(EqualityComparer<int>.Default);
        graph.SetAsNewGeneration([1, 2]);
        graph.AddConnection([1, 2], 3);

        // The offspring joins the generation its parents are in, and the next call closes it.
        graph.GenerationCount.ShouldBe(2);
        graph.CurrentGeneration.Count.ShouldBe(3);

        graph.SetAsNewGeneration([3]);
        graph.GenerationCount.ShouldBe(3);
        graph.CurrentGeneration.Single().Value.ShouldBe(3);
    }

    [Fact]
    public void AGenealogyGraph_RendersEveryGenerationItHolds()
    {
        var graph = new GenealogyGraph<int>(EqualityComparer<int>.Default);
        graph.SetAsNewGeneration([1, 2]);
        graph.AddConnection([1, 2], 3);
        graph.SetAsNewGeneration([3]);

        var dot = graph.ToGraphViz();

        dot.ShouldStartWith("digraph G {");
        dot.ShouldContain("label=\"Generation 1\"");
        dot.ShouldContain("label=\"Generation 2\"");
    }
}
