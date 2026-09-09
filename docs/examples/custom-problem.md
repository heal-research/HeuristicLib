# Model your own problem

The built-in problems are useful for learning, but real work starts when the objective comes from your own domain. This example builds a problem type from scratch: a workshop decides how many of each product to build this week, given limited machine hours.

It shows the three decisions every custom problem forces you to make. What is a candidate, how is it evaluated, and what happens to candidates that break a constraint.

## The domain

Four products. Each earns a profit per unit and consumes hours on a cutting machine and an assembly bench. Both machines have a weekly capacity, and no product runs more than 25 units in one batch.

| Product | Profit | Cutting hours | Assembly hours |
| ------- | -----: | ------------: | -------------: |
| stool   |     10 |             1 |              2 |
| chair   |     18 |             2 |              3 |
| table   |     40 |             4 |              5 |
| shelf   |     14 |             2 |              1 |

Cutting has 160 hours available, assembly has 140.

Keep the domain data in its own type. The problem stays readable and the same data can be reused by tests, reports and a second problem formulation later.

```csharp
public sealed record ProductionPlan(
    string[] Products,
    double[] ProfitPerUnit,
    double[] CuttingHoursPerUnit,
    double[] AssemblyHoursPerUnit,
    double CuttingCapacity,
    double AssemblyCapacity,
    int MaximumBatchSize)
{
    public (double Cutting, double Assembly) Usage(IReadOnlyList<int> quantities)
    {
        double cutting = 0, assembly = 0;

        for (var i = 0; i < quantities.Count; i++)
        {
            cutting += quantities[i] * CuttingHoursPerUnit[i];
            assembly += quantities[i] * AssemblyHoursPerUnit[i];
        }

        return (cutting, assembly);
    }

    public double Profit(IReadOnlyList<int> quantities)
    {
        double profit = 0;
        for (var i = 0; i < quantities.Count; i++) profit += quantities[i] * ProfitPerUnit[i];
        return profit;
    }

    public bool IsFeasible(IReadOnlyList<int> quantities)
    {
        var (cutting, assembly) = Usage(quantities);
        return cutting <= CuttingCapacity && assembly <= AssemblyCapacity;
    }
}
```

## Choose a candidate representation

A plan is one quantity per product, so a candidate is an `IntegerVector` of length four with each position bounded between `0` and the maximum batch size. `IntegerVectorSearchSpace` expresses exactly that, and the existing integer operators work with it without any extra code.

The machine capacities are deliberately **not** part of the search space. A search space describes structural validity, the shape an answer must have. Capacity is a property of this week's schedule and belongs to the problem, which is what lets you reuse the same representation when capacity changes.

## Write the problem

Derive from `SingleSolutionProblem<TSelf, TCandidate, TSearchSpace>` and implement one method. The base class handles batching several candidates for you.

The first type argument is the problem's own type. That is the [curiously recurring pattern](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern): it lets the base class name the derived type in members that return or accept the problem, so a factory hands back a `ProductMixProblem` rather than something you have to cast. You write the class name twice and get nothing else to think about.

```csharp
public sealed class ProductMixProblem(ProductionPlan plan)
    : SingleSolutionProblem<ProductMixProblem, IntegerVector, IntegerVectorSearchSpace>(
        SingleObjective.Maximize,
        new IntegerVectorSearchSpace(plan.Products.Length, 0, plan.MaximumBatchSize))
{
    public override ObjectiveVector Evaluate(IntegerVector candidate, IRandomNumberGenerator random)
    {
        var quantities = candidate.ToArray();
        var (cutting, assembly) = plan.Usage(quantities);

        var overrun = Math.Max(0.0, cutting - plan.CuttingCapacity)
                    + Math.Max(0.0, assembly - plan.AssemblyCapacity);

        return plan.Profit(quantities) - 100.0 * overrun;
    }
}
```

Three things are worth pointing at.

The constructor passes `SingleObjective.Maximize` because more profit is better. Nothing else in the configuration has to know the direction; selectors and analyzers read it from the problem.

`Evaluate` returns an `ObjectiveVector`, and a single `double` converts to one implicitly. A multiobjective problem returns several values from the same method.

The `random` parameter exists for problems whose evaluation is itself stochastic, such as a simulation. This one ignores it. If you do use it, draw from that generator rather than a static `Random` so that runs stay reproducible.

## Handle infeasible candidates

Crossover and mutation will produce plans that exceed capacity. You have three options, and the choice matters more than most operator settings.

