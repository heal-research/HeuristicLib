# Move development planning to GitHub Issues

Status: decision accepted on 2026-10-13; migration safeguards below accepted on 2026-10-23; migration not started. Existing planning sources remain authoritative until their batch handover is recorded.

Canonical repository: [heal-research/HeuristicLib](https://github.com/heal-research/HeuristicLib).

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

For architectural work, record the accepted scope, acceptance date, supporting source or decision comment, remaining unaccepted questions and next review checkpoint. If acceptance evidence is missing or conflicting, mark it as uncertain rather than inferring approval. Editing the description does not extend an earlier approval; a material scope or design change requires explicit acceptance and a new decision record.

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

The migration issue records each source section, its inspected commit revision, destination or disposition, and handover status. Identify any staged or unstaged changes separately from the committed source and preserve their relevant content in the destination before retirement. A commit permalink does not capture uncommitted content. This migration does not itself authorize staging, committing or other Git actions.

Convert relative repository links when transferring text to issues. Use commit permalinks for historical evidence, acceptance records and retired source sections; use maintained repository links for current artifacts. Verify that linked revisions and artifacts are accessible to issue readers. Keep executable prototypes and reproducibility scripts discoverable without relying on a retired plan. Migrate sanitized evidence summaries; raw benchmark reports, result snapshots and host-identifying output remain local unless sanitized publication is explicitly requested.

Each batch follows this handover:

1. Create or update its destination issues and record their URLs in the migration mapping. Until handover, identify them as migration drafts and keep the local sources authoritative.
2. Verify coverage, acceptance evidence, links and completion criteria. Reconcile source changes made since inventory, including staged and unstaged edits; retain unresolved discrepancies explicitly and defer handover of affected items.
3. After the pilot review has been accepted, replace the migrated local planning content with temporary pointers to its issues and record the completed handover in the migration issue. From that point, the issues are authoritative for those items; the local pointers contain no competing plan. Keep unaffected sections authoritative locally until their own handover.
4. Reuse the recorded destinations when resuming interrupted work. Check the source pointers and destination contents before completing a partially recorded handover; do not create duplicate issues or retire unchecked content.

Package 3 removes the temporary pointers once repository guidance and inbound references have been updated. The pilot review gate remains in force: no handover or source retirement occurs before it is accepted.

## Migration packages

### 1. Inventory and pilot

1. Make `gh` available in the executing environment and authenticate if needed. Verify installation with `gh --version`, authentication with `gh auth status`, and repository access with `gh repo view heal-research/HeuristicLib --json hasIssuesEnabled,viewerPermission,defaultBranchRef`. Confirm Issues are enabled and the account has the permissions needed to manage them. Use `--repo heal-research/HeuristicLib` explicitly for GitHub operations.
2. Read existing issues, including closed ones, before creating anything. Reuse or update relevant issues rather than introducing duplicates.
3. Inventory `plans/`, the developer backlog and references to them. Classify active work, unresolved questions, rejected approaches, completed history and repository artifacts. Check enough source and history to identify stale or completed work; retain uncertainties explicitly.
4. Create one migration issue carrying this accepted decision and execution checklist. Use its description to maintain the temporary source-to-destination mapping defined above, including reasons for retaining repository artifacts. This is migration evidence, not a permanent second backlog.
5. Pilot one focused backlog item, the separate user-documentation/glossary work item and a small representative slice of a complex plan such as operator unification. The focused item should be small enough to judge whether its issue supplies sufficient context on its own. The documentation issue should state that structure and scope need agreement before broad rewriting. The complex slice must exercise mixed completion states, accepted constraints, rejected alternatives and retained artifacts, with links to any necessary parent or dependency context.
6. Verify the issue descriptions, labels and links. Have the reviewer identify the next authorized action, its constraints, acceptance evidence and next checkpoint using only the issues and their linked context. Present this pilot and proposed remaining batches for review. Stop at this checkpoint before bulk conversion, batch handover or retiring source files.

### 2. Migrate the remaining planning content

After explicit acceptance of the pilot review, migrate coherent batches of related work using the batch handover above, including the pilot items. For each source item, record its destination in the migration issue before retiring it. Carry accepted decisions, significant alternatives, completion criteria and review boundaries across explicitly.

Use the shapes demonstrated by the existing material:

- A focused fix such as rejecting a competing `AlgorithmRun.Stream()` call belongs in one issue with the reported behavior and a regression-test outcome. Recheck its relevance before presenting it as a current defect.
- The experiment serialization boundary belongs in a design-question issue until its intended contract is accepted.
- Performance regression benchmarking can use a parent issue with independently reviewable runner, comparison and CI deliverables. Preserve the accepted constraints from its existing plan.
- A rejected shared `RunHost` belongs in a closed issue preserving what was tried, why it failed to simplify ownership and what evidence would justify reconsideration.

This package migrates planning content. It does not implement the underlying fixes, features, architectural changes or documentation redesign.

### 3. Switch repository guidance and retire replaced plans

1. Update `AGENTS.md` so planned work starts from the assigned issue, with relevant parent, dependency and decision context. Update its decision-recording and review-source skill overrides as well, including acceptance evidence, discovery of rejected decisions and issue completion rules.
2. Update planning-role references in the developer guidelines, contributing index, requirements and other affected files. Keep these edits limited to the organization change; their architectural and product rules remain the source of current contracts.
3. Preserve the upstream skill copies. Adapt their repository-specific behavior through the existing overrides rather than adding a parallel issue-tracker framework.
4. Remove temporary pointers and retire migrated plan files and backlog sections only after checking their coverage and repairing references. Verify historical permalinks and preservation of relevant formerly uncommitted content; Git history alone preserves only committed documents. Useful current rules and repository artifacts must remain discoverable.
5. Preserve this accepted decision in the migration issue, then retire this transitional plan when the migration is complete.

## Validation and completion

The migration is complete when:

- Each inventoried source item has a checked issue destination, a retained artifact with a reason, or an explicit disposition as obsolete/completed history. Source sections and revisions, relevant uncommitted changes and completed handovers are accounted for.
- Open work and rejected decisions are discoverable through issues, with proposal and acceptance status, acceptance evidence and superseded decisions clear.
- An issue supplies enough context to identify scope, relevant decisions, completion criteria and the next review checkpoint without reconstructing a long discussion.
- The pilot demonstrates this for focused work, the documentation proposal and a complex plan slice; its review has been explicitly accepted.
- Implementation issues identify their integration destination, and closed issues satisfy the applicable implementation, investigation or parent completion rule.
- Repository guidance points to the adopted workflow and no replaced local tracker remains active.
- Historical links identify accessible revisions, current artifact links resolve, and migrated evidence summaries preserve the boundary around local raw output.
- Repository links, changed Markdown formatting and the published documentation build pass the applicable checks in `AGENTS.md`.
- No underlying feature implementation or broad documentation rework has been included merely because its plan was migrated.

## References

Repository sources: [agent contract](../AGENTS.md), [developer guidelines](../docs/contributing/developer-guidelines.md), [developer backlog](developer-backlog.md) and [benchmarking plan](performance-regression-benchmarking.md).

- [GitHub Issues overview](https://docs.github.com/en/issues/tracking-your-work-with-issues/learning-about-issues/about-issues)
- [GitHub Projects overview](https://docs.github.com/en/issues/planning-and-tracking-with-projects/learning-about-projects/about-projects)
- [Project draft items](https://docs.github.com/en/issues/planning-and-tracking-with-projects/managing-items-in-your-project/adding-items-to-your-project#creating-draft-issues)
- [Editing issue descriptions](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/editing-an-issue)
- [Issue dependencies](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/creating-issue-dependencies)
- [PR links and closing behavior](https://docs.github.com/en/issues/tracking-your-work-with-issues/using-issues/linking-a-pull-request-to-an-issue)
- [GitHub CLI issue access](https://cli.github.com/manual/gh_issue_view)
