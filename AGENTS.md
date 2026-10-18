# AGENTS.md

Contributor contract shared by coding agents. `CLAUDE.md` imports this file.

## Find the relevant guidance

Follow the developer guidelines for implementation, public APIs and documentation responsibilities, and the glossary for terminology. Read the sections and topic pages relevant to the task.

| Task                                                               | Guidance                                                                                                                                                |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Implementation and public API conventions                          | [Developer guidelines](docs/contributing/developer-guidelines.md)                                                                                       |
| Capability boundaries and architectural tradeoffs                  | [Requirements](docs/contributing/requirements.md) and [design goals](docs/contributing/design-goals.md)                                                 |
| Naming and domain language in code, docs, tests, examples or plans | [Glossary](docs/guide/glossary.md); prefer canonical terms unless a local context explicitly defines a narrower meaning                                 |
| Adding or moving types and dependencies                            | [Layering](docs/contributing/architecture/layering.md)                                                                                                  |
| Execution state, resolution, sharing or wrappers                   | [Execution nodes](docs/contributing/architecture/execution-nodes.md) and [execution resolution](docs/contributing/architecture/execution-resolution.md) |
| Authoring or changing operators                                    | [Operator implementation](docs/contributing/architecture/operator-implementation.md)                                                                    |
| Analyzers, observations and result ownership                       | [Analyzer architecture](docs/contributing/architecture/analyzers.md)                                                                                    |
| Test placement, API usage specs and suite selection                | [Test suite guide](test/README.md)                                                                                                                      |
| Planned work or a continuation                                     | The task's stated spec or relevant plan under `plans/`; identify current scope and decision status rather than inferring them from the branch name      |

Unresolved work and rejected approaches are recorded in [the developer backlog](plans/developer-backlog.md). Check relevant rejected approaches before proposing a previously discussed direction.

## Architecture checkpoint

Discuss architectural changes with the developer and obtain explicit acceptance before implementing them. A change is architectural when it:

- adds, removes or reshapes a public or shared type, interface, base class, generic parameter, role or child slot;
- moves a responsibility, dependency, or state ownership or lifetime across types, layers or packages;
- changes a contract, or how execution, resolution or advice work;
- introduces a new pattern, wrapper, adapter, registry or execution mechanism.

Routine changes that follow established patterns may proceed. Before editing code, state in one line whether the change is routine or architectural.

Before seeking agreement, inspect the relevant code and guidance yourself. Present the design as developer guidelines § 2.1 describes, together with unresolved risks or assumptions and a recommendation.

Ask only about requirements and tradeoffs, recommend an answer to each question, and challenge weak designs, including the developer's own suggestions. For designs with several open decisions, use the `grilling` skill. Resolve material objections before implementing. Record accepted decisions in the relevant plan under `plans/`; accepted decisions need no repeated approval.

A general implementation or refactoring request does not approve an agent-invented redesign. List unrequested architectural improvements as suggestions in the final reply instead of implementing them.

Return to the discussion when implementation deviates from the accepted design, including when the code starts to need workarounds, flags, casts, parallel hierarchies or adapters that the design did not name. Do not patch around a design problem.

## Repository map

- `src/HeuristicLib`: core public contracts and library implementations.
- `src/HeuristicLib.Experimental`: experimental problems, workflows and features.
- `src/HeuristicLib.PythonInterop`: Python integration and adapters.
- `test`: core, Experimental, scenario, API usage and documentation suites; see `test/README.md` for ownership and placement.
- `analyzers`: Roslyn analyzers and code fixes for repository-specific API usage rules.
- `docs`: published VitePress documentation. `plans`: development plans, investigations and review records.
- `samples`: runnable C# applications included in the solution and referencing `src` projects.
- `python-samples`: Python demonstrations and notebooks outside the solution; solution validation does not validate them. See [their README](python-samples/README.md).
- `.agents/skills` and `.claude`: shared skills and agent-specific configuration; see [the skill inventory](.agents/skills/README.md).

## Validation commands

Use the .NET SDK selected by `global.json`. Tests use Microsoft.Testing.Platform with xUnit v3, so select suites with `--project` and use xUnit filters such as `--filter-class`, rather than VSTest's `--filter` syntax. Run build and test commands sequentially in this checkout.

- Restore dependencies with `dotnet restore`.
- Build with `dotnet build --configuration Release --no-restore`.
- Run a selected suite, for example `dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore`.
- Narrow that suite with an appropriate filter, for example `--filter-class '*ResolutionScopeTests'`.
- For final whole-solution validation, use `dotnet test --solution ./HEAL.HeuristicLib.slnx --configuration Release --no-restore --max-parallel-test-modules 1`.
- Check whitespace with `dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore`.
- Check code style with `dotnet format style ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity warn`.
- Check analyzer fixes with `dotnet format analyzers ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity error`.
- Build published documentation with `npm run docs:build`; install npm dependencies with `npm ci` when needed.
- Check changed Markdown, JSON and YAML files with `npx --no-install prettier --check <files>`.
- Validate packaging changes with `dotnet pack --configuration Release --no-restore`.

CI runs restore, Release build and tests, documentation build, formatting verification and package creation. Formatting verification is currently non-blocking in CI, so a green CI format job does not prove that formatting is clean.

Use the [test suite guide](test/README.md#validation-order) for test selection and validation order. For prose-only changes, select the relevant documentation or formatting checks. Validate tooling and build configuration changes through the affected workflows. Changes to published C# examples require documentation-suite validation; public API usage changes also require API usage validation.

## Agent skills

The repository ships general-purpose agent skills, copied verbatim from their sources; the skill inventory records their provenance and update procedure. Where a skill's instructions conflict with this file, the developer guidelines or the glossary, the repository wins. Adjust skill behavior in this section instead of editing the copied skills.

### Repository overrides

- Composition, for `grill-with-docs`: apply both `grilling` and `domain-modeling`. If the runtime has no `Skill` invocation tool, read both skills from its skill inventory and follow their workflows directly.
- Glossary, for `domain-modeling` and `grill-with-docs`: `docs/guide/glossary.md` is the domain glossary. Challenge terms against it and record resolved terms there, using its status labels and entry format. Entries may name the type that represents a concept. Do not create `CONTEXT.md` or `CONTEXT-MAP.md`.
- Decisions, for `domain-modeling` and `grill-with-docs`: do not create ADRs or `docs/adr/`. Record a settled decision in the plan of the change it belongs to under `plans/`. Approaches that were tried and rejected go to "Discussed, tried, and rejected" in `plans/developer-backlog.md`. Contributor and architecture docs describe the current state of the code, not decision history.
- Review scope, for `two-axis-review`: for reviews of the current working tree, compare against the fixed point with `git diff $(git merge-base <fixed-point> HEAD)` and include untracked files from `git status --short`.
- Review sources, for `two-axis-review`: skip the generic `docs/agents/issue-tracker.md` and `/setup-matt-pocock-skills` prerequisite. Use the task's stated spec or relevant plan under `plans/`, rather than assuming a branch-named plan. The standards are this file, the developer guidelines, the design goals, the glossary and, for tests, `test/README.md`.
- Compatibility, for `csharp-refactoring`: backward compatibility is not a design goal (developer guidelines § 1.1). A requested public rename, move or removal is an authorized breaking change. Migrate every in-repository caller, including tests, samples and documentation snippets, and add no `[Obsolete]` shims, forwarding members or `[TypeForwardedTo]` attributes.
- Validation, for all skills: use the validation commands and test execution strategy above instead of a skill's generic build and test commands.