**Penalize** infeasibility in the objective, as above. Every hour over capacity costs 100, far more than any hour can earn, so infeasible plans sort below feasible ones while still being ranked among themselves. That gradient is the point: a plan four hours over capacity scores better than one forty hours over, so selection can walk back toward the feasible region instead of treating every violation as equally dead.

**Repair** the candidate inside a custom operator, so infeasible plans never reach evaluation. This preserves the whole population but biases the search toward whatever the repair rule happens to produce.

**Reject** by returning a constant worst value. Simple, and usually the weakest choice, because it flattens the landscape and the search gets no signal about which direction leads back to feasibility.

Penalizing has one consequence you must handle at the end: a penalized candidate is still in the final population, so filter before reporting.

## Configure and run

::: tip Project layout
Put `ProductionPlan` and `ProductMixProblem` in their own files. If you keep everything in `Program.cs`, the top-level statements below must come before the two type declarations, because C# does not allow top-level statements after a type.
:::

```csharp
var workshop = new ProductionPlan(
    Products: ["stool", "chair", "table", "shelf"],
    ProfitPerUnit: [10, 18, 40, 14],
    CuttingHoursPerUnit: [1, 2, 4, 2],
    AssemblyHoursPerUnit: [2, 3, 5, 1],
    CuttingCapacity: 160,
    AssemblyCapacity: 140,
    MaximumBatchSize: 25);

var problem = new ProductMixProblem(workshop);

var algorithm = GeneticAlgorithm.Create(
    new UniformDistributedCreator(),
    new SinglePointCrossover(),
    new UniformSomePositionsMutator { Probability = 0.15 },
    selector: TournamentSelector.For(problem, tournamentSize: 3),
    populationSize: 300,
    maximumGenerations: 500,
    mutationRate: 0.4);

var finalState = await algorithm.CompleteAsync(
    problem,
    RandomNumberGenerator.Create(seed: 2));

var best = finalState.Population.EvaluatedCandidates
    .Where(candidate => workshop.IsFeasible(candidate.Candidate))
    .MaxBy(candidate => candidate.ObjectiveVector[0])
    ?? throw new InvalidOperationException("The final population contains no feasible plan.");

var (cutting, assembly) = workshop.Usage(best.Candidate);

Console.WriteLine($"Profit: {workshop.Profit(best.Candidate):F0}");
Console.WriteLine();

for (var product = 0; product < workshop.Products.Length; product++)
{
    Console.WriteLine($"  {workshop.Products[product],-6} {best.Candidate[product],3}");
}

Console.WriteLine();
Console.WriteLine($"  cutting   {cutting,3:F0} / {workshop.CuttingCapacity:F0} hours");
Console.WriteLine($"  assembly  {assembly,3:F0} / {workshop.AssemblyCapacity:F0} hours");
```

The reporting step filters to feasible plans and reads the profit from the domain object rather than from `ObjectiveVector`, because the objective value carries the penalty and the profit does not. Selecting on the penalized value while reporting the true value is the pattern to copy.

## What it finds

```
Profit: 1270

  stool    0
  chair    0
  table   23
  shelf   25

  cutting   142 / 160 hours
  assembly  140 / 140 hours
```

The plan drops stools and chairs entirely. Both consume assembly time that a table or a shelf turns into more profit, and assembly is the binding constraint at 140 of 140 hours while cutting still has 18 hours spare. Identifying the bottleneck is usually more valuable to the person who asked than the profit number itself.

This instance is small enough to check by exhaustive search, and `1270` is the true optimum. Six of ten seeds reach it; the others stop at `1258`, a local optimum one unit away. That ratio is worth knowing before you trust a single run, and it is why the [experiments API](/guide/execution/experiments) exists.

## Adapting this

The shape carries over to most custom problems. Put domain data in its own type, pick the representation that makes invalid answers hardest to express, derive from `SingleSolutionProblem`, and decide deliberately how infeasible candidates are treated.

Two changes are worth trying on this example. Give each product a setup cost that applies only when its quantity is above zero, which makes the objective discontinuous and much harder for a smooth search. Or replace the penalty with a repair operator that scales a plan down until it fits, and compare how the two formulations behave across seeds.

Continue with [Problems](/guide/fundamentals/problems) for the full problem contract, [Writing operators](/guide/extending/writing-operators) to build that repair operator, or [Multiobjective optimization](/examples/multi-objective) to optimize profit and machine load at the same time.
