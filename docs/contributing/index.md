# Contributing

This section records the rules and architecture used when changing HeuristicLib itself. Library users do not need these pages to configure or run an optimizer.

Start with the repository `AGENTS.md` file for validation commands and test placement. Then use the topic that matches the change:

| Topic                                                                         | Use it for                                                      |
| ----------------------------------------------------------------------------- | --------------------------------------------------------------- |
| [Design goals](/contributing/design-goals)                                    | Durable product and architecture principles                     |
| [Requirements](/contributing/requirements)                                    | Supported capabilities and package boundaries                   |
| [Developer guidelines](/contributing/developer-guidelines)                    | Implementation rules, public API conventions and design changes |
| [Analyzer architecture](/contributing/architecture/analyzers)                 | Analyzer configuration, observation and result ownership        |
| [Execution instances](/contributing/architecture/execution-instances)         | Run scoped state and dependency resolution                      |
| [Operator implementation](/contributing/architecture/operator-implementation) | Internal operator bases and analyzer rules                      |

Open work and unresolved decisions live in `plans/developer-backlog.md`, outside the published documentation source.
