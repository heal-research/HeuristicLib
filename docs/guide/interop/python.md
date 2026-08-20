# Python interop

HeuristicLib can run inside Python through [pythonnet](https://pythonnet.github.io/). The bridge is under development. There is no HeuristicLib package on PyPI yet, so you must build the .NET assemblies from this repository before importing them.

## Requirements

- .NET 10 SDK
- Python 3.10 or newer
- A local clone of HeuristicLib

Create a virtual environment and install pythonnet:

```console
python -m venv .venv
.venv\Scripts\activate
python -m pip install pythonnet
```

On Linux or macOS, activate the environment with `source .venv/bin/activate`.

Publish the interop project from the repository root:

```console
dotnet publish src/HeuristicLib.PythonInterop --configuration Release
```

The command writes the loadable assemblies to `src/HeuristicLib.PythonInterop/bin/Release/net10.0/publish`.

## Load HeuristicLib

Run this script from the repository root:

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

from HEAL.HeuristicLib.PythonInterop import InteractiveSymbolicRegression

symbols = list(InteractiveSymbolicRegression.GetAvailableSymbols())
print(symbols)
```

Call `load("coreclr")` before importing `clr`. Add the publish directory to `sys.path` before calling `clr.AddReference`.

## Pass Python data to .NET

pythonnet can convert many Python values automatically. Create explicit .NET arrays when a method signature requires them:

```python
from System import Array, Double
from HEAL.HeuristicLib.PythonInterop import (
    InteractiveSymbolicRegression,
    InteractiveSymRegParameters,
)

x = Array[Double]([-2.0, -1.0, 0.0, 1.0, 2.0])
y = Array[Double]([5.0, 2.0, 1.0, 2.0, 5.0])

data = InteractiveSymbolicRegression.CreateRegressionDataFromArrays(x, y)

parameters = InteractiveSymRegParameters()
parameters.PopulationSize = 100
parameters.Generations = 30
parameters.Seed = 42
```

The interop assembly also contains helpers for symbolic regression callbacks, regression data, experiment results and genealogy analysis. These APIs may change while the Python integration is being developed.

## Run the interactive example

The [interactive symbolic regression demonstrator](https://github.com/heal-research/HeuristicLib/tree/dev/examples/PythonInteractiveDemonstrator) is the most complete Python example. It uses FastAPI and pythonnet to run HeuristicLib and stream each generation to a browser.

From the repository root:

```console
dotnet publish src/HeuristicLib.PythonInterop --configuration Release
python -m pip install -r examples/PythonInteractiveDemonstrator/requirements.txt
python examples/PythonInteractiveDemonstrator/app.py
```

Open `http://localhost:8765` after the server starts.

The `examples/PythonInteroperability` directory contains notebooks for Python callbacks and custom symbolic regression scoring. They use the same local publish workflow.

## Current limitations

- HeuristicLib cannot be installed with `pip`.
- Python must load assemblies built from a compatible HeuristicLib checkout.
- The interop API is experimental and may change between commits.
- Python callbacks cross the Python and .NET boundary, so callback heavy workloads need measurement.

Use the C# API for packaged applications today. Use Python interop for experiments, notebooks and integration work where building from source is acceptable.
