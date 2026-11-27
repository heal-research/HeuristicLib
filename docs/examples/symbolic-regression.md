# Symbolic regression

Symbolic regression searches for an equation that fits observed data. The search changes the structure of an expression tree while a numeric refiner fits constants inside each candidate.

This example learns the linear function `y = 2.5x + 1.3` from generated samples.

## Create the training data

<<< ../../test/HeuristicLib.Tests.Docs/Examples/SymbolicRegressionExampleDocumentationSpecs.cs#symbolic-regression-imports{csharp}

<<< ../../test/HeuristicLib.Tests.Docs/Examples/SymbolicRegressionExampleDocumentationSpecs.cs#symbolic-regression-data{csharp}

`RegressionData` holds named input columns and one target column. Use the same structure when loading data from a file or another .NET data source.

## Define the expression language

<<< ../../test/HeuristicLib.Tests.Docs/Examples/SymbolicRegressionExampleDocumentationSpecs.cs#symbolic-regression-language{csharp}

The search space limits tree size and specifies which symbols may occur. The problem evaluates an expression with mean squared error. Lower values are better.

## Configure genetic programming

<<< ../../test/HeuristicLib.Tests.Docs/Examples/SymbolicRegressionExampleDocumentationSpecs.cs#symbolic-regression-algorithm{csharp}

The creator generates trees of different shapes and depths. Crossover exchanges subtrees. Mutation either replaces a node, perturbs a constant or replaces a subtree. The numeric refiner fits constants after those structural changes.

## Run and use the best model

<<< ../../test/HeuristicLib.Tests.Docs/Examples/SymbolicRegressionExampleDocumentationSpecs.cs#symbolic-regression-run{csharp}

Run it with `dotnet run`:

```
Training MSE: 3.75599E-28
Model: ((((x * 1.581138830091886) * 1.5811388300764972) - (0.39441562410836661 * 2.3229346002388009)) + 2.2162017001161143)

    x        y  prediction
-5.00  -11.200     -11.200
-4.75  -10.575     -10.575
-4.50   -9.950      -9.950
-4.25   -9.325      -9.325
-4.00   -8.700      -8.700
```

The predictions reproduce the targets exactly, and the mean squared error is at the limit of double precision. The search recovered the underlying function.

The printed tree does not look like `2.5x + 1.3`, because nothing simplifies it algebraically. It multiplies `x` by `1.581` twice, and `1.581²` is `2.5`. Its constants compute `2.216 - 0.394 * 2.323`, which is `1.3`. Genetic programming finds numerically correct models in structurally redundant forms, and the redundancy is normal rather than a defect.

`Compile(optimize: true)` folds constant subtrees such as `0.394 * 2.323` into one constant. It does not reorder the tree, so the slope and the intercept stay split:

<<< ../../test/HeuristicLib.Tests.Docs/Examples/SymbolicRegressionExampleDocumentationSpecs.cs#symbolic-regression-compiled{csharp}

```
((((x * 1.581138830091886) * 1.5811388300764972) - 0.9162017001161058) + 2.2162017001161143)
```

The result remains an expression tree. You can inspect it, render it as infix, C#, Python or LaTeX, and evaluate it on new rows. There is no algebraic simplifier in the library today.

Read [Symbolic regression](/guide/domains/symbolic-regression) for train and test partitions, metrics and model export. Read [Symbolic expressions](/guide/domains/symbolic-expressions) to define a different expression language.
