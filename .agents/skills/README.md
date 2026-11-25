# Agent skills

Codex reads skills from this folder. Claude Code reads `.claude/skills/`, which holds identical copies of the non-.NET skills, and gets the .NET skills from the `dotnet` plugin enabled in `.claude/settings.json`. The copies are not symlinks because git on Windows checks symlinks out as plain files by default.

Skills are verbatim upstream copies with their licenses. Repository-specific behavior belongs in the "Agent skills" section of `AGENTS.md`.

| Skill                | Source                                                                                         | Claude Code                      | Local change                                                            |
| -------------------- | ---------------------------------------------------------------------------------------------- | -------------------------------- | ----------------------------------------------------------------------- |
| `csharp-refactoring` | [dotnet/skills](https://github.com/dotnet/skills) `plugins/dotnet/skills/csharp-refactoring`   | `dotnet` plugin                  | None                                                                    |
| `setup-local-sdk`    | [dotnet/skills](https://github.com/dotnet/skills) `plugins/dotnet/skills/setup-local-sdk`      | `dotnet` plugin                  | None                                                                    |
| `two-axis-review`    | [mattpocock/skills](https://github.com/mattpocock/skills) `skills/engineering/code-review`     | `.claude/skills/two-axis-review` | Renamed from `code-review`, which clashes with the Claude Code built-in |
| `domain-modeling`    | [mattpocock/skills](https://github.com/mattpocock/skills) `skills/engineering/domain-modeling` | `.claude/skills/domain-modeling` | None                                                                    |
| `grill-with-docs`    | [mattpocock/skills](https://github.com/mattpocock/skills) `skills/engineering/grill-with-docs` | `.claude/skills/grill-with-docs` | None                                                                    |
| `grilling`           | [mattpocock/skills](https://github.com/mattpocock/skills) `skills/productivity/grilling`       | `.claude/skills/grilling`        | None                                                                    |

Copied from dotnet/skills at `a55fbf42c36a37b94ec07291bd79cb6b6bf3a04d` (`dotnet` plugin 0.2.4) and mattpocock/skills at `c55ee46073ed923f86ce59a5eb3b6d895095d1b7`.

To update a skill, copy the whole upstream folder, reapply the listed local change, copy the result to `.claude/skills/` where the table lists a folder, and update the commits above. The .NET skills are copied for Codex only; Claude Code uses the plugin's current version.
