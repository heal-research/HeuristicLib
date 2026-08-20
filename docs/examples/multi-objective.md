# Multiobjective optimization with NSGA-II

Most real decisions have more than one goal, and the goals disagree. A cheaper design holds less. A faster schedule costs more. There is no single best answer, only a set of answers where improving one goal must cost you another.

This example designs a cylindrical container. It minimizes the material in the walls and maximizes the volume held, and it returns the whole tradeoff curve rather than one compromise.

## Why not just combine the objectives

The tempting shortcut is to score each design with something like `volume - 0.5 * material` and run a normal genetic algorithm. That works, but it answers a different question. Picking the weight picks the answer, and you have to pick it before seeing what the tradeoffs look like.

Multiobjective search inverts the order. It gives you the tradeoff curve first, and you choose from it afterwards with the numbers in front of you. Weighted sums also cannot reach designs on a concave part of a tradeoff curve at any weight, so some good answers are unreachable no matter how you tune.

Use a weighted sum when your domain genuinely supplies the weights, such as a real price. Use multiobjective search when the weights are a guess.

## Dominance and the Pareto front

One design **dominates** another when it is at least as good on every objective and strictly better on at least one. A design nobody dominates is **nondominated**, and the set of nondominated designs is the **Pareto front**.

A container using less material and holding more is strictly better, so it dominates. A container using less material but holding less is not comparable; both belong on the front. That is why `MultiObjective` directions carry no total order comparer. Asking for "the best" candidate is not a well-formed question here, and the API declines to answer it rather than inventing a ranking.

## Define the problem

Two decisions, radius and height, so a candidate is a `RealVector` of length two. Evaluation returns two values instead of one.

```csharp
var searchSpace = new RealVectorSearchSpace(
    length: 2,
    minimum: [2.0, 2.0],
    maximum: [12.0, 25.0]);

var problem = new FuncProblem<RealVector, RealVectorSearchSpace>(
    (RealVector candidate) =>
    {
        var radius = candidate[0];
        var height = candidate[1];

        var material = 2 * Math.PI * radius * radius + 2 * Math.PI * radius * height;
        var volume = Math.PI * radius * radius * height;

        return new[] { material, volume };
    },
    searchSpace,
    MultiObjective.Create(ObjectiveDirection.Minimize, ObjectiveDirection.Maximize));
```

`MultiObjective.Create` takes one direction per objective, and the order must match the order of the returned array. Here material is minimized and volume is maximized. Mixing directions in one problem is fine; nothing has to be rewritten as a minimization.

## Configure NSGA-II

`NSGA2` takes the same creator, crossover and mutator as a genetic algorithm. Two things differ: selection and replacement must both understand dominance, and there is no `Elites` setting because the replacer already keeps the best fronts.

```csharp
var algorithm = new NSGA2<RealVector, RealVectorSearchSpace, FuncProblem<RealVector, RealVectorSearchSpace>>
{
    PopulationSize = 100,
    MaximumGenerations = 200,
    Creator = new UniformDistributedCreator(),
    Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
    Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1),
    Selector = ParetoCrowdingTournamentSelector.For(problem, dominateOnEqualities: false, tournamentSize: 2),
    Replacer = ParetoCrowdingReplacer.For(problem, dominateOnEqualities: true)
};
```

Both operators rank by dominance first and break ties by crowding distance, preferring candidates in sparse regions. That second criterion is what spreads the population along the front instead of letting it bunch up in one attractive corner.

The population size is also the resolution of your answer. A hundred candidates can describe at most a hundred points on the curve.

## Run and read the front

```csharp
var finalState = await algorithm.CompleteAsync(problem, RandomNumberGenerator.Create(seed: 5));

var front = ParetoFront.ExtractFrom(finalState.Population.EvaluatedCandidates, problem.Objective)
    .OrderBy(candidate => candidate.ObjectiveVector[0])
    .ToList();

Console.WriteLine($"Nondominated: {front.Count} of {finalState.Population.EvaluatedCandidates.Count}");
Console.WriteLine();
Console.WriteLine(" material    volume   radius   height    h/r");

foreach (var candidate in front.Where((_, index) => index % 12 == 0))
{
    var radius = candidate.Candidate[0];
    var height = candidate.Candidate[1];

    Console.WriteLine(
        $"{candidate.ObjectiveVector[0],9:F1}{candidate.ObjectiveVector[1],10:F1}" +
        $"{radius,9:F2}{height,9:F2}{height / radius,7:F2}");
}
```

`ParetoFront.ExtractFrom` filters a population down to its nondominated members. NSGA-II drives the whole population toward the front, so late in a run most of it already qualifies, but extracting explicitly is still the correct step because nothing guarantees it.

```
Nondominated: 100 of 100

 material    volume   radius   height    h/r
     89.7      63.3     2.48     3.28   1.32
    365.4     535.3     4.55     8.22   1.80
    859.9    1936.1     6.76    13.48   1.99
   1206.5    3217.0     7.93    16.28   2.05
   1563.7    4745.5     8.96    18.83   2.10
   1861.2    6161.4     9.75    20.64   2.12
   2120.8    7493.2    10.38    22.15   2.13
   2347.2    8723.4    10.90    23.39   2.15
   2568.7    9986.5    11.38    24.53   2.15
```

Every row is a legitimate answer. Nine rows are shown; the front holds one hundred.

The last column is the interesting one, and it is why this problem makes a good example. For a cylinder of fixed volume, the shape using least material has height exactly twice the radius. Nothing in the code says so. The search rediscovers it: through the middle of the front `h/r` sits within a few percent of `2`. It drifts at both ends because the extreme points have fewer neighbours to be crossed with and converge more slowly, not because the geometry changes.

That is a useful habit when checking multiobjective results. A front that spans the objectives but violates a property you can derive by hand has not converged, however smooth the curve looks.

## Choose a design

The front is the input to a decision, not the decision. Once the tradeoff is visible, a constraint from the domain usually picks the point.

```csharp
var choice = front.First(candidate => candidate.ObjectiveVector[1] >= 2000);

Console.WriteLine(
    $"Cheapest design holding at least 2000: " +
    $"radius {choice.Candidate[0]:F2}, height {choice.Candidate[1]:F2}, " +
    $"material {choice.ObjectiveVector[0]:F1}");
```

```
Cheapest design holding at least 2000: radius 6.96, height 14.02, material 917.1
```

Because the front is sorted by increasing material, the first entry meeting the volume requirement is the cheapest one that meets it. Applying the requirement now rather than as a constraint during the search means you can also see what relaxing it would buy, which a single-objective run cannot tell you.

## Reporting

Report the front, not one point from it. State every objective with its unit and direction, and keep the seed: a front is a stochastic result like any other, and two seeds give slightly different curves.

When comparing configurations, compare fronts rather than single values. A front that is better everywhere is a clear win. A front better in one region and worse in another is a real tradeoff and needs a decision, not an average.

Continue with [Objectives and evaluated candidates](/guide/fundamentals/objectives) for dominance semantics, [Model your own problem](/examples/custom-problem) for a problem type with domain data, or [Experiments](/guide/execution/experiments) to repeat this across seeds.
