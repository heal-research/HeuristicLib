# Samples

Runnable C# applications that use HeuristicLib the way an application would. Each sample references `src/HeuristicLib` directly, so an API change that makes a sample awkward or impossible breaks the build here rather than after a release.

```bash
dotnet run --project samples/TravelingSalesman
```

## Adding a sample

- Add one project per sample to `HEAL.HeuristicLib.slnx` under the `samples` folder, with `<IsPackable>false</IsPackable>`.
- Keep the sample's data files beside its code.
- Write the `using` directives out; samples inherit no global usings.
- Choose a task with enough substance to show why a component exists.

| Sample              | Shows                                                                                                  |
| ------------------- | ------------------------------------------------------------------------------------------------------ |
| `TravelingSalesman` | A problem that supplies its own operators, a run with quality tracking, and reading the best candidate |
