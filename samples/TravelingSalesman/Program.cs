using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

// A problem that states its own operator preferences configures an algorithm on its own: no operators are supplied
// here, and the crossover, mutator and creator come from the problem first and its encoding for whatever it declines.
var problem = new TravelingSalesmanProblem();
var cityCount = problem.SearchSpace.Length;

var algorithm = GeneticAlgorithm.For(problem, populationSize: 100, maximumGenerations: 300);

// A run pairs the configuration with a problem and a random source. Analyzers attach to the run, not to the
// algorithm, so the same configuration can be run again under different observation.
var run = algorithm
    .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .TrackBestMedianWorst(out var quality);

var finalState = await run.CompleteAsync();

var generations = run.GetResult(quality);
var bestTour = finalState.Population.EvaluatedCandidates
    .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;

Console.WriteLine($"Cities: {cityCount}   Generations: {generations.Count}");
Console.WriteLine();
Console.WriteLine($"{"Generation",12}{"Best",12}{"Median",12}{"Worst",12}");
foreach (var generation in Milestones(generations.Count))
{
    var entry = generations[generation];
    Console.WriteLine($"{generation + 1,12}{entry.Best.ObjectiveVector[0],12:F1}{entry.Median.ObjectiveVector[0],12:F1}{entry.Worst.ObjectiveVector[0],12:F1}");
}

Console.WriteLine();
Console.WriteLine($"Shortest tour: {bestTour.ObjectiveVector[0]:F1}");
Console.WriteLine($"Order visited: {string.Join(" -> ", bestTour.Candidate)}");

// The default instance is a four by four grid of cities spaced 100 apart, so a tour that steps between neighbours the
// whole way round costs exactly 1600. That makes the optimum known here, which is unusual and useful in a sample.
const double KnownOptimum = 1600.0;
Console.WriteLine(bestTour.ObjectiveVector[0] <= KnownOptimum
    ? "This run reached the known optimum."
    : $"This run finished {bestTour.ObjectiveVector[0] - KnownOptimum:F1} above the known optimum of {KnownOptimum:F0}.");

static IEnumerable<int> Milestones(int count) =>
    new[] { 0, count / 4, count / 2, 3 * count / 4, count - 1 }.Distinct().Where(index => index >= 0);
