# GitHub Copilot instructions — AFTERTIDE

This is an original Godot/.NET Caribbean survival and settlement game. Before any implementation:
- Read the existing root `AGENTS.md` first; it remains the authoritative agent policy.
- Read `README.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/SOURCES.md`, and `NOTICE.md` as applicable.
- Read `docs/AI-IDE-SESSION-2026-11-17.md` for cross-IDE continuity and account/session limitations.

Engineering:
- Preserve the architecture boundary between engine-independent simulation, Godot presentation, geography, and save state.
- Preserve stable identities and save compatibility or design an explicit migration.
- Do not claim complete Caribbean terrain, RTX features, PhysX, multiplayer, a performance target, or manual gameplay completion without real verification.
- Prefer small changes, dedicated branches/worktrees for concurrent agents, and traceable PRs.
- Run relevant domain/build tests; report which tests and runtime checks were actually performed and which remain outstanding.
- Do not incorporate unlicensed game assets, proprietary third-party code, AI-generated secrets, or private chats in commits.
- Never silently activate API billing, enable remote sharing, force-push or release.

Communication:
- The maintainer generally prefers Hungarian explanations, while player-facing game text is English.
- Keep verification notes and decisions in the repository handoff document when explicitly requested. Vendor agent chats are not automatically synchronized with Git.
