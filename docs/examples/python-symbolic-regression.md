# Run symbolic regression from Python

This example fits an expression to Python data and prints the best R² value after each generation. Python calls the HeuristicLib symbolic regression runner through pythonnet.

::: warning Experimental interop
Python interop is under development. HeuristicLib does not have a pip package. Build the .NET projects locally before running this example.
:::

## Prepare the environment

Publish the interop assembly and install pythonnet:

```console
dotnet publish src/HeuristicLib.PythonInterop --configuration Release
python -m venv .venv
.venv\Scripts\activate
python -m pip install pythonnet
```

On Linux or macOS, activate the environment with `source .venv/bin/activate`.

## Fit an expression

Save this script as `fit.py` in the repository root:

```python
from pathlib import Path
import sys

from pythonnet import load

load("coreclr")

import clr

publish_dir = Path(
    "src/HeuristicLib.PythonInterop/bin/Release/net10.0/publish"
).resolve()
sys.path.append(str(publish_dir))
clr.AddReference("HEAL.HeuristicLib.PythonInterop")

from System import Array, Double, Func
from HEAL.HeuristicLib.Encodings.SymbolicExpressions import ExpressionTree
from HEAL.HeuristicLib.Objectives import ObjectiveVector
from HEAL.HeuristicLib.PythonInterop import (
    InteractiveSymbolicRegression,
    InteractiveSymRegParameters,
)

x = Array[Double]([-2.0, -1.0, 0.0, 1.0, 2.0])
y = Array[Double]([5.0, 2.0, 1.0, 2.0, 5.0])

parameters = InteractiveSymRegParameters()
parameters.PopulationSize = 100
parameters.Generations = 30
parameters.TreeLength = 20
parameters.TreeDepth = 8
parameters.Seed = 42


def observe_population(trees, objectives):
    best_r2 = max(float(objective[0]) for objective in objectives)
    print(f"best R²: {best_r2:.4f}")

    return Array[Array[Double]](
        [Array[Double]([float(objective[0])]) for objective in objectives]
    )


callback = Func[
    Array[ExpressionTree],
    Array[ObjectiveVector],
    Array[Array[Double]],
](observe_population)

population = InteractiveSymbolicRegression.Run(
    x,
    y,
    callback,
    parameters,
)

best = max(
    population.EvaluatedCandidates,
    key=lambda candidate: float(candidate.ObjectiveVector[0]),
)

expression = InteractiveSymbolicRegression.FormatTree(best.Candidate)
print(f"model: {expression}")
print(f"R²: {float(best.ObjectiveVector[0]):.4f}")
```

Run it with:

```console
python fit.py
```

The target samples follow `y = x² + 1`. The genetic programming run searches for an expression rather than receiving that equation as a fixed model form.

The callback receives every evaluated population. It returns the objective values unchanged after printing progress. A Python application can use the same callback to store a quality curve or update a plot.

## Interactive application

The repository also contains a FastAPI application that streams each population to a browser:

<img class="example-demo" src="/interactive-symbolic-regression.gif" alt="Drawing a curve and fitting it with the interactive symbolic regression demonstrator">

Run the [interactive demonstrator](https://github.com/heal-research/HeuristicLib/tree/dev/python-samples/PythonInteractiveDemonstrator) after publishing the interop assembly:

```console
python -m pip install -r python-samples/PythonInteractiveDemonstrator/requirements.txt
python python-samples/PythonInteractiveDemonstrator/app.py
```

Read [Python interop](/guide/interop/python) for loading rules, available helpers and current limitations.
