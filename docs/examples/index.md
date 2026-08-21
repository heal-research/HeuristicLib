# Examples

Start with a complete optimization task. Each example provides a runnable configuration and explains the choices that affect the result.

## Solve a problem

### Numeric optimization

[Minimize the Rastrigin function](/examples/numeric-optimization) with a genetic algorithm over real vectors. This is the shortest example and a good first test of an installation.

### Traveling salesperson

[Solve a TSPLIB instance](/examples/traveling-salesperson) with permutation crossover and mutation. The example loads `berlin52` and records a quality curve.

### Symbolic regression

[Train a symbolic regression model](/examples/symbolic-regression) from generated observations. The result is an expression tree that can be inspected and compiled.

## Go further

### Model your own problem

[Build a problem type from scratch](/examples/custom-problem) for a production planning task with domain data, integer decisions and capacity constraints. Read this one when the built-in problems stop matching your work.

### Multiobjective optimization

[Find a Pareto front with NSGA-II](/examples/multi-objective) for a design with two conflicting goals. The result is a set of tradeoffs rather than one answer.

## Connect from Python

### Symbolic regression from Python

[Fit an expression from Python](/examples/python-symbolic-regression) through pythonnet. The example configures a run, observes each population and reads the best model without hiding the .NET boundary.

To define a new problem or write a reusable component, read [Problems](/guide/fundamentals/problems), [Writing operators](/guide/extending/writing-operators) and [Writing algorithms](/guide/extending/writing-algorithms).
