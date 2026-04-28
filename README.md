# HeuristicLib

"A modern, reimagined library for heuristic and evolutionary algorithms"

This is the spiritual successor to [HeuristicLab](https://github.com/heal-research/HeuristicLab):

- **Library first**: Focus on a well-designed API that is intuitive to use.
- **Focused scope**: Does less, but does it well.
- **Modern C#**: Built with the latest C# features.
- **Open**: Be usable from other ecosystems like Python.

HeuristicLib is currently in an early alpha stage. Architecture and public APIs may change when a stronger design is justified.

## Steering docs

- [`AGENTS.md`](AGENTS.md) is the repository-wide contributor contract for humans and AI agents.
- [`docs/design-goals.md`](docs/design-goals.md) records the durable design principles and clarifies which current patterns are still provisional.

Noticeable differences to HeuristicLab:
- No GUI
  - HeuristicLib focuses on being a library first.
- No operator graph
  - Write algorithms in plain C# code.
- No serialization
  - Only key datastructures (e.g., algorithm configurations or ISolutions) are serializable.

## Development Setup

This project supports multiple development environments:

- **Visual Studio**: Full IDE support for .NET development.
- **.NET Dev Container**: A containerized environment with .NET SDK, Node.js, npm, ESLint, Git, and common CLI tools pre-installed.


### Setting DevContainer

Use the "Dev Containers: Open Folder in Container" command and wait for the container to build and initialize.

**Prerequisites:**
  - Docker container environment set up
  - Visual Studio Code or compatible IDE
  - C# (.NET) extension installed


**Quick Start:**

```bash
dotnet build
dotnet test
dotnet publish
```
