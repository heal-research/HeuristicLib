# SymReg Demonstrator

Interactive symbolic regression demo — draw a curve and get a fitted mathematical expression using **HeuristicLib** Genetic Programming via [pythonnet](https://github.com/pythonnet/pythonnet).

This example is part of the [HeuristicLib](../../README.md) project. It uses the `HEAL.HeuristicLib.Extensions` library to run symbolic regression from Python, bridging .NET and Python through pythonnet's CLR hosting.

## Demo

![Demo](./documentation/demo.gif)

## How It Works

1. The user draws a curve on a fullscreen canvas in the browser.
2. The drawn points are sent to a FastAPI/WebSocket backend (`app.py`).
3. A child process loads the HeuristicLib .NET assemblies via pythonnet and runs Genetic Programming–based symbolic regression (`InteractiveSymbolicRegression.Run`).
4. Each generation's population is streamed back to the frontend for live visualization of candidate expressions.
5. The final Pareto front of solutions (expression, R², complexity) is displayed for selection.

## Prerequisites

**Easy way** use the available **PythonInteroperability devcontainer**
  - uses  *.NET 10.0 SDK*, *Python 3.12* and installs `requirements.txt`
  - proceed with `dotnet publish` 
  - and `python examples/PythonInteractiveDemonstrator/app.py`

**Manual way**:
- *.NET 10.0 SDK* — required to build the HeuristicLib .NET libraries.
- *Python 3.10+* — with the packages listed in `requirements.txt`.

### Build HeuristicLib

Before running the app, publish the `HeuristicLib.Extensions` project so pythonnet can load the assemblies:

```bash
dotnet publish src/HeuristicLib.Extensions -c Release
```

This produces the assemblies in `src/HeuristicLib.Extensions/bin/Release/net10.0/publish/`, which `app.py` references.

### Usage

```bash
cd examples/PythonInteractiveDemonstrator
pip install -r requirements.txt
python app.py
```

Then open `http://localhost:8765` in your browser.



## Credits

A great *Thank You* to my dear friends and colleagues:
- **Bernhard Werth** ([@BernhardWerth](https://github.com/BernhardWerth)) and **Philipp Fleck** ([@NimZwei](https://github.com/NimZwei)) — for bringing HeuristicLib to life
- **Lukas Kammerer** ([@LukasCamera](https://github.com/LukasCamera)) — for the original C# implementation and concept this app is based on


And credits to:
- **Claude Opus** — for code generation
