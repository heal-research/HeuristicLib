# Problems

A problem is the boundary between your domain and the search algorithm. It tells HeuristicLib which candidates are valid, how to evaluate them and how to compare their objective values.

## Start with a function problem

When evaluation is a single function, `FuncProblem.Create` is the shortest useful definition:

```csharp
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;

var searchSpace = new BoundedRealVectorSearchSpace(
    length: 3,
    minimum: [-10.0],
    maximum: [10.0]);

var problem = FuncProblem.Create(
    evaluateFunc: (RealVector candidate) => candidate.Sum(x => x * x),
    encoding: searchSpace,
    objective: SingleObjective.Minimize);
```

The candidate `[0, 0, 0]` has value `0` and is the global optimum of this sphere function.

## What belongs in a problem

Keep behavior that defines the optimization question in the problem:

- candidate evaluation
- domain data needed by evaluation
- the search space
- minimize or maximize directions

Keep search policy in the algorithm configuration. Population size, mutation strength and stopping criteria describe how to search, not what counts as a good answer.

## Built-in problems

The library includes benchmark and domain problems that are useful for learning, tests and comparisons. `TestFunctionProblem` wraps numerical benchmarks such as Rastrigin. Other namespaces cover traveling salesperson and machine learning tasks.

Built-in problems expose their search space, so creators can use the same bounds:

```csharp
var creator = new UniformDistributedCreator(problem.SearchSpace);
```

## Single and multiple objectives

Use `SingleObjective.Minimize` or `SingleObjective.Maximize` for one value. Multiobjective problems return several values and provide one direction for each. A multiobjective algorithm such as NSGA-II then works with dominance instead of reducing every tradeoff to one score.

See [Objective vectors and evaluated candidates](/guide/fundamentals/objectives) for comparison semantics.

## When to create a problem type

Create a dedicated problem type when evaluation needs named domain data, validation or reusable behavior. A domain type makes that contract visible and avoids closing over a large mutable object graph in a delegate.

A problem evaluating one candidate at a time derives from `SingleSolutionProblem` and overrides one method:

```csharp
public abstract class SingleSolutionProblem<TSelf, TCandidate, TSearchSpace>
{
    public abstract ObjectiveVector Evaluate(TCandidate candidate, IRandomNumberGenerator random);
}
```

The first type argument is the problem's own type, so a derived problem writes its own name there:

```csharp
public sealed class ProductMixProblem(ProductionPlan plan)
    : SingleSolutionProblem<ProductMixProblem, IntegerVector, IntegerVectorSearchSpace>(
        SingleObjective.Maximize,
        new IntegerVectorSearchSpace(plan.Products.Length, 0, plan.MaximumBatchSize))
```

That is the [curiously recurring pattern](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern), and it is what lets the base class name the derived type in members that return a problem, so nothing hands you back a base type to cast. It is also the argument most often forgotten; the compiler reports it as CS0305, wrong number of type arguments.

[Model your own problem](/examples/custom-problem) builds one end to end, including the choice of representation and how to treat candidates that violate a constraint.

Evaluation should be safe to call more than once. Parallel algorithms and experiments may evaluate different candidates concurrently. If evaluation uses randomness, derive it from the run's explicit random source rather than hidden global state.

## Checklist

Before choosing an algorithm, verify that:

- the candidate type can express every acceptable answer
- the search space rejects or repairs invalid values
- evaluation always returns the expected number of objectives
- every objective has the correct minimize or maximize direction
- evaluation failures produce clear domain errors
- repeated evaluation is safe
