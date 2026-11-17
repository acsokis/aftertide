# AFTERTIDE — shared IDE and AI-agent session handoff
Date: 2026-11-17
Status: documentation-only; local IDE and account authentication NOT configured by this document.

## Purpose
Preserve a **curated, project-specific development context** across GitHub checkouts used by Visual Studio Code, supported VS Code Agents Window sessions, Google Antigravity Agent Manager, and Visual Studio Community. This is **not** the full ChatGPT conversation transcript, and it does not synchronize vendor chat histories or live sessions.

## Project baseline (verified in this repository)
- AFTERTIDE: an original, persistent first-person Caribbean survival, exploration and settlement prototype. Working title; naming clearance still pending.
- Engine: Godot 4.7.2 .NET; language: C#; SDK: .NET 10 (`Aftertide.csproj`). Main Godot scene: `Aftertide/Main.tscn`.
- Separate simulation and Godot scene presentation; preserve save compatibility and stable IDs. See `docs/ARCHITECTURE.md`.
- Current playable detail is **one local starting area** near northeastern Jamaica, not a finished seamless playable Caribbean. Do not claim global streaming, advanced RTX/PhysX features, multiplayer, or verified performance.
- Current priorities are in `docs/ROADMAP.md`: manual full-chapter 3D traversal/QA, save integrity, workforce navigation and original art, fork portability and profiling.
- Review `README.md`, `AGENTS.md`, `NOTICE.md`, `docs/ARCHITECTURE.md`, `docs/SOURCES.md`, and `docs/ROADMAP.md` before implementation.
- The **existing root `AGENTS.md` takes precedence** for repo-specific engineering policies. Do not overwrite it with generic cross-IDE instructions.

## Developer environments and account boundaries
- OpenAI Codex in VS Code: sign in through **ChatGPT** using the existing paid ChatGPT account; do not substitute a metered API key silently.
- Google Antigravity / Agent Manager: sign in with the existing eligible Google subscription account; inspect current tool/version support. A remote or local Antigravity conversation is separate from a Codex conversation.
- GitHub Copilot (including Visual Studio Community or VS Code integrations): depends on separately verified GitHub Copilot entitlements; a GitHub repository account alone does not imply a paid Copilot plan.
- Microsoft VS Code Agent Host remote agent sessions / GitHub Environments (Preview) are a provider-specific session-sharing mechanism. They do **not** automatically ingest external ChatGPT chats or Antigravity sessions; their setup is separate from this Git repository.
- No new subscription, cloud environment, API billing, secrets or remote access should be enabled without explicit approval. No API keys or personal billing details belong in Git.

## Agent workflow for safe concurrent development
1. Read the project context above and inspect the current checkout/branch.
2. Before coding, document scope, risks, affected files, test plan and any required game-engine validation.
3. For simultaneous agents use separate branches or `git worktree` directories. Never run two writing agents against the same working directory.
4. Keep simulation deterministic where possible and preserve save compatibility (or add migrations).
5. Run `dotnet build Aftertide.csproj` and `dotnet run --project tests/Aftertide.Checks.csproj -- Aftertide/Data` when available. Godot runtime smoke tests need an isolated save directory and `AFTERTIDE_SMOKE=1`; see README.
6. Clearly distinguish checks actually performed from checks not performed. A successful build alone is not a playable-game or performance validation.
7. Open a PR for review; do not merge, release, force-push or deploy without approval. Update this handoff only when substantial verified decisions or results change.

## Current handoff (2026-11-17)
- Objective: Make AFTERTIDE's ongoing development context accessible across the supported agent-capable editors without relying on a single vendor chat session.
- The repository already had a project-specific `AGENTS.md` and developer docs. They are retained unchanged.
- Cross-IDE guidance is now stored in this document and the Copilot instruction file at `.github/copilot-instructions.md`.
- Existing Windows IDE installations, remote agent session settings, extension versions and OAuth login status are **not verified or modified**.
- A screenshot in the originating discussion showed an `IslandSurvivor / AfterTide` editor/session entry with a large displayed diff; it does not prove all local changes have been committed and pushed. Check Git status locally before any pull/merge.
- This is the independent AFTERTIDE repository `acsokis/aftertide`, not the separate repository `vizisiklo/IslandSurvival_Alpha_0.2`. Do not copy code or data between them without rights review.

## Optional continuation record (append after validation)
- Date / IDE / agent / branch / last commit:
- Work completed:
- Checks run (exact commands and outcomes):
- Outstanding errors and uncommitted changes:
- Decision and next task:

## Reference
https://code.visualstudio.com/docs/agents/run/remote-agent-sessions#_github-environments-preview
