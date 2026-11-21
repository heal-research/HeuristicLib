# Move development planning to GitHub Issues

Status: migration in progress on `backlog-to-issue-migration`. The issues listed below own their migrated topics. Remaining backlog entries and plans stay authoritative until individually reviewed and migrated.

Canonical repository: [heal-research/HeuristicLib](https://github.com/heal-research/HeuristicLib).

## Current migration record

The branch started from `0674ddca3a7260addec913f8d826062ab767ea99`. The source removals and reference updates below are tracked on this branch. The remote branch exists, and integration into `dev` remains pending.

| Issue                                                                                                     | State               | Migrated material and disposition                                                                                                                                                                                                                                                                     |
| --------------------------------------------------------------------------------------------------------- | ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [#7 — Migrate development planning](https://github.com/heal-research/HeuristicLib/issues/7)               | Open                | Overall migration outcome. Keep this local file as the temporary migration record until the cutover is complete.                                                                                                                                                                                      |
| [#8 — Cooperative cancellation](https://github.com/heal-research/HeuristicLib/issues/8)                   | Open                | Removed the operator-cancellation backlog entry and its duplicate symbolic-regression follow-up. Related execution-policy and rejected-context entries point to the issue.                                                                                                                            |
| [#9 — Configuration and experiment persistence](https://github.com/heal-research/HeuristicLib/issues/9)   | Open                | Removed the configuration-serialization and epoch-clock persistence entries and `configuration-parameters-and-experiment-persistence.md`.                                                                                                                                                             |
| [#10 — Symbolic-regression extensions](https://github.com/heal-research/HeuristicLib/issues/10)           | Open                | Removed `symbolic-regression-extension-roadmap.md`. The issue owns the extension scope and legacy migration dependencies.                                                                                                                                                                             |
| [#11 — Authoring and architecture enforcement](https://github.com/heal-research/HeuristicLib/issues/11)   | Open                | Removed `architecture-boundary-enforcement-plan.md` and the authoring-rule, analyzer-shipping, parameter-completeness, and parameter-generation entries. The issue includes the explicit dependency model.                                                                                            |
| [#12 — Performance regression benchmarking](https://github.com/heal-research/HeuristicLib/issues/12)      | Open                | Removed `performance-regression-benchmarking.md` and the observation, batching, and resolver-performance entries. Storage, baseline management, and comparison across machines remain design decisions.                                                                                               |
| [#13 — Symbolic-regression foundation decisions](https://github.com/heal-research/HeuristicLib/issues/13) | Closed as completed | Preserved selected designs, rejected alternatives, and rationale. Removed `symbolic-regression-decisions.md`; retained its four open foundation follow-ups in one backlog entry. Problem-dependent validation already has its own entry. Completed history and additional legacy detail were retired. |
| [#14 — Data-analysis modernization](https://github.com/heal-research/HeuristicLib/issues/14)              | Open                | Removed `data-analysis-modernization-plan.md`. The related optional-dependency-extraction entry points to the issue.                                                                                                                                                                                  |

`run-cancellation.md` was also removed as completed history. Its fix, guide updates, acceptance record, and regression tests are preserved in the branch's starting commit.

### Agreed working procedure

- Select and discuss one topic at a time. Draft the issue in chat, including relevant design detail, open questions, and completion criteria.
- Obtain explicit approval for every public change, including issue creation, edits, comments, closure, and PR actions. Approval applies to the specific action and text discussed.
- Keep useful plan content in the issue itself. Preserve significant decisions and rejected alternatives, with proposal and acceptance status clear. Omit decision dates and links to backlog or plan files from issue descriptions.
- After an approved issue is created and verified, remove its migrated source content in this branch and repair local references. Record its disposition here.
- Use closed issues for completed decision work. A closed decision record preserves rationale and does not claim that separate follow-up work is implemented.

This individually reviewed procedure replaces the original fixed pilot selection and temporary-pointer handover. The migration issue retains its approved concise description; this file holds the temporary source mapping.

### Remaining migration work

- Review the remaining developer backlog, including conditional ideas and rejected approaches, and give each item an issue destination or explicit disposition.
- Review `racing-migration-notes.md`, `mating-contract.md`, `heuristiclab_operator_migration_overview.md`, and `union-types-candidates.md`.
- Update contributor guidance and repository-specific skill overrides for issue-based planning and discovery of open and closed decisions.
- Finish reference repairs, validate the final cutover, commit and publish the branch changes when authorized, and integrate into `dev`.
- Retire this file with the emptied backlog once the migration record has served its purpose and the cutover is complete.

## Accepted decision

Use GitHub Issues as the default home for ideas, bugs, design questions and implementation plans. The issue description is the working plan; a separate checked-in specification is not required.

Keep the organization simple:

- Maintain the current understanding, accepted scope, open questions and completion criteria in the issue description.
- Use comments for discussion, decision records, progress and validation evidence. Summarize settled discussion back into the description and link to significant decision comments.
- Use a checklist for steps within one change. Introduce child issues when work needs an independent discussion, review or completion state.
- Preserve rejected alternatives, their reasoning and conditions for reconsideration in their owning issues. Use a separate closed issue when a decision has independent relevance.
- Keep current library contracts, contributor rules and terminology in the existing repository documentation. Keep executable examples, tests, prototypes and reproducibility artifacts in the repository and link to them from issues.
- Do not adopt spec-driven development tooling, a mandatory specification layer or an ADR framework as part of this migration.

The complete user-documentation and glossary rework is a separate work item. Creating its issue is within migration scope; performing that rework is not.

## Responsibility mapping

| Existing material                                           | Destination                                                                         |
| ----------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| Individual unfinished backlog item                          | One issue, after checking relevance and duplicates                                  |
| Unresolved design question                                  | One issue with the question, alternatives and eventual decision                     |
| Implementation plan for one change                          | Issue description with useful implementation steps and completion criteria          |
| Roadmap spanning independent changes                        | Parent issue linking actionable child issues                                        |
| Review checkpoints and implementation history               | Issue comments and links to commits or PRs                                          |
| Rejected approach                                           | Owning issue or separate closed issue with reasoning and reconsideration conditions |
| Current behavior and architectural rules                    | Existing user and contributor documentation                                         |
| Prototype code, benchmark programs and supporting artifacts | Repository files linked from the relevant issue                                     |

Classify contents rather than creating one issue per Markdown file. A single document may mix several responsibilities. Conversely, related fragments in several documents may belong to one issue.

Retire the replaced planning entries so that the issue and local file do not become competing working plans. Preserve useful evidence and decisions before retiring a source. Completed historical records need not become new open work.

## Minimal issue conventions

Start with normal open/closed states and a few useful labels. Reuse existing labels where possible; `bug`, `enhancement`, `documentation` and `needs-decision` are sufficient starting examples. Additional boards, custom fields and automation are outside the initial migration; GitHub Projects remains an optional follow-up as described below.

An actionable issue should explain its problem, desired outcome, approach or unresolved questions, and completion criteria. The structure can be informal and proportional to the work. An idea may start with a short description and gain detail when it is investigated.

An open issue or label does not by itself approve implementation. Preserve the distinction between proposed and accepted approaches, and record review checkpoints explicitly. A design investigation may finish with an accepted decision and concrete follow-up work rather than an implementation.

For architectural work, record the accepted scope, acceptance evidence, remaining unaccepted questions and next review checkpoint. If acceptance evidence is missing or conflicting, mark it as uncertain rather than inferring approval. Editing the description does not extend an earlier approval; a material scope or design change requires explicit acceptance and a new decision record.

Use one consistent `decision` label, reusing an equivalent existing label if available, for issues carrying durable decisions. State each decision's outcome explicitly, such as accepted, rejected or superseded. Link relevant active issues to these records, including the specific decision comment when needed. Keep rejected alternatives with their owning work unless they warrant independent discovery and discussion. The contributor guidance must explain how to find decision records across both open and closed issues before revisiting a design.

Parent issues own the overall outcome and shared decisions. Child issues own their individual deliverables. Use blocking relationships where a real dependency exists, without repeating the complete parent plan in each child.

GitHub descriptions can be edited and have edit history, but they evolve independently of code revisions. Link significant decisions to their resulting commits or PRs; keep implemented contracts verifiable in code, tests and current documentation. Issue context must be fetched when working from a checkout.

Implementation issues state their required integration destination, such as the default branch or a named delivery branch. Close them only when their completion criteria, including validation and any acceptance checkpoint, are satisfied at that destination, and link the resulting commits or PRs. GitHub closing keywords apply when a PR targets the default branch; use deliberate manual closure for other destinations and avoid automatic closure before the criteria are met. Close investigation issues when their decision and any concrete follow-up work are recorded. Close parent issues when their overall outcome and shared criteria are satisfied, not merely because their children are closed.

## Optional follow-up: GitHub Projects

Proposal recorded on 2026-10-24; adoption is undecided and is not a migration requirement.

Consider one GitHub Project if issues alone do not make it easy to see which items need discussion, await a decision or are ready for implementation. A board could help selected issues progress from investigation and discussion to a well-formulated, accepted implementation scope, then through implementation and review. Possible statuses include Backlog, Exploring, Awaiting decision, Ready, In progress, Review and Done; the exact workflow is not decided. No sprint planning or iterations are required.

The project would organize issues, while their descriptions and decision comments remain the working plans. Moving an item to Ready would require clear scope, completion criteria and any required design acceptance recorded in the issue; a board move would not itself grant approval. Optional project drafts could hold tentative candidates before conversion to issues, but neither drafts nor a board should become a reason to migrate every backlog observation.

First evaluate whether issue descriptions, labels and open/closed states provide enough visibility. If they do, skip Projects entirely and avoid an additional system to maintain. If they do not, try a small board with selected issues before adopting it more broadly.

## Source evidence and batch handover

This file records source material, its inspected revision, destination or disposition, and handover status. Identify any staged or unstaged changes separately from the committed source and preserve their relevant content in the destination before retirement. Git history captures committed content. This migration does not itself authorize staging, committing or other Git actions.

Issue descriptions preserve the relevant source content directly. Remove links to backlog entries and plan files, including historical permalinks. Convert useful links to maintained code, tests, documentation, or artifacts into accessible repository links. Keep executable prototypes and reproducibility scripts discoverable. Migrate sanitized evidence summaries; raw benchmark reports, result snapshots and host-identifying output remain local unless sanitized publication is explicitly requested.

Each batch follows this handover:

1. Create or update its destination issues and record their URLs in the migration mapping. Until handover, identify them as migration drafts and keep the local sources authoritative.
2. Verify coverage, acceptance evidence, links and completion criteria. Reconcile source changes made since inventory, including staged and unstaged edits; retain unresolved discrepancies explicitly and defer handover of affected items.
3. After the selected topic and its source removal have been approved, remove its migrated local planning content and record the completed handover here. From that point, its issue is authoritative. Keep unaffected sections authoritative locally until their own handover.
4. Reuse the recorded destinations when resuming interrupted work. Check source changes and destination contents before completing a partially recorded handover; do not create duplicate issues or retire unchecked content.

Package 3 finishes the repository guidance and reference updates. Each topic requires review before its issue is published or its source retired.

## Migration packages

### 1. Inventory and pilot

1. Make `gh` available in the executing environment and authenticate if needed. Verify installation with `gh --version`, authentication with `gh auth status`, and repository access with `gh repo view heal-research/HeuristicLib --json hasIssuesEnabled,viewerPermission,defaultBranchRef`. Confirm Issues are enabled and the account has the permissions needed to manage them. Use `--repo heal-research/HeuristicLib` explicitly for GitHub operations.
2. Read existing issues, including closed ones, before creating anything. Reuse or update relevant issues rather than introducing duplicates.
3. Inventory `plans/`, the developer backlog and references to them. Classify active work, unresolved questions, rejected approaches, completed history and repository artifacts. Check enough source and history to identify stale or completed work; retain uncertainties explicitly.
4. Create the approved migration issue and maintain the temporary source-to-destination mapping in this file, including reasons for retaining repository artifacts.
5. Select topics individually with the developer. Preserve mixed completion states, accepted constraints, rejected alternatives and retained artifacts where relevant.
6. Review each draft and proposed source removal before publication. Verify the resulting description and links before handover. The issue must identify scope, constraints, completion criteria and remaining decisions on its own.

### 2. Migrate the remaining planning content

Continue migrating individually approved topics using the handover above. For each source item, record its destination here before retiring it. Carry accepted decisions, significant alternatives, completion criteria and review boundaries across explicitly.

Use the shapes demonstrated by the existing material:

- A focused fix belongs in one issue with the reported behavior and a regression-test outcome. The competing `AlgorithmRun.Stream()` example from the original plan is already covered by the current implementation and tests.
- The experiment serialization boundary belongs in a design-question issue until its intended contract is accepted.
- Performance regression benchmarking can use a parent issue with independently reviewable runner, comparison and CI deliverables. Preserve the accepted constraints from its existing plan.
- A rejected shared `RunHost` belongs in a closed issue preserving what was tried, why it failed to simplify ownership and what evidence would justify reconsideration.

This package migrates planning content. It does not implement the underlying fixes, features, architectural changes or documentation redesign.

### 3. Switch repository guidance and retire replaced plans

1. Update `AGENTS.md` so planned work starts from the assigned issue, with relevant parent, dependency and decision context. Update its decision-recording and review-source skill overrides as well, including acceptance evidence, discovery of rejected decisions and issue completion rules.
2. Update planning-role references in the developer guidelines, contributing index, requirements and other affected files. Keep these edits limited to the organization change; their architectural and product rules remain the source of current contracts.
3. Preserve the upstream skill copies. Adapt their repository-specific behavior through the existing overrides rather than adding a parallel issue-tracker framework.
4. Retire migrated plan files and backlog sections after checking their coverage and repairing references. Preserve relevant formerly uncommitted content. Useful current rules and repository artifacts must remain discoverable.
5. Verify the migration issue's completion criteria, obtain approval for any final public update, and retire this transitional plan when the migration is complete.

## Validation and completion

The migration is complete when:

- Each inventoried source item has a checked issue destination, a retained artifact with a reason, or an explicit disposition as obsolete/completed history. Source sections and revisions, relevant uncommitted changes and completed handovers are accounted for.
- Open work and rejected decisions are discoverable through issues, with proposal and acceptance status, acceptance evidence and superseded decisions clear.
- An issue supplies enough context to identify scope, relevant decisions, completion criteria and the next review checkpoint without reconstructing a long discussion.
- Each migrated topic's description and source disposition have been explicitly reviewed and approved.
- Implementation issues identify their integration destination, and closed issues satisfy the applicable implementation, investigation or parent completion rule.
- Repository guidance points to the adopted workflow and no replaced local tracker remains active.
- Issues contain their planning context directly, current artifact links resolve, and migrated evidence summaries preserve the boundary around local raw output.
- Repository links, changed Markdown formatting and the published documentation build pass the applicable checks in `AGENTS.md`.
- No underlying feature implementation or broad documentation rework has been included merely because its plan was migrated.

## References

Repository sources: [agent contract](../AGENTS.md), [developer guidelines](../docs/contributing/developer-guidelines.md), [developer backlog](developer-backlog.md) and [benchmarking issue](https://github.com/heal-research/HeuristicLib/issues/12).

- [GitHub Issues overview](https://docs.github.com/en/issues/tracking-your-work-with-issues/learning-about-issues/about-issues)
- [GitHub Projects overview](https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/about-projects)
- [Project draft items](https://docs.github.com/en/issues/planning-and-tracking-with-projects/managing-items-in-your-project/adding-items-to-your-project#creating-draft-issues)
- [Editing issue descriptions](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/editing-an-issue)
- [Issue dependencies](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/creating-issue-dependencies)
- [PR links and closing behavior](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue)
- [GitHub CLI issue access](https://cli.github.com/manual/gh_issue_view)
