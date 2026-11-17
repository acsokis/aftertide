# AFTERTIDE — authoritative AI development instructions

This document is the shared, repository-level operating policy for OpenAI Codex, GitHub Copilot, Google Antigravity, and any local AI coding assistant used with AFTERTIDE. Last revised: 2026-11-17.

## 1. Project identity and authoritative sources

- Create an **original, persistent first-person Caribbean survival, exploration and settlement game**. Player-facing text is in English; explain development progress to the maintainer in Hungarian unless requested otherwise.
- Technology currently checked into this repo: **Godot 4.7.2 .NET, C#, .NET 10**. Read `README.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/SOURCES.md` and `NOTICE.md` before substantive subsystem edits. Inspect actual versions rather than assuming local installations match.
- `docs/AI-IDE-SESSION-2026-11-17.md` is a curated cross-IDE handoff; `.github/copilot-instructions.md` points Copilot to this policy. The session file is not a complete or live copy of a ChatGPT conversation.
- Treat code, tests and versioned project docs as the source of truth. Do not infer completed features or performance from plans, chat discussions or successful compilation.

## 2. Architecture, intellectual property and game integrity

- Keep **simulation, geography, movement, UI/presentation and saved state** separated. Prefer deterministic simulation, explicit data, stable IDs and save compatibility, or supply tested migrations.
- Do not import proprietary/commercial-game assets, unreviewed reference code, uncleared models or third-party content without rights review. Do not copy code or assets from the separate Island Survival project without explicit permission and licensing review.
- The prototype has **one detailed local Jamaica region**, not a finished seamless playable Caribbean. Do not claim implemented global streaming, full animal ecosystems, multiplayer, RTX/path tracing, DLSS, PhysX or proven FPS until independently validated.
- Review generated work. AI tools are development-time helpers, not runtime requirements. The shipped game must not require cloud model credentials or internet to run.

## 3. Multi-agent and IDE operation

- Compatible surfaces may include VS Code, the VS Code Agents Window, Visual Studio Community and Google Antigravity. The same Git checkout and these versioned instructions provide **shared project context**, not automatic synchronization of different vendors' chat sessions.
- Use OpenAI Codex with **Sign in with ChatGPT** where the existing paid plan supports it; never silently switch to separately billed OpenAI API credentials.
- Use Antigravity with an eligible existing Google account. GitHub Copilot access must be verified separately; do not assume a paid Copilot entitlement.
- Start each new task by inspecting the current branch, `git status`, the relevant docs and existing tests. Outline a scoped plan and identify affected files.
- Two writing agents must **never modify the same worktree concurrently**. Give separate tasks their own Git branches/worktrees and review diffs/CI before merging.
- Never silently enable remote sessions, SSH tunnels, cloud agents, automatic deployment, new subscriptions or paid API usage. Remote-host session sharing is distinct from storing files in Git.

## 4. Local AI and ComfyUI policy

- Local AI may be used for coding assistance or **offline development-asset workflows** (e.g. ComfyUI); it is not a requirement for playing AFTERTIDE.
- **Inventory first**: find actual installations, active processes, configuration files, environment/package managers, model folders, checkpoint and LoRA references, ComfyUI workflows, custom nodes, shared model directories, and disk usage across all plausible Windows volumes/paths.
- Do not assume that a directory called `LocalAI`, `ComfyUI`, `Ollama`, `llama.cpp` or `models` is obsolete. Version age alone does not determine compatibility. Record executable version, model name/revision, workflow dependencies, installed runtimes, and tested compatibility.
- Prefer **the newest verified compatible version** of each needed local AI runtime/model; 'newest' is a *reviewed target*, not permission to install untested builds or silently swap checkpoints. On RTX 4080-class hardware, validate VRAM and workflow performance rather than assuming compatibility.
- Reuse **one canonical shared models directory** where supported; for ComfyUI, prefer supported model search paths through `extra_model_paths.yaml` instead of duplicating large weight files. Never delete files referred to by workflows, custom nodes, other AI tools, or pinned project tests.
- Before cleanup: identify the active install, compare hashes when necessary, inspect current workflow references and dependencies, make a backup/snapshot and generate an explicit candidate list. Treat directories with no verification as **unknown**, not obsolete.
- Even where general cleanup is authorized, **quarantine or back up** confirmed unused duplicates before irreversible deletion; first confirm no active process uses them. Do not implement an automatic recursive delete of suspected AI folders.
- Do not commit model weights, training data, generated media, output caches, virtual environments or personal machine-specific configuration to this repository. Keep local inventory in an appropriately private location.

## 5. Validation and release gates

- Baseline build: `dotnet build Aftertide.csproj`.
- Domain checks: `dotnet run --project tests/Aftertide.Checks.csproj -- Aftertide/Data`.
- Engine smoke checks: follow `README.md`, use `AFTERTIDE_SMOKE=1`, an **isolated save directory** via `AFTERTIDE_SAVE_DIR`, and verify `AFTERTIDE_SMOKE_PASS` with exit code 0.
- Do not call a build pass a complete 3D gameplay test, a graphics-quality test or an FPS benchmark. Clearly report which checks were executed versus not executed.
- Keep focused diffs and meaningful commits. Never commit secrets, personal chat transcripts, private prompts, credentials, saves, caches or uncleared assets.
- No force-push, unreviewed merge, public release or production deployment without explicit maintainer approval and appropriate validation.

## 6. Handoff discipline

When a significant task completes, record in the curated session or task document:
- branch/commit, IDE/agent, concrete changes, verified tool/model versions;
- tests run with actual results, outstanding issues and uncommitted changes;
- compatibility impact, next decision and any manual setup still needed.

The absence of local Windows access must be disclosed. A GitHub commit is **not** proof that a local installation, IDE sign-in, running ComfyUI workflow or AI model cleanup succeeded.
