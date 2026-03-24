# HeuristicLib

"A modern, reimagined library for heuristic and evolutionary algorithms"

This is the spiritual successor to [HeuristicLab](https://github.com/heal-research/HeuristicLab):

- **Library first**: Focus on a well-designed API that is intuitive to use.
- **Focused scope**: Does less, but does it well.
- **Modern C#**: Built with the latest C# features.
- **Open**: Be usable from other ecosystems like Python. 

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