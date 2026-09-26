# Agent skills

Shared agent skills for this repository. Codex discovers them here. Claude Code reads only `.claude/skills/`, which holds identical copies of the non-.NET skills, and gets the .NET skills through the `dotnet` plugin that `.claude/settings.json` enables. The copies are duplicated rather than symlinked because git on Windows checks symlinks out as plain files unless each contributor enables them.

The skills are copied verbatim from their sources, with each source's license beside them. Repository-specific behavior lives in the "Agent skills" section of `AGENTS.md`, not in these files, so an update is a plain copy of the new upstream version.

| Skill | Source | Claude Code | Local change |
| --- | --- | --- | --- |
| `csharp-refactoring` | [dotnet/skills](https://github.com/dotnet/skills) `plugins/dotnet/skills/csharp-refactoring` | `dotnet` plugin | None |
| `setup-local-sdk` | [dotnet/skills](https://github.com/dotnet/skills) `plugins/dotnet/skills/setup-local-sdk` | `dotnet` plugin | None |
| `two-axis-review` | [mattpocock/skills](https://github.com/mattpocock/skills) `skills/engineering/code-review` | `.claude/skills/two-axis-review` | Renamed from `code-review`, which clashes with the Claude Code built-in |
| `domain-modeling` | [mattpocock/skills](https://github.com/mattpocock/skills) `skills/engineering/domain-modeling` | `.claude/skills/domain-modeling` | None |
| `grill-with-docs` | [mattpocock/skills](https://github.com/mattpocock/skills) `skills/engineering/grill-with-docs` | `.claude/skills/grill-with-docs` | None |
| `grilling` | [mattpocock/skills](https://github.com/mattpocock/skills) `skills/productivity/grilling` | `.claude/skills/grilling` | None |

Copied from dotnet/skills at `a55fbf42c36a37b94ec07291bd79cb6b6bf3a04d`, `dotnet` plugin version 0.2.4, and from mattpocock/skills at `c55ee46073ed923f86ce59a5eb3b6d895095d1b7`.

When updating a skill, copy the whole upstream folder, reapply the local change listed above, copy the result to `.claude/skills/` as well when the table lists a folder there, and update the commit here. Both copies must stay identical. The .NET skills are only copied for Codex; Claude Code takes the plugin's current version, so the two can differ until the next copy.
