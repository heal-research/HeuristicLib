# Python samples

Demonstrations that drive HeuristicLib from Python through pythonnet.

These are not part of `HEAL.HeuristicLib.slnx`, so nothing here is built, formatted, analyzed or tested by CI, and nothing here breaks when the C# API changes. They are checked by running them. Treat a change to the .NET surface as a reason to run them by hand.

C# samples, which do break on an API change, live in [`samples`](../samples).

## Contents

- `PythonInteractiveDemonstrator` — a FastAPI application that runs symbolic regression and streams each generation to a browser.
- `PythonInteroperability` — notebooks for Python callbacks and custom symbolic regression scoring.

Both need the interop assembly published locally first; see [the Python interop guide](../docs/guide/interop/python.md).
