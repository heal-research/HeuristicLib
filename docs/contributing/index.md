# Contributing

This section records the rules and architecture used when changing HeuristicLib itself. Library users do not need these pages to configure or run an optimizer.

Start with the repository `AGENTS.md` file for workflow and validation commands. Then use the topic that matches the change:

| Topic                                                                         | Use it for                                                                                 |
| ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| [Design goals](/contributing/design-goals)                                    | Durable product and architecture principles                                                |
| [Requirements](/contributing/requirements)                                    | Supported capabilities and package boundaries                                              |
| [Developer guidelines](/contributing/developer-guidelines)                    | Implementation rules, public API conventions and design changes                            |
| [Layering](/contributing/architecture/layering)                               | Which layer a type belongs to, which dependencies are allowed and who owns a run           |
| [Execution nodes](/contributing/architecture/execution-nodes)                 | What configurations, executors and run results own, and how they are constructed           |
| [Execution resolution](/contributing/architecture/execution-resolution)       | How a scope builds, keeps and shares execution nodes, and how substitutions order wrappers |
| [Operator implementation](/contributing/architecture/operator-implementation) | Authoring paths, arities and executors for operators, new operations and Roslyn guardrails |
| [Analyzers](/contributing/architecture/analyzers)                             | Observation boundaries, clocks, traces and the ownership of analyzer results               |
