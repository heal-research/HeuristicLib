# AGENTS.md

Contributor contract for coding agents. `CLAUDE.md` imports this file.

## Guidance

Read the guidance relevant to the task:

| Task                                                | Guidance                                                                                                                                                |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Implementation and public API conventions           | [Developer guidelines](docs/contributing/developer-guidelines.md)                                                                                       |
| Capability boundaries and architectural tradeoffs   | [Requirements](docs/contributing/requirements.md) and [design goals](docs/contributing/design-goals.md)                                                 |
| Naming and domain language                          | [Glossary](docs/reference/glossary.md); prefer canonical terms unless a local context defines a narrower meaning                                        |
| Adding or moving types and dependencies             | [Layering](docs/contributing/architecture/layering.md)                                                                                                  |
| Scoped state, resolution, sharing or wrappers       | [Execution nodes](docs/contributing/architecture/execution-nodes.md) and [execution resolution](docs/contributing/architecture/execution-resolution.md) |
| Authoring or changing operators                     | [Operator implementation](docs/contributing/architecture/operator-implementation.md)                                                                    |
| Analyzers, observations and result ownership        | [Analyzer architecture](docs/contributing/architecture/analyzers.md)                                                                                    |
| Test placement, API usage specs and suite selection | [Test suite guide](test/README.md)                                                                                                                      |

## Work tracking

Planned work, design repairs and their decisions live in GitHub Issues; an active handover lives on its owning branch. Do not add local plan or backlog files. Take scope from the assigned issue, its parent, dependencies and decisions, not from the branch name, and check related open and closed issues before revisiting a design.

An issue does not by itself approve implementation. Use sub-issues only for deliverables that are reviewed or completed separately. An implementation issue closes when its completion criteria are met on its integration branch; an investigation issue closes once its decision and follow-up issues are recorded.

Every public GitHub change and every push needs explicit approval. Draft issue text in chat first: concise, concrete, ending with a short completion outcome.

## Architecture checkpoint

Discuss architectural changes with the developer and obtain explicit acceptance before implementing them. A change is architectural when it:

- adds, removes or reshapes a public or shared type, interface, base class, generic parameter, role or child slot;
- moves a responsibility, dependency, or state ownership or lifetime across types, layers or packages;
- changes a contract, or how execution, resolution or substitution work;
- introduces a new pattern, wrapper, adapter, registry or execution mechanism.

Routine changes that follow established patterns may proceed. Before editing code, state in one line whether the change is routine or architectural.

Before proposing a design, inspect the relevant code and guidance and try to express the scenario with existing mechanisms (developer guidelines § 2.5). Present it as § 2.1 describes, with unresolved risks, assumptions and a recommendation. Ask only about requirements and tradeoffs, recommend an answer to each question, and challenge weak designs, including the developer's own. Use the `grilling` skill when several decisions are open. Record accepted decisions on the owning issue; they need no repeated approval.

A general implementation or refactoring request does not approve an agent-invented redesign; list unrequested architectural improvements as suggestions in the final reply. Return to the discussion when implementation deviates from the accepted design or starts to need workarounds, flags, casts, parallel hierarchies or adapters the design did not name.

## Repository map

- `src/HeuristicLib`: core public contracts and library implementations.
- `src/HeuristicLib.Experimental`: experimental problems, workflows and features.
- `src/HeuristicLib.PythonInterop`: Python integration and adapters.
- `test`: core, Experimental, scenario, API usage and documentation suites.
- `analyzers`: Roslyn analyzers and code fixes for repository-specific API usage rules.
- `docs`: published VitePress documentation.
- `samples`: runnable C# applications in the solution, referencing `src` projects.
- `python-samples`: Python demonstrations outside the solution; solution validation does not cover them.
- `.agents/skills` and `.claude`: shared skills and agent-specific configuration.

## Validation commands

Use the .NET SDK selected by `global.json`. Tests use Microsoft.Testing.Platform with xUnit v3: select suites with `--project` and filter with xUnit options such as `--filter-class`, not VSTest's `--filter`. Run build and test commands sequentially in this checkout.

- Restore: `dotnet restore`.
- Build: `dotnet build --configuration Release --no-restore`.
- Test one suite: `dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore`, optionally with `--filter-class '*ResolutionScopeTests'`.
- Test the solution: `dotnet test --solution ./HEAL.HeuristicLib.slnx --configuration Release --no-restore --max-parallel-test-modules 1`.
- Whitespace: `dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore`.
- Code style: `dotnet format style ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity warn`.
- Analyzer fixes: `dotnet format analyzers ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity error`.
- Documentation: `npm run docs:build`, after `npm ci` when needed.
- Markdown, JSON and YAML: `npx --no-install prettier --check <files>`.
- Packaging: `dotnet pack --configuration Release --no-restore`.

Select tests by the [validation order](test/README.md#validation-order). For prose-only changes, run the relevant documentation and formatting checks; validate tooling and build changes through the affected workflows. The CI formatting job is non-blocking, so a green CI run does not prove formatting is clean.

## Agent skills

Skills in `.agents/skills` are verbatim upstream copies; [their inventory](.agents/skills/README.md) records provenance and updates. This file, the developer guidelines and the glossary override them. Adjust skill behavior here, not in the copies:

- `grill-with-docs`: apply both `grilling` and `domain-modeling`. Without a `Skill` tool, read both skills and follow their workflows directly.
- `domain-modeling` and `grill-with-docs`: `docs/reference/glossary.md` is the domain glossary; challenge terms against it and record resolved terms there in its entry format. Create no `CONTEXT.md`, `CONTEXT-MAP.md`, ADRs or `docs/adr/`; decisions go on the owning issue.
- `two-axis-review`: for the working tree, diff against `git merge-base <fixed-point> HEAD` and include untracked files. Skip the `docs/agents/issue-tracker.md` and `/setup-matt-pocock-skills` prerequisite. The spec is the assigned issue or branch handover; the standards are this file, the developer guidelines, the design goals, the glossary and `test/README.md`. Assess smells with developer guidelines § 2.4 and § 3.6: extraction, new types and polymorphism need justification, and removing or inlining machinery or keeping limited duplication may be the better fix.
- `csharp-refactoring`: backward compatibility is not a goal (developer guidelines § 1.1). A requested public rename, move or removal is an authorized breaking change: migrate every in-repository caller, including tests, samples and documentation snippets, and add no `[Obsolete]` shims, forwarding members or `[TypeForwardedTo]`.
- All skills: use the validation commands above instead of a skill's generic build and test commands.

## Commits

Write each commit message as one imperative line of at most 72 characters, such as `Forward root executor controls`, with no body. Never attribute agents or AI tools: no co-author trailers, signatures or "generated by" notices.
