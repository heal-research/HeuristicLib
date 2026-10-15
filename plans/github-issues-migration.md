# Move development planning to GitHub Issues

Status: decision accepted on 2026-10-13; migration not started. Existing planning sources remain in use until their contents are migrated.

Canonical repository: [heal-research/HeuristicLib](https://github.com/heal-research/HeuristicLib).

## Accepted decision

Use GitHub Issues as the default home for ideas, bugs, design questions and implementation plans. The issue description is the working plan; a separate checked-in specification is not required.

Keep the organization simple:

- Maintain the current understanding, accepted scope, open questions and completion criteria in the issue description.
- Use comments for discussion, decision records, progress and validation evidence. Summarize settled discussion back into the description and link to significant decision comments.
- Use a checklist for steps within one change. Introduce child issues when work needs an independent discussion, review or completion state.
- Preserve rejected alternatives, their reasoning and conditions for reconsideration in closed issues.
- Keep current library contracts, contributor rules and terminology in the existing repository documentation. Keep executable examples, tests, prototypes and reproducibility artifacts in the repository and link to them from issues.
- Do not adopt spec-driven development tooling, a mandatory specification layer or an ADR framework as part of this migration.

The complete user-documentation and glossary rework is a separate work item. Creating its issue is within migration scope; performing that rework is not.

## Responsibility mapping

| Existing material                                           | Destination                                                                  |
| ----------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Individual unfinished backlog item                          | One issue, after checking relevance and duplicates                           |
| Unresolved design question                                  | One issue with the question, alternatives and eventual decision              |
| Implementation plan for one change                          | Issue description with useful implementation steps and completion criteria   |
| Roadmap spanning independent changes                        | Parent issue linking actionable child issues                                 |
| Review checkpoints and implementation history               | Issue comments and links to commits or PRs                                   |
| Rejected approach                                           | Closed issue with the decision, reasoning and conditions for reconsideration |
| Current behavior and architectural rules                    | Existing user and contributor documentation                                  |
| Prototype code, benchmark programs and supporting artifacts | Repository files linked from the relevant issue                              |

Classify contents rather than creating one issue per Markdown file. A single document may mix several responsibilities. Conversely, related fragments in several documents may belong to one issue.

Retire the replaced planning entries so that the issue and local file do not become competing working plans. Preserve useful evidence and decisions before retiring a source. Completed historical records need not become new open work.

## Minimal issue conventions

Start with normal open/closed states and a few useful labels. Reuse existing labels where possible; `bug`, `enhancement`, `documentation` and `needs-decision` are sufficient starting examples. Additional boards, custom fields and automation are outside the initial migration.

An actionable issue should explain its problem, desired outcome, approach or unresolved questions, and completion criteria. The structure can be informal and proportional to the work. An idea may start with a short description and gain detail when it is investigated.

An open issue or label does not by itself approve implementation. Preserve the distinction between proposed and accepted approaches, and record review checkpoints explicitly. A design investigation may finish with an accepted decision and concrete follow-up work rather than an implementation.

Parent issues own the overall outcome and shared decisions. Child issues own their individual deliverables. Use blocking relationships where a real dependency exists, without repeating the complete parent plan in each child.

GitHub descriptions can be edited and have edit history, but they evolve independently of code revisions. Link significant decisions to their resulting commits or PRs; keep implemented contracts verifiable in code, tests and current documentation. Issue context must be fetched when working from a checkout.

GitHub closing keywords apply when a PR targets the default branch; intermediate delivery branches may require deliberate issue completion tracking.

## Migration packages

### 1. Inventory and pilot

1. Make `gh` available in the executing environment and authenticate if needed. Verify installation with `gh --version`, authentication with `gh auth status`, and repository access with `gh repo view heal-research/HeuristicLib --json hasIssuesEnabled,viewerPermission,defaultBranchRef`. Confirm Issues are enabled and the account has the permissions needed to manage them. Use `--repo heal-research/HeuristicLib` explicitly for GitHub operations.
2. Read existing issues, including closed ones, before creating anything. Reuse or update relevant issues rather than introducing duplicates.
3. Inventory `plans/`, the developer backlog and references to them. Classify active work, unresolved questions, rejected approaches, completed history and repository artifacts. Check enough source and history to identify stale or completed work; retain uncertainties explicitly.
4. Create one migration issue carrying this accepted decision and execution checklist. Use its description to maintain the temporary source-to-destination mapping, including issue URLs and reasons for retaining any repository artifacts. This is migration evidence, not a permanent second backlog.
5. Pilot one focused backlog item and the separate user-documentation/glossary work item. The focused item should be small enough to judge whether its issue supplies sufficient context on its own. The documentation issue should state that structure and scope need agreement before broad rewriting.
6. Verify the issue descriptions, labels and links, then present the pilot and proposed remaining batches for review. Stop at this checkpoint before bulk conversion or retiring source files.

### 2. Migrate the remaining planning content

After the pilot review, migrate coherent batches of related work. For each source item, record its destination in the migration issue before retiring it. Carry accepted decisions, significant alternatives, completion criteria and review boundaries across explicitly.

Use the shapes demonstrated by the existing material:

- A focused fix such as rejecting a competing `AlgorithmRun.Stream()` call belongs in one issue with the reported behavior and a regression-test outcome. Recheck its relevance before presenting it as a current defect.
- The experiment serialization boundary belongs in a design-question issue until its intended contract is accepted.
- Performance regression benchmarking can use a parent issue with independently reviewable runner, comparison and CI deliverables. Preserve the accepted constraints from its existing plan.
- A rejected shared `RunHost` belongs in a closed issue preserving what was tried, why it failed to simplify ownership and what evidence would justify reconsideration.

This package migrates planning content. It does not implement the underlying fixes, features, architectural changes or documentation redesign.

### 3. Switch repository guidance and retire replaced plans

1. Update `AGENTS.md` so planned work starts from the assigned issue, with relevant parent, dependency and decision context. Update its decision-recording and review-source skill overrides as well.
2. Update planning-role references in the developer guidelines, contributing index, requirements and other affected files. Keep these edits limited to the organization change; their architectural and product rules remain the source of current contracts.
3. Preserve the upstream skill copies. Adapt their repository-specific behavior through the existing overrides rather than adding a parallel issue-tracker framework.
4. Retire migrated plan files and backlog sections only after checking their coverage and repairing references. Git history preserves retired documents; useful current rules and repository artifacts must remain discoverable.
5. Preserve this accepted decision in the migration issue, then retire this transitional plan when the migration is complete.

## Validation and completion

The migration is complete when:

- Each inventoried source item has a checked issue destination, a retained artifact with a reason, or an explicit disposition as obsolete/completed history.
- Open work and rejected decisions are discoverable through issues, with proposal and acceptance status clear.
- An issue supplies enough context to identify scope, relevant decisions, completion criteria and the next review checkpoint without reconstructing a long discussion.
- Repository guidance points to the adopted workflow and no replaced local tracker remains active.
- Repository links, changed Markdown formatting and the published documentation build pass the applicable checks in `AGENTS.md`.
- No underlying feature implementation or broad documentation rework has been included merely because its plan was migrated.

## References

Repository sources: [agent contract](../AGENTS.md), [developer guidelines](../docs/contributing/developer-guidelines.md), [developer backlog](developer-backlog.md) and [benchmarking plan](performance-regression-benchmarking.md).

- [GitHub Issues overview](https://docs.github.com/en/issues/tracking-your-work-with-issues/learning-about-issues/about-issues)
- [Editing issue descriptions](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/editing-an-issue)
- [Issue dependencies](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/creating-issue-dependencies)
- [PR links and closing behavior](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue)
- [GitHub CLI issue access](https://cli.github.com/manual/gh_issue_view)
