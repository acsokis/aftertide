# Architecture

- `Aftertide/Data/gameplay.json`: original items, masses, capacities through code,
  crafting/build costs, production inputs/outputs and ten objective definitions.
- `Simulation.cs`: engine-independent needs, bags with preserved food age,
  atomic exchanges, workforce conditions, weather schedule, trust and objectives.
- `Geography.cs` / `Data/geography.json`: source coast polygons, documented
  regional projection and metre-scale local conversion. Relief is procedural.
- `AftertideGame.cs`: Godot scene, supported placement, navigation, interactions,
  work-site presentation, geographic chart and portable save adapter.
- `Player.cs`: first-person input, physical capsule and camera.
- `CaribbeanMap.cs`: overview from the same coast data used locally.
- `Checks.cs` / `AftertideSmoke.cs`: deterministic domain and engine smoke checks.

Simulation state is independent of rendered models. UI sends requests to it.
Construction consumes actual inventory. Production uses shared storage and
requires an assigned worker to be at a work site. Navigation uses a bounded local
navmesh, projected construction/resource obstacles, RVO and actual body collision;
avoidance is not a replacement for collision. The initial version has one worker.

Save schema 1 uses the `aftertide-jamaica-1` world ID. Quest completion and action
receipts are persisted. Files are written through a temporary file with a previous
backup. Legacy project saves are never interpreted as AFTERTIDE saves.

Current limits: no global Caribbean streaming, no remote settlements, no full
animal ecosystem, no multiplayer and no validated PhysX/path-tracing parity.
The boat uses a bounded shallow-water movement model, not a full marine solver.
Further split the scene adapter as features mature; keep the simulation testable.
