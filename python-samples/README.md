# Python samples

Demonstrations that drive HeuristicLib from Python through pythonnet.

These are not part of `HEAL.HeuristicLib.slnx`, so solution validation does not build or test them or catch breakage when the C# API changes. They are checked by running them. Treat a change to the .NET surface as a reason to run them by hand.

C# samples, which do break on an API change, live in [`samples`](../samples).

## Contents

- `PythonInteractiveDemonstrator` — a FastAPI application that runs symbolic regression and streams each generation to a browser.
- `PythonInteroperability` — notebooks for Python callbacks and custom symbolic regression scoring, and `best_so_far_curve.py`, which plots a run's best quality over evaluations with matplotlib.

Both need the interop assembly published locally first; see [the Python interop guide](../docs/guide/interop/python.md).
