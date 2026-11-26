# Compare algorithms

Which algorithm suits a problem is an empirical question. This example compares a genetic algorithm with a hill climber on two ten-dimensional functions: Sphere, which has a single minimum, and Rastrigin, which has a local minimum at every integer coordinate. Both algorithms get the same evaluation budget and the same twenty seeds.

## Create the project

```console
dotnet new console --framework net10.0 --name CompareAlgorithms
cd CompareAlgorithms
dotnet add package HEAL.HeuristicLib --prerelease
```

Replace `Program.cs` with these imports:

<<< ../../test/HeuristicLib.Tests.Docs/Examples/CompareAlgorithmsDocumentationSpecs.cs#compare-algorithms-imports{csharp}

followed by this program:

<<< ../../test/HeuristicLib.Tests.Docs/Examples/CompareAlgorithmsDocumentationSpecs.cs#compare-algorithms-program{csharp}

Run it with `dotnet run`. It prints the best, median and worst result of the twenty runs and the average number of evaluated candidates:

```text
configuration                best     median      worst  evaluations
Sphere, GA                9.11E-07   8.97E-06   3.29E-05        20000
Sphere, hill climber       0.00165    0.00299      0.006        14561
Rastrigin, GA              0.00199       1.99       4.98        20000
Rastrigin, hill climber       34.3       99.3        163         1931
```

## Give every algorithm the same budget

A generation of a genetic algorithm and a step of a hill climber do different amounts of work, so the example limits evaluations, not iterations. `LimitedToCandidates(evaluator, budget)` stops an algorithm once its evaluator has processed `budget` candidates. The genetic algorithm sets `maximumGenerations: null`, so the budget alone stops it. The hill climber has no stopping setting of its own. The budget is the only limit placed on it.

The budget is checked after each state, so a run can finish the batch that crosses it. `Last(point => point.Evaluations <= budget)` therefore reads every run at exactly the same budget. `Analyzer.BestSoFar<RealVector>()` records the best objective over all evaluated candidates of a run, so the comparison does not depend on what each algorithm keeps in its final state.

`Repeat(20)` with seed `999` runs each configuration twenty times on the same random streams. The evaluations column shows how much of the budget each configuration actually used.

## Read the result

On Sphere both algorithms find the minimum region. The hill climber samples one batch of neighbors per step and moves to the best one if it improves. With a fixed mutation strength of `0.1`, improving neighbors become rare close to the minimum: after about 14,600 evaluations on average a whole batch fails to improve and the run ends, more than two orders of magnitude short of the genetic algorithm.

On Rastrigin the difference is the point of the example. The hill climber stops after about 1,900 evaluations in the first local minimum it reaches, with a median of `99.3`, and leaves the rest of its budget unused. The genetic algorithm keeps a population that can cross between minima. Its median run ends at `1.99`, a local minimum with two coordinates one unit away from the optimum, and its best run comes within `0.002` of it.

A baseline such as the hill climber shows whether a population is worth its cost. Here it clearly is on Rastrigin. The spread within one configuration matters too: on Rastrigin the genetic algorithm ends anywhere between `0.002` and `4.98`, which is why each configuration runs twenty times.

## Next steps

- [Algorithms](/guide/fundamentals/algorithms#compare-algorithms-fairly) lists the rules of a fair comparison.
- [Experiments](/guide/execution/experiments) varies the settings of one algorithm over a parameter grid.
- [Analysis of algorithm runs](/guide/execution/observability-and-analysis) reads runs at other clocks, such as elapsed time.
