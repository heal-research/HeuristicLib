# AGENTS.md

This folder contains executable API usage specs.

- These files are not ordinary assertion-heavy unit tests.
- Their main value is that they express intended usage clearly, compile cleanly, and run in normal test flow.
- A spec may contain only a few assertions if its main job is to document API shape and ergonomics.
- Do not remove seemingly unused setup, locals, or intermediate values when they help show the intended usage flow.
- Keep each spec focused on one user story.
- It is fine to keep current-state and desired-state specs side by side when that makes the refactoring path clearer.
- Detailed edge cases still belong in the normal unit test projects.
