# Samples

Runnable C# applications that use HeuristicLib the way an application would.

Each sample is its own project, referencing `src/HeuristicLib` directly rather than a published package. That is the point: a sample compiles against the working tree, so an API change that makes a sample awkward or impossible breaks the build here rather than surfacing after a release. A sample that cannot break is not telling you anything.

Run one with:

```bash
dotnet run --project samples/TravelingSalesman
```

## Adding a sample

One project per sample, added to `HEAL.HeuristicLib.slnx` under the `samples` folder. A sample owns its data files, so an instance file or CSV lives beside the code that reads it instead of in a shared pile.

Set `<IsPackable>false</IsPackable>`; samples ship as source, not as packages.

Write the `using` directives out. Samples inherit no global usings, which is deliberate — the import list is part of what a reader copies.

Aim for a task with enough substance to show why a component exists. A sample that only proves the library loads teaches nothing that the getting-started guide does not already cover.

| Sample | Shows |
| --- | --- |
| `TravelingSalesman` | A problem that supplies its own operators, a run with quality tracking, and reading the best candidate |
