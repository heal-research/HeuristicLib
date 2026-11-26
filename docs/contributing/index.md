# Start contributing

This section is for changing HeuristicLib itself. To use the library, start with the [guide](/guide/).

## Set up

You need the .NET SDK that `global.json` selects and, for the documentation, Node.js. Then restore and build:

```console
dotnet restore
dotnet build --configuration Release --no-restore
```

`npm ci` installs the documentation tools, and `npm run docs:dev` serves the site locally.

## Find your way

| Folder                           | Holds                                                            |
| -------------------------------- | ---------------------------------------------------------------- |
| `src/HeuristicLib`               | The core package: contracts, algorithms, operators and encodings |
| `src/HeuristicLib.Experimental`  | Experimental problems, workflows and features                    |
| `src/HeuristicLib.PythonInterop` | The Python integration                                           |
| `test`                           | Unit, scenario, API usage and documentation suites               |
| `analyzers`                      | Roslyn analyzers for the repository's API rules                  |
| `docs`                           | This site                                                        |
| `samples`                        | Runnable C# applications                                         |

## Plan the change

Planned work and design decisions live in [GitHub Issues](https://github.com/heal-research/HeuristicLib/issues). Check the related issues before you start, and discuss a change to a public type, a contract or a mechanism before you implement it. [Developer guidelines](/contributing/developer-guidelines) § 2.1 describes how to present a design.

## Validate it

Run the narrowest tests that give confidence, then widen. The [test suite guide](https://github.com/heal-research/HeuristicLib/blob/main/test/README.md) says where a test goes and in which order to run the suites.

```console
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore
dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore
```

For a documentation change, run `npm run docs:build` and the documentation tests. [Writing documentation](/contributing/writing-documentation) covers the rest.

## Commit

Write each commit message as one imperative line of at most 72 characters, such as `Forward root executor controls`.

## Read further

| Page                                                       | Use it for                                                      |
| ---------------------------------------------------------- | --------------------------------------------------------------- |
| [Design goals](/contributing/design-goals)                 | Durable product and architecture principles                     |
| [Requirements](/contributing/requirements)                 | Supported capabilities and package boundaries                   |
| [Developer guidelines](/contributing/developer-guidelines) | Implementation rules, public API conventions and design changes |
| [Architecture overview](/contributing/architecture/)       | How the parts of the library fit together                       |
